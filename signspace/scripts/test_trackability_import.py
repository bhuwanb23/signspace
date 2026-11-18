#!/usr/bin/env python3
"""Regression tests on SYNTHETIC probe data; these do not measure a headset."""
import json
from pathlib import Path
import subprocess
import sys
import tempfile
import unittest
ROOT = Path(__file__).resolve().parent.parent
DATA = ROOT / 'unity/Assets/StreamingAssets/trackability.json'
DOC = ROOT / 'docs/03-content/TRACKABILITY.md'
SCRIPT = ROOT / 'scripts/fill_trackability.py'
CHECK = ROOT / 'scripts/check_trackability.py'
class ImportTests(unittest.TestCase):
    def setUp(self):
        self.tmp = tempfile.TemporaryDirectory()
        self.d = Path(self.tmp.name)
        self.data = self.d / 'trackability.json'
        self.data.write_bytes(DATA.read_bytes())
        self.doc = self.d / 'trackability.md'
        self.doc.write_bytes(DOC.read_bytes())
        self.log = self.d / 'probe-daylight-dominant.jsonl'
        self.ids = [v['id'] for v in json.loads(self.data.read_text())['variants']]
    def tearDown(self): self.tmp.cleanup()
    def call(self, *args):
        return subprocess.run([sys.executable, str(SCRIPT), str(self.log), '--data',str(self.data),
                               '--doc',str(self.doc),'--date','2026-01-01',*args],capture_output=True,text=True)
    def row(self, shape, rate=.9):
        return {'shape':shape,'lockedFrames':rate,'holdSeconds':10}
    def test_partial_never_writes_either_file(self):
        self.log.write_text(json.dumps(self.row(self.ids[0]))+'\n')
        data, doc = self.data.read_bytes(), self.doc.read_bytes()
        result=self.call('--emit-doc')
        self.assertNotEqual(result.returncode,0,result.stdout)
        self.assertEqual(data,self.data.read_bytes())
        self.assertEqual(doc,self.doc.read_bytes())
    def test_bad_row_refused_not_silently_counted(self):
        cases=[{'shape':'B','holdSeconds':10},{'shape':'B','lockedFrames':2,'holdSeconds':10},
               {'shape':'B','lockedFrames':float('nan'),'holdSeconds':10},
               {'shape':'B','lockedFrames':True,'holdSeconds':10},
               {'shape':'B','lockedFrames':.8,'holdSeconds':5}]
        for row in cases:
            with self.subTest(row=row):
                self.log.write_text(json.dumps(row)+'\n')
                before=self.data.read_bytes()
                self.assertNotEqual(self.call().returncode,0)
                self.assertEqual(self.data.read_bytes(),before)
    def test_unknown_id_and_malformed_json_refused(self):
        for content in (json.dumps(self.row('NO_SUCH_SHAPE'))+'\n','{broken\n'):
            self.log.write_text(content)
            self.assertNotEqual(self.call().returncode,0)
    def test_complete_data_and_same_cell_repeats(self):
        rows=[self.row(id,.9) for id in self.ids]
        rows[0]=self.row(self.ids[0],.55)
        rows.append(self.row(self.ids[0],.85))
        self.log.write_text('\n'.join(map(json.dumps,rows))+'\n')
        result=self.call('--emit-doc')
        self.assertEqual(result.returncode,0,result.stdout+result.stderr)
        data=json.loads(self.data.read_text())
        self.assertTrue(data['measured'])
        row=data['variants'][0]
        self.assertTrue(row['borderline'])
        self.assertEqual(row['holdCount'],2)
        self.assertEqual(len(row['rates']),1)
        checked=subprocess.run([sys.executable,str(CHECK),'--data',str(self.data),'--doc',str(self.doc)],capture_output=True,text=True)
        self.assertEqual(checked.returncode,0,checked.stdout+checked.stderr)
    def test_straddling_repeats_do_not_require_mean_near_floor(self):
        rows=[self.row(id,.9) for id in self.ids]
        rows[0]=self.row(self.ids[0],.1)
        rows.extend(self.row(self.ids[0],1) for _ in range(9))
        self.log.write_text('\n'.join(map(json.dumps,rows))+'\n')
        result=self.call('--emit-doc')
        self.assertEqual(result.returncode,0,result.stdout+result.stderr)
        row=json.loads(self.data.read_text())['variants'][0]
        self.assertTrue(row['borderline'])
        self.assertGreater(row['lockRate'],.85)
        checked=subprocess.run([sys.executable,str(CHECK),'--data',str(self.data),'--doc',str(self.doc)],capture_output=True,text=True)
        self.assertEqual(checked.returncode,0,checked.stdout+checked.stderr)
    def test_invalid_date_refused_without_writing(self):
        self.log.write_text('\n'.join(json.dumps(self.row(id)) for id in self.ids)+'\n')
        before=self.data.read_bytes()
        result=self.call('--date','not-a-date')
        self.assertNotEqual(result.returncode,0)
        self.assertEqual(before,self.data.read_bytes())
    def test_low_rate_does_not_claim_unresolvable_camera(self):
        rows=[self.row(id,.2) for id in self.ids]
        self.log.write_text('\n'.join(map(json.dumps,rows))+'\n')
        self.assertEqual(self.call().returncode,0)
        self.assertEqual(json.loads(self.data.read_text())['variants'][0]['status'],'tracks-never-locks')
if __name__ == '__main__': unittest.main(verbosity=2)
