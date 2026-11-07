import importlib.util
import json
from pathlib import Path
import subprocess
import tempfile
import unittest
from unittest.mock import patch

spec = importlib.util.spec_from_file_location('supervisor', Path(__file__).with_name('sandbox_diagnostic_supervisor.py'))
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)

class CleanupTests(unittest.TestCase):
    def test_start_failure_still_stops_only_preassigned_id_and_checks_list(self):
        calls = []
        def fake_cli(folder, label, args, timeout):
            calls.append((label,args,timeout))
            if label == 'start':
                return {'exit_code': None, 'timed_out': True}
            if label.startswith('stop'):
                return {'exit_code': 0}
            return {'exit_code': 0, 'stdout': json.dumps({'WindowsSandboxEnvironments':[{'Id':'unrelated-environment'}]})}
        with tempfile.TemporaryDirectory() as root, patch.object(module, 'cli', side_effect=fake_cli):
            folder = Path(root)
            module.supervise(folder, 10, 'watchdog', None)
            ownership = json.loads((folder/'ownership.json').read_text())
            result = json.loads((folder/'supervisor_result.json').read_text())
        self.assertEqual(calls[1][1], ['stop','--id',ownership['sandbox_id'],'--raw'])
        self.assertTrue(result['exact_owned_id_absent'])
        self.assertFalse(result['passed'])

    def test_failed_or_unparseable_list_never_claims_absence(self):
        def fake_cli(folder,label,args,timeout):
            return {'exit_code':1,'stdout':'not json'}
        with tempfile.TemporaryDirectory() as root, patch.object(module,'cli',side_effect=fake_cli) as mocked:
            folder=Path(root)
            module.supervise(folder,10,'watchdog',None)
            result=json.loads((folder/'supervisor_result.json').read_text())
        self.assertFalse(result['exact_owned_id_absent'])
        self.assertFalse(result['passed'])
        self.assertEqual(sum(call.args[1].startswith('stop') for call in mocked.call_args_list),3)

    def test_cli_timeout_is_preserved(self):
        with tempfile.TemporaryDirectory() as root, patch.object(module.subprocess,'run',side_effect=subprocess.TimeoutExpired('wsb',1,output=b'partial')):
            result=module.cli(Path(root),'timeout',['list','--raw'],1)
            self.assertTrue((Path(root)/'timeout.json').exists())
        self.assertTrue(result['timed_out'])
        self.assertIsNone(result['exit_code'])
        self.assertEqual(result['stdout'],'partial')

if __name__=='__main__':
    unittest.main()
