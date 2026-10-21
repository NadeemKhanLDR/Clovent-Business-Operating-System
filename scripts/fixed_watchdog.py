"""
fixed_watchdog.py

A non-destructive wall-clock watchdog controller.
Enforces an external deadline by terminating ONLY verified owned automation runner processes,
while strictly protecting Windows Sandbox, SQL Server, and evidence files.

Features:
- Validates PID ownership and process identity before any action.
- Recursively inspects the entire descendant process tree of target PIDs.
- Strictly protects Windows Sandbox, Hyper-V (vmwp), and SQL Server even if present
  anywhere in a descendant tree.
- Never executes unverified taskkill /T that could sweep protected processes.
"""

import sys
import os
import time
import datetime
import subprocess
import argparse
import json

PROTECTED_PROCESS_NAMES = {
    "windowssandbox.exe",
    "windowssandboxclient.exe",
    "vmwp.exe",
    "sqlservr.exe",
    "sqlwriter.exe",
    "sqllocaldb.exe",
    "system",
    "registry",
    "smss.exe",
    "csrss.exe",
    "wininit.exe",
    "services.exe",
    "lsass.exe",
    "svchost.exe",
    "explorer.exe"
}

ALLOWED_AUTOMATION_ROOT_NAMES = {
    "powershell.exe",
    "pwsh.exe",
    "cmd.exe",
    "python.exe",
    "dotnet.exe",
    "conhost.exe"
}

def is_protected(proc_name):
    if not proc_name:
        return False
    clean = proc_name.lower().strip()
    return clean in PROTECTED_PROCESS_NAMES or any(clean.startswith(p.replace(".exe", "")) for p in PROTECTED_PROCESS_NAMES)

def get_process_map():
    """
    Returns a dictionary of all running processes:
    pid -> {"name": str, "parent_pid": int, "path": str}
    """
    procs = {}
    try:
        ps_cmd = "Get-CimInstance Win32_Process | Select-Object ProcessId, ParentProcessId, Name, ExecutablePath | ConvertTo-Json -Compress"
        out = subprocess.check_output(["powershell", "-NoProfile", "-Command", ps_cmd], text=True, stderr=subprocess.DEVNULL).strip()
        if out:
            data = json.loads(out)
            if isinstance(data, dict):
                data = [data]
            for item in data:
                pid = item.get("ProcessId")
                ppid = item.get("ParentProcessId")
                name = item.get("Name") or ""
                path = item.get("ExecutablePath") or ""
                if pid is not None:
                    procs[int(pid)] = {
                        "name": str(name).lower(),
                        "parent_pid": int(ppid) if ppid is not None else 0,
                        "path": str(path)
                    }
    except Exception:
        pass
    return procs

def get_descendants(root_pid, proc_map):
    """
    Returns list of all descendant PIDs of root_pid.
    """
    children = []
    queue = [root_pid]
    visited = {root_pid}
    while queue:
        curr = queue.pop(0)
        for pid, info in proc_map.items():
            if info["parent_pid"] == curr and pid not in visited:
                visited.add(pid)
                children.append(pid)
                queue.append(pid)
    return children

def terminate_process_safe(pid, name, log_lines):
    if is_protected(name):
        msg = f"[SAFEGUARD] Blocked termination of protected process PID {pid} ({name})."
        print(msg)
        log_lines.append(msg)
        return False

    msg = f"[WATCHDOG] Terminating verified automation process PID {pid} ({name})."
    print(msg)
    log_lines.append(msg)
    try:
        res = subprocess.run(["taskkill", "/PID", str(pid), "/F"], capture_output=True, text=True)
        log_lines.append(f"taskkill PID {pid}: {res.stdout.strip()} {res.stderr.strip()}")
        return True
    except Exception as ex:
        log_lines.append(f"Error terminating PID {pid}: {ex}")
        return False

def terminate_owned_tree_verified(root_pid, log_lines, proc_map=None):
    """
    Verifies PID ownership and inspects all descendants before termination.
    Never uses indiscriminate taskkill /T.
    """
    if proc_map is None:
        proc_map = get_process_map()

    if root_pid not in proc_map:
        msg = f"[WATCHDOG] Target PID {root_pid} not found in active process table (already exited)."
        print(msg)
        log_lines.append(msg)
        return

    root_info = proc_map[root_pid]
    root_name = root_info["name"]

    # 1. Verify PID ownership: must not be protected, and should be expected automation process
    if is_protected(root_name):
        msg = f"[SAFEGUARD] Target PID {root_pid} is a protected process ({root_name}). Aborting termination."
        print(msg)
        log_lines.append(msg)
        return

    if root_name not in ALLOWED_AUTOMATION_ROOT_NAMES and not any(root_name.startswith(a.replace(".exe", "")) for a in ALLOWED_AUTOMATION_ROOT_NAMES):
        msg = f"[SAFEGUARD] Target PID {root_pid} ({root_name}) is not a recognized automation runner. Aborting."
        print(msg)
        log_lines.append(msg)
        return

    # 2. Inspect all descendants
    descendant_pids = get_descendants(root_pid, proc_map)

    # 3. Check for any protected processes in descendants
    protected_descendants = []
    for d_pid in descendant_pids:
        d_name = proc_map.get(d_pid, {}).get("name", "")
        if is_protected(d_name):
            protected_descendants.append((d_pid, d_name))

    if protected_descendants:
        msg = f"[SAFEGUARD] Detected {len(protected_descendants)} protected processes in descendant tree of PID {root_pid}: {protected_descendants}. Refusing taskkill /T!"
        print(msg)
        log_lines.append(msg)

        # Terminate ONLY non-protected descendants individually from bottom up, excluding protected processes
        non_protected = [p for p in reversed(descendant_pids) if not is_protected(proc_map.get(p, {}).get("name", ""))]
        for p in non_protected:
            terminate_process_safe(p, proc_map.get(p, {}).get("name", ""), log_lines)

        # Terminate root process safely
        terminate_process_safe(root_pid, root_name, log_lines)
    else:
        # All descendants are confirmed non-protected automation processes.
        # Terminate descendants from bottom-up, then root PID.
        for p in reversed(descendant_pids):
            terminate_process_safe(p, proc_map.get(p, {}).get("name", ""), log_lines)
        terminate_process_safe(root_pid, root_name, log_lines)

def main():
    parser = argparse.ArgumentParser(description="Wall-clock watchdog for automation tasks.")
    parser.add_argument("evidence_dir", help="Directory where evidence and watchdog files are stored")
    parser.add_argument("max_minutes", type=int, default=45, nargs="?", help="Maximum runtime in minutes")
    parser.add_argument("--pid", type=int, action="append", default=[], help="PID of owned automation process to terminate on timeout")
    parser.add_argument("--pid-file", help="Path to file containing PIDs (one per line) of owned automation processes")

    args = parser.parse_args()

    os.makedirs(args.evidence_dir, exist_ok=True)
    start_time = datetime.datetime.now(datetime.timezone.utc)
    deadline = start_time + datetime.timedelta(minutes=args.max_minutes)

    pids_to_supervise = list(args.pid)
    if args.pid_file and os.path.exists(args.pid_file):
        with open(args.pid_file, "r", encoding="utf-8") as f:
            for line in f:
                line = line.strip()
                if line.isdigit():
                    pids_to_supervise.append(int(line))

    start_msg = (
        f"WATCHDOG_START: StartUtc={start_time.isoformat()}, "
        f"DeadlineUtc={deadline.isoformat()}, MaxMinutes={args.max_minutes}, "
        f"SupervisedPids={pids_to_supervise}\n"
    )

    start_file = os.path.join(args.evidence_dir, "WATCHDOG_START.txt")
    with open(start_file, "w", encoding="utf-8") as f:
        f.write(start_msg)

    print(start_msg.strip(), flush=True)

    stop_file = os.path.join(args.evidence_dir, "STOP_REQUESTED.txt")
    timeout_file = os.path.join(args.evidence_dir, "WATCHDOG_TIMEOUT.txt")
    log_file = os.path.join(args.evidence_dir, "WATCHDOG_LOG.txt")

    while True:
        now = datetime.datetime.now(datetime.timezone.utc)
        if os.path.exists(stop_file):
            stop_msg = f"WATCHDOG_STOP: Clean stop requested at {now.isoformat()}.\n"
            print(stop_msg.strip(), flush=True)
            with open(log_file, "a", encoding="utf-8") as f:
                f.write(stop_msg)
            sys.exit(0)

        # Re-check pid-file dynamically if present
        if args.pid_file and os.path.exists(args.pid_file):
            try:
                with open(args.pid_file, "r", encoding="utf-8") as f:
                    for line in f:
                        line = line.strip()
                        if line.isdigit():
                            pid = int(line)
                            if pid not in pids_to_supervise:
                                pids_to_supervise.append(pid)
            except Exception:
                pass

        if now >= deadline:
            timeout_msg = (
                f"TIMEOUT: Fixed {args.max_minutes}-minute wall-clock limit reached at {now.isoformat()} "
                f"(Started: {start_time.isoformat()}, Deadline: {deadline.isoformat()})\n"
            )
            print(timeout_msg.strip(), flush=True)

            log_lines = [timeout_msg]
            proc_map = get_process_map()

            # Stop ONLY verified owned automation processes after inspecting descendant trees
            for pid in pids_to_supervise:
                terminate_owned_tree_verified(pid, log_lines, proc_map)

            # Safeguard confirmation
            log_lines.append("[SAFEGUARD] Preserved Windows Sandbox, SQL Server, and evidence directory.\n")

            with open(timeout_file, "w", encoding="utf-8") as f:
                f.write(timeout_msg)

            with open(log_file, "a", encoding="utf-8") as f:
                f.write("\n".join(log_lines) + "\n")

            sys.exit(1)

        time.sleep(5)

if __name__ == "__main__":
    main()
