#!/usr/bin/env python3
"""Static regression checks for the shelf integration; not a Unity or device test."""
from pathlib import Path
import re
import subprocess
import sys
import unittest

ROOT = Path(__file__).resolve().parent.parent
PROJECT = ROOT / 'unity/Assets/_Project'
def text(path):
    return (PROJECT / path).read_text()

class ShelfWiring(unittest.TestCase):
    def test_arc_matches_prefab_builder_and_previs(self):
        source = text('Scripts/Core/LockPayoff.cs')
        prefab = text('Prefabs/LockPayoff.prefab')
        builder = text('Editor/LockPayoffBuilder.cs')
        previs = (ROOT / 'scripts/payoff_previs.py').read_text()
        self.assertRegex(source, r'arcSeconds\s*=\s*0\.50f')
        self.assertRegex(prefab, r'(?m)^  arcSeconds:\s*0\.5\s*$')
        self.assertRegex(builder, r'arcSeconds\s*=\s*0\.50f')
        self.assertRegex(previs, r'(?m)^ARC\s*=\s*0\.50\b')

    def test_scene_build_order_and_material(self):
        scene = text('Editor/SceneBuilder.cs')
        builder = text('Editor/PersistenceBuilder.cs')
        self.assertLess(scene.index('PersistenceBuilder.Build(includeProbe: false)'),
                        scene.index('payoff.shelf ='))
        self.assertIn('payoff.flyFrom = refGo.transform;', scene)
        self.assertIn('anchor.objectMaterial = BuildMaterials.Get(', builder)
        self.assertIn('anchor.objectPrefab = AssetDatabase.LoadAssetAtPath', builder)

    def test_prefab_view_guid_and_components(self):
        prefab = text('Prefabs/ShelfObject.prefab')
        meta = text('Scripts/Spatial/ShelfObjectView.cs.meta')
        guid = re.search(r'(?m)^guid: ([0-9a-f]{32})$', meta).group(1)
        self.assertIn('guid: ' + guid, prefab)
        self.assertIn('MeshFilter:', prefab)
        self.assertIn('MeshRenderer:', prefab)
        self.assertIn('objectMaterial: {fileID: 0}', prefab)  # wired by builder/anchor

    def test_single_idle_writer_and_full_shelf_guard(self):
        view = text('Scripts/Spatial/ShelfObjectView.cs')
        anchor = text('Scripts/Spatial/ShelfAnchor.cs')
        payoff = text('Scripts/Core/LockPayoff.cs')
        self.assertNotIn('void OnEnable()', view)
        self.assertIn('return index >= slotCount ? -1 : index;', anchor)
        self.assertIn('ClearSlot(index, go);', anchor)
        self.assertIn('RegisterIdle(go);', anchor)
        self.assertIn('shelf.Adopt(obj, shapeId)', payoff)

    def test_budget_positive_and_negative(self):
        script = ROOT / 'scripts/shelf_budget.py'
        for args, expected in [(['--check'], 0), (['--check', '--slots', '200'], 1)]:
            result = subprocess.run([sys.executable, str(script), *args], capture_output=True, text=True)
            self.assertEqual(result.returncode, expected, result.stdout + result.stderr)

if __name__ == '__main__':
    unittest.main(verbosity=2)
