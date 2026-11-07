"""Detached owner/supervisor for bounded, evidence-preserving Sandbox diagnostics.

launch returns immediately; supervisor survives controller exit and owns cleanup.
No CBOS application, database, or security-policy operations are performed.
"""
import argparse
import datetime as dt
import json
import os
from pathlib import Path
import subprocess
import sys
import time
import uuid
from xml.sax.saxutils import escape

WSB = str(Path(os.environ['LOCALAPPDATA']) / 'Microsoft/WindowsApps/wsb.exe')

def utc():
    return dt.datetime.now(dt.timezone.utc).isoformat()

def save(path, value):
    tmp = path.with_suffix(path.suffix + '.tmp')
    tmp.write_text(json.dumps(value, indent=2), encoding='utf-8')
    tmp.replace(path)

def cli(folder, label, args, timeout):
    record = {'start_utc': utc(), 'args': args, 'timeout_seconds': timeout}
    try:
        p = subprocess.run([WSB, *args], capture_output=True, text=True,
                           timeout=max(.1, timeout), creationflags=subprocess.CREATE_NO_WINDOW)
        record.update(exit_code=p.returncode, stdout=p.stdout, stderr=p.stderr)
    except subprocess.TimeoutExpired as ex:
        record.update(exit_code=None, timed_out=True,
                      stdout=(ex.stdout or b'').decode(errors='replace') if isinstance(ex.stdout, bytes) else ex.stdout,
                      stderr=(ex.stderr or b'').decode(errors='replace') if isinstance(ex.stderr, bytes) else ex.stderr)
    record['end_utc'] = utc()
    save(folder / (label + '.json'), record)
    return record

def supervise(folder, seconds, mode, media):
    owned = str(uuid.uuid4())
    started = time.monotonic()
    deadline = started + seconds
    save(folder / 'ownership.json', dict(sandbox_id=owned, supervisor_pid=os.getpid(),
         start_utc=utc(), deadline_utc=(dt.datetime.now(dt.timezone.utc)+dt.timedelta(seconds=seconds)).isoformat(),
         budget_seconds=seconds, mode=mode))
    def remaining(cap):
        left = deadline - time.monotonic()
        if left <= 0:
            raise TimeoutError('absolute deadline reached')
        return min(cap, left)
    outcome = {'sandbox_id': owned, 'reason': 'exception'}
    try:
        maps = [(str(folder), 'C:\\Diagnostic', False)]
        if media:
            maps.append((media, 'C:\\SqlMedia', True))
        xml = '<Configuration><Networking>Disable</Networking><MappedFolders>'
        for host, guest, readonly in maps:
            xml += f'<MappedFolder><HostFolder>{escape(host)}</HostFolder><SandboxFolder>{guest}</SandboxFolder><ReadOnly>{str(readonly).lower()}</ReadOnly></MappedFolder>'
        xml += '</MappedFolders></Configuration>'
        (folder / 'sandbox_config.xml').write_text(xml, encoding='utf-8')
        result = cli(folder, 'start', ['start', '--id', owned, '-c', xml, '--raw'], remaining(60))
        if result.get('exit_code') != 0:
            raise RuntimeError('Sandbox start failed or timed out; cleanup still owns preassigned ID')
        # System context avoids depending on an interactive connection/client.
        output = open(folder / 'exec_stdout.txt', 'w', encoding='utf-8')
        error = open(folder / 'exec_stderr.txt', 'w', encoding='utf-8')
        worker = subprocess.Popen([WSB, 'exec', '--id', owned, '-r', 'System', '-c',
                                   'C:\\Diagnostic\\guest.cmd', '--raw'],
                                  stdout=output, stderr=error, creationflags=subprocess.CREATE_NO_WINDOW)
        save(folder / 'worker.json', {'pid': worker.pid, 'dispatch_utc': utc()})
        guest_boot_deadline = min(deadline, time.monotonic() + 45)
        try:
            while True:
                if (folder / 'guest_result.json').exists():
                    outcome['reason'] = 'guest_complete'
                    break
                if time.monotonic() >= deadline:
                    outcome['reason'] = 'deadline'
                    break
                marker = 'stall_started.json' if mode == 'watchdog' else 'guest_started.json'
                if time.monotonic() >= guest_boot_deadline and not (folder / marker).exists():
                    outcome['reason'] = 'guest_start_failed'
                    break
                if worker.poll() is not None and worker.returncode != 0:
                    outcome['reason'] = 'exec_failed'
                    break
                time.sleep(.25)
        finally:
            save(folder / 'cleanup_requested.json', {'utc': utc(), 'reason': outcome['reason']})
            # Client termination alone is never treated as Sandbox termination.
            if worker.poll() is None:
                worker.terminate()
                try:
                    worker.wait(timeout=5)
                except subprocess.TimeoutExpired:
                    worker.kill()
                    worker.wait(timeout=5)
            save(folder / 'worker_exit.json', {'utc': utc(), 'exit_code': worker.returncode})
            output.close()
            error.close()
    except Exception as ex:
        outcome['error'] = repr(ex)
    finally:
        verified = False
        stop_ok = False
        for attempt in range(1, 4):
            result = cli(folder, f'stop_{attempt}', ['stop', '--id', owned, '--raw'], 20)
            stop_ok = result.get('exit_code') == 0
            listing = cli(folder, f'final_list_{attempt}', ['list', '--raw'], 10)
            try:
                data = json.loads(listing.get('stdout') or '')
                ids = [str(item['Id']).lower() for item in data['WindowsSandboxEnvironments']]
                verified = listing.get('exit_code') == 0 and owned.lower() not in ids
            except (ValueError, KeyError, TypeError):
                verified = False
            if verified:
                break
        outcome.update(end_utc=utc(), wall_seconds=round(time.monotonic()-started,3),
                       stop_exit_zero=stop_ok, exact_owned_id_absent=verified)
        if mode == 'watchdog':
            outcome['passed'] = (outcome['reason'] == 'deadline' and stop_ok and verified
                                 and (folder / 'stall_started.json').exists())
        else:
            outcome['passed'] = outcome['reason'] == 'guest_complete' and stop_ok and verified
        save(folder / 'supervisor_result.json', outcome)

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('action', choices=['launch', 'supervise'])
    parser.add_argument('--folder', required=True)
    parser.add_argument('--seconds', type=int, required=True)
    parser.add_argument('--mode', choices=['watchdog', 'prerequisite'], required=True)
    parser.add_argument('--media')
    args = parser.parse_args()
    folder = Path(args.folder).resolve()
    if not folder.is_dir() or (folder / 'ownership.json').exists():
        raise RuntimeError('Use an existing unique prepared folder without prior ownership')
    if args.action == 'launch':
        cmd = [sys.executable, str(Path(__file__).resolve()), 'supervise', '--folder', str(folder),
               '--seconds', str(args.seconds), '--mode', args.mode]
        if args.media:
            cmd += ['--media', args.media]
        with open(folder / 'supervisor_stdout.txt', 'w') as out, open(folder / 'supervisor_stderr.txt', 'w') as err:
            p = subprocess.Popen(cmd, stdin=subprocess.DEVNULL, stdout=out, stderr=err,
                                 creationflags=subprocess.DETACHED_PROCESS | subprocess.CREATE_NEW_PROCESS_GROUP,
                                 close_fds=True)
        save(folder / 'launcher_exit.json', {'utc': utc(), 'supervisor_pid': p.pid,
                                            'launcher_pid': os.getpid(), 'note': 'Launcher exits; detached supervisor owns deadline and cleanup'})
        print(json.dumps({'folder': str(folder), 'supervisor_pid': p.pid}))
    else:
        supervise(folder, args.seconds, args.mode, args.media)

if __name__ == '__main__':
    main()
