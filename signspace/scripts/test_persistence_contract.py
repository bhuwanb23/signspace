#!/usr/bin/env python3
"""Static contract regression checks; Unity editor/device tests are separate and still required."""
import pathlib
import re
import unittest
ROOT = pathlib.Path(__file__).resolve().parent.parent
P = ROOT / 'unity/Assets/_Project'
def read(name): return (P / name).read_text()
class Contract(unittest.TestCase):
    def test_schema_data_and_legacy_migration(self):
        s = read('Scripts/Core/SaveSystem.cs')
        for pattern in (r'public int schema = 3;', r'public string layoutVariant',
                        r'public List<ShapeUnlock> unlockRecords', r'unlockedUtc = DateTime.UtcNow',
                        r'oldSchema >= 2', r'unlockedUtc = ""'):
            self.assertRegex(s, pattern)
        self.assertIn('header.schema > CurrentSchema', s)
        self.assertIn('WritesBlocked = true', s)
        self.assertIn('if (WritesBlocked) { LastSaveStatus', s)
    def test_no_destructive_rebind_or_world_restore(self):
        s = read('Scripts/Core/SaveSystem.cs')
        shelf = read('Scripts/Spatial/ShelfAnchor.cs')
        self.assertNotIn('Data.unlocked.Clear()', s)
        self.assertIn('refusing to overwrite an existing shelf placement', s)
        self.assertIn('transform.localPosition = save.Data.localPos', shelf)
        self.assertIn('transform.SetParent(anchorTransform, false)', shelf)
        self.assertNotRegex(shelf, r'transform\.position\s*=\s*save\.Data\.lastWorldPos')
    def test_current_room_precedes_anchor(self):
        s = read('Scripts/Spatial/ShelfAnchor.cs')
        body = s[s.index('void ResolveForCurrentRoom()'):s.index('void Update()', s.index('void ResolveForCurrentRoom()'))]
        self.assertLess(body.index('TryGetCurrentRoomId'), body.index('TryResolve'))
        self.assertLess(body.index('save.Data.roomId'), body.index('TryResolve'))
        for state in ('RoomNotFound', 'DifferentRoom', 'AnchorNotFound', 'UnsupportedLayout'):
            self.assertIn('Outcome.' + state, body)
        finder = read('Scripts/Spatial/SurfaceFinder.cs')
        body = finder[finder.index('public bool TryResolve('):finder.index('static string AnchorUuidOf')]
        self.assertIn('GetCurrentRoom()', body)
        self.assertNotIn('GetAnchors()', body)
        self.assertNotIn('nameMatch', body)
    def test_transition_monitor_and_editor_checks_exist(self):
        shelf = read('Scripts/Spatial/ShelfAnchor.cs')
        self.assertIn('LogLaunch("room-transition")', shelf)
        self.assertIn('Result == Outcome.AnchorNotFound', shelf)
        self.assertIn('SIGNSPACE/P4.3 Check persistence', read('Editor/PersistenceP43Checks.cs'))

if __name__ == '__main__': unittest.main(verbosity=2)
