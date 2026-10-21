"""
fixed_watchdog.py

A non-destructive wall-clock watchdog controller.
Enforces an external deadline by terminating ONLY its owned automation runner process tree,
while strictly protecting Windows Sandbox, SQL Server, and evidence files.

Usage:
    python fixed_watchdog.py <evidence_dir> <max_minutes> [--pid <PID>] [--pid-file <PATH>]
"""

import sys
import os
import time
import datetime
import subprocess
import argparse

PROTECTED_PROCESS_NAMES = {
    "windowssandbox.exe",
    "windowssandboxclient.exe",
    "vmwp.exe",
    "sqlservr.exe",
    "sqlwriter.exe",
    "sqllocaldb.exe",
}

def is_protected(proc_name):
    if not proc_name:
        return False
    clean = proc_name.lower().strip()
    return clean in PROTECTED_PROCESS_NAMES or any(clean.startswith(p.replace(".exe", "")) for p in PROTECTED_PROCESS_NAMES)

def get_process_name(pid):
    try:
        out = subprocess.check_output(
            ["powershell", "-NoProfile", "-Command", f"(Get-Process -Id {pid} -ErrorAction SilentlyContinue).ProcessName"],
            text=True, stderr=subprocess.DEVNULL
        ).strip()
        if out:
            return f"{out}.exe"
    except Exception:
        pass
    return ""

def terminate_owned_tree(root_pid, log_lines):
    root_name = get_process_name(root_pid)
    if not root_name:
        msg = f"[WATCHDOG] Process PID {root_pid} already exited or not found."
        print(msg)
        log_lines.append(msg)
        return

    if is_protected(root_name):
        msg = f"[SAFEGUARD] Refusing to terminate protected process PID {root_pid} ({root_name})."
        print(msg)
        log_lines.append(msg)
        return

    msg = f"[WATCHDOG] Terminating owned automation process tree rooted at PID {root_pid} ({root_name})..."
    print(msg)
    log_lines.append(msg)

    try:
        res = subprocess.run(
            ["taskkill", "/PID", str(root_pid), "/T", "/F"],
            capture_output=True, text=True
        )
        log_lines.append(f"taskkill output: {res.stdout.strip()} {res.stderr.strip()}")
    except Exception as ex:
        log_lines.append(f"Error terminating PID {root_pid}: {ex}")

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
            # Stop ONLY registered owned automation processes
            for pid in pids_to_supervise:
                terminate_owned_tree(pid, log_lines)

            # Safeguard confirmation:
            log_lines.append("[SAFEGUARD] Preserved Windows Sandbox, SQL Server, and evidence directory.\n")

            with open(timeout_file, "w", encoding="utf-8") as f:
                f.write(timeout_msg)

            with open(log_file, "a", encoding="utf-8") as f:
                f.write("\n".join(log_lines) + "\n")

            sys.exit(1)

        time.sleep(5)

if __name__ == "__main__":
    main()
