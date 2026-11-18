#!/usr/bin/env python3
"""Static path, Unity asset and code regressions; never a Unity compile or device test."""
from pathlib import Path
import re, unittest, urllib.parse
ROOT=Path(__file__).resolve().parent.parent
ASSETS=ROOT/'unity/Assets'
class Integrity(unittest.TestCase):
    def test_monobehaviour_names_match_files(self):
        wrong=[]
        for p in (ASSETS/'_Project').rglob('*.cs'):
            for name, bases in re.findall(r'public\s+(?:sealed\s+|abstract\s+)?class\s+(\w+)\s*:\s*([^\n{]+)',p.read_text()):
                if 'MonoBehaviour' in bases and name != p.stem: wrong.append((p,name))
        self.assertEqual(wrong,[])
    def test_component_guids_in_main_scene(self):
        scene=(ASSETS/'_Project/Scenes/Main.unity').read_text(encoding='utf-8')
        for file in ('Scripts/Core/AppStateMachine.cs.meta','Scripts/Spatial/MrUkSurfaceFinder.cs.meta'):
            meta=(ASSETS/'_Project'/file).read_text(encoding='utf-8')
            guid=re.search(r'(?m)^guid: (\w+)',meta).group(1)
            self.assertIn('m_Script: {fileID: 11500000, guid: '+guid+', type: 3}',scene)
        self.assertNotIn('m_Script: {fileID: 1157667494}',scene)
    def test_no_duplicate_asset_guids(self):
        seen={}
        for p in ASSETS.rglob('*.meta'):
            m=re.search(r'(?m)^guid: (\w+)',p.read_text(encoding='utf-8',errors='replace'))
            if m:
                self.assertNotIn(m.group(1),seen,f'duplicate: {p} and {seen.get(m.group(1))}')
                seen[m.group(1)]=p
    def test_state_back_reports_old_then_new(self):
        src=(ASSETS/'_Project/Scripts/Core/AppStateMachine.cs').read_text(encoding='utf-8')
        back=src.split('public void Back()',1)[1].split('public void UpdateHandsMissing',1)[0]
        self.assertIn('OnStateChanged?.Invoke(Previous, Current)',back)
    def test_mruk_v207_load_and_capture_default(self):
        src=(ASSETS/'_Project/Scripts/Spatial/MrukBoot.cs').read_text(encoding='utf-8')
        self.assertIn('mrukSingleton.LoadSceneFromDevice(requestSceneCaptureIfNoDataFound, true)',src)
        self.assertIn('public bool requestSceneCaptureIfNoDataFound = false;',src)
        self.assertIn('requestSceneCaptureIfNoDataFound: 0',(ASSETS/'_Project/Scenes/Main.unity').read_text(encoding='utf-8'))
    def test_trackability_panel_wording_matches_scripts(self):
        panel=(ASSETS/'_Project/Scripts/Matching/Trackability.cs').read_text(encoding='utf-8')
        for phrase in ('handshape variants met the lock-rate threshold — measured ',
                       'excluded by design, pending tracking measurements.'):
            self.assertIn(phrase,panel)
            for name in ('fill_trackability.py','check_trackability.py'):
                self.assertIn(phrase,(ROOT/'scripts'/name).read_text(encoding='utf-8'))
        self.assertNotIn('handshape variants are robustly trackable today',panel)
    def test_local_markdown_links(self):
        broken=[]
        skip={'Library','Temp','obj','Builds','Logs','UserSettings','.git','node_modules','PackageCache'}
        for p in ROOT.rglob('*.md'):
            if skip & set(p.parts): continue
            text=re.sub(r'```.*?```','',p.read_text(encoding='utf-8',errors='replace'),flags=re.S)
            for m in re.finditer(r'(?<!!)\[[^\]\n]+\]\(([^\s)]+)(?:\s+"[^"]*")?\)',text):
                v=m.group(1).strip('<>')
                if re.match(r'\w+://|mailto:|#',v):continue
                v=urllib.parse.unquote(v.split('#')[0].split('?')[0])
                if v and '{' not in v and '<' not in v and not (p.parent/v).exists():broken.append((str(p),v))
        self.assertEqual(broken,[])
if __name__=='__main__': unittest.main(verbosity=2)
