using System.Collections.Generic;
using Signspace.Matching;
using Signspace.Spatial;
using UnityEditor;
using UnityEngine;

namespace Signspace.Editor
{
    /// <summary>
    /// Desk proof for the three P4.2 boxes that were checked "implemented" but never measured:
    /// objects do not overlap in their slots, the next empty slot is visible from launch one, and
    /// twelve objects stay inside the draw-call / triangle budget.
    ///
    /// It builds the shelf's real objects from the shipped prefab and the shipped silhouette
    /// meshes, at the real slot spacing, and reads the numbers back - rather than being satisfied
    /// by the presence of code that is supposed to produce them.
    ///
    /// Run from the menu, or in batch:
    ///   Unity -batchmode -quit -executeMethod Signspace.Editor.ShelfCheck.Run
    /// </summary>
    public static class ShelfCheck
    {
        static readonly string[] Shapes = { "B", "U", "V", "W", "C", "O" };
        const string PrefabPath = "Assets/_Project/Prefabs/ShelfObject.prefab";

        [MenuItem("SIGNSPACE/P4.2 Shelf check")]
        public static void Run()
        {
            var root = new GameObject("ShelfCheckRoot");
            var material = new Material(Shader.Find("Unlit/Color"));
            bool ok = true;

            try
            {
                // ---- the shelf, configured exactly as PersistenceBuilder configures it -----------
                var slots = new GameObject("Slots");
                slots.transform.SetParent(root.transform, false);

                var anchor = root.AddComponent<ShelfAnchor>();
                anchor.slotRoot = slots.transform;
                anchor.slotCount = 12;
                anchor.slotColumns = 6;
                anchor.slotSpacing = 0.075f;
                anchor.slotRowSpacing = 0.065f;
                anchor.slotEmptyMaterial = material;
                anchor.slotMarkerScale = 0.05f;

                var saveGo = new GameObject("SaveSystem");
                saveGo.transform.SetParent(root.transform, false);
                var save = saveGo.AddComponent<SaveSystem>();
                save.Data.unlocked.Clear();
                save.Data.unlockRecords.Clear();
                anchor.save = save;

                // ---- 1. the slot grid itself ------------------------------------------------------
                var positions = new List<Vector3>();
                for (int i = 0; i < anchor.slotCount; i++)
                {
                    Transform slot = anchor.SlotTransform(i);
                    if (slot == null)
                    {
                        Debug.LogError("[ShelfCheck] slot " + i + " was not created");
                        ok = false;
                        continue;
                    }

                    var local = slot.localPosition;
                    if (positions.Exists(p => Vector3.Distance(p, local) < 0.001f))
                    {
                        Debug.LogError("[ShelfCheck] slot " + i + " shares a position with another slot");
                        ok = false;
                    }
                    positions.Add(local);
                }

                float gridWidth = 0f, gridDepth = 0f;
                foreach (var p in positions)
                {
                    gridWidth = Mathf.Max(gridWidth, Mathf.Abs(p.x) * 2f + anchor.slotMarkerScale);
                    gridDepth = Mathf.Max(gridDepth, Mathf.Abs(p.z) * 2f + anchor.slotMarkerScale);
                }

                // The plate is 0.50 x 0.16 m (PersistenceBuilder). A grid that overhangs it would put
                // objects in the air at the ends of the shelf.
                bool gridFits = gridWidth <= 0.50f && gridDepth <= 0.16f;
                Debug.Log("[ShelfCheck] slot grid " + gridWidth.ToString("F3") + " x " + gridDepth.ToString("F3") +
                          " m inside a 0.500 x 0.160 m plate (need <=) " + (gridFits ? "PASS" : "FAIL"));
                ok &= gridFits;
                ok &= positions.Count == anchor.slotCount;

                // ---- 2. every silhouette fits its cell -------------------------------------------
                float widestX = 0f, deepestZ = 0f;
                foreach (string id in Shapes)
                {
                    Mesh mesh = ShelfSilhouettes.For(id);
                    if (mesh == null)
                    {
                        Debug.LogError("[ShelfCheck] no mesh for " + id);
                        ok = false;
                        continue;
                    }
                    widestX = Mathf.Max(widestX, mesh.bounds.size.x);
                    deepestZ = Mathf.Max(deepestZ, mesh.bounds.size.z);

                    int actual = mesh.triangles.Length / 3;
                    int recipe = ShelfSilhouettes.TriangleCount(id);
                    if (actual != recipe)
                    {
                        Debug.LogError("[ShelfCheck] " + id + " recipe says " + recipe + " triangles, mesh has " +
                                       actual + " - the budget script would under-count.");
                        ok = false;
                    }
                }

                bool cellsFit = widestX <= anchor.slotSpacing && deepestZ <= anchor.slotRowSpacing;
                Debug.Log("[ShelfCheck] widest silhouette " + widestX.ToString("F3") + " x " + deepestZ.ToString("F3") +
                          " m in a " + anchor.slotSpacing.ToString("F3") + " x " + anchor.slotRowSpacing.ToString("F3") +
                          " m cell (need <=) " + (cellsFit ? "PASS" : "FAIL"));
                ok &= cellsFit;

                // ---- 3. the empty-slot affordance, at launch one and later ----------------------
                anchor.RefreshSlots();
                ok &= CheckMarkers(anchor, 0, "launch 1 (nothing unlocked)");

                save.Data.unlocked.Add("B");
                save.Data.unlocked.Add("U");
                save.Data.unlocked.Add("V");
                anchor.RefreshSlots();
                ok &= CheckMarkers(anchor, 3, "three unlocked");

                // A full shelf is the budget's worst case: twelve occupied slots show twelve markers.
                // Add shapes rather than calling a private unlock path, so the count comes from the
                // same RefreshSlots a real session uses.
                foreach (string id in Shapes) save.Data.unlocked.Add(id);
                foreach (string id in Shapes) save.Data.unlocked.Add(id);
                anchor.RefreshSlots();

                // ---- 4. twelve objects, from the shipped prefab ---------------------------------
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                if (prefab == null)
                {
                    Debug.LogError("[ShelfCheck] " + PrefabPath + " did not load - no object can be tested.");
                    ok = false;
                }
                else
                {
                    var objects = new List<GameObject>();
                    var shapesBySlot = new List<string>();
                    for (int i = 0; i < anchor.slotCount; i++)
                    {
                        string shape = Shapes[i % Shapes.Length];
                        Transform slot = anchor.SlotTransform(i);

                        var go = Object.Instantiate(prefab, slot, false);
                        var view = go.GetComponent<ShelfObjectView>();
                        if (view == null)
                        {
                            Debug.LogError("[ShelfCheck] prefab has no ShelfObjectView");
                            ok = false;
                            break;
                        }
                        view.objectMaterial = material;
                        view.SetShape(shape);
                        go.transform.localPosition = Vector3.up * anchor.ObjectLift(shape);
                        go.transform.localRotation = Quaternion.identity;
                        go.transform.localScale = Vector3.one;
                        objects.Add(go);
                        shapesBySlot.Add(shape);
                    }

                    ok &= CheckOverlap(objects, shapesBySlot);
                    ok &= CheckCounts(objects, anchor);
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(material);
            }

            Debug.Log("[ShelfCheck] RESULT: " + (ok ? "PASS" : "FAIL"));
            if (Application.isBatchMode && !ok) EditorApplication.Exit(1);
        }

        /// <summary>
        /// One dimmed marker at the next empty slot, brighter markers behind it, nothing beyond it.
        /// The colour is read from the renderer's property block because that is what the user sees -
        /// a marker that is merely active but not dimmed is not the affordance the task asks for.
        /// </summary>
        static bool CheckMarkers(ShelfAnchor anchor, int unlocked, string label)
        {
            int active = 0, dim = 0, bright = 0;
            for (int i = 0; i < anchor.slotCount; i++)
            {
                Transform slot = anchor.SlotTransform(i);
                Transform marker = slot != null ? slot.Find("Marker") : null;
                if (marker == null || !marker.gameObject.activeSelf) continue;
                active++;

                var renderer = marker.GetComponent<Renderer>();
                if (renderer == null) continue;

                var block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                Color c = renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty("_Color")
                    ? block.GetColor("_Color")
                    : Color.clear;

                // 0.30/0.32/0.35 is the dim "next empty" tint; 0.58/0.60/0.64 is a filled slot.
                if (Mathf.Abs(c.r - 0.30f) < 0.02f && Mathf.Abs(c.g - 0.32f) < 0.02f) { dim++; if (i != unlocked) return Fail(label, "dim marker at slot " + i + ", expected " + unlocked); }
                else if (Mathf.Abs(c.r - 0.58f) < 0.02f && Mathf.Abs(c.g - 0.60f) < 0.02f) bright++;
            }

            bool ok = active == unlocked + 1 && dim == 1 && bright == unlocked;
            Debug.Log("[ShelfCheck] markers, " + label + ": " + active + " visible (" + dim + " dim, " + bright +
                      " filled), expected " + (unlocked + 1) + " with 1 dim " + (ok ? "PASS" : "FAIL"));
            return ok;
        }

        static bool Fail(string label, string why)
        {
            Debug.LogError("[ShelfCheck] markers, " + label + ": " + why);
            return false;
        }

        /// <summary>No two resting objects share a cubic millimetre of space.</summary>
        static bool CheckOverlap(List<GameObject> objects, List<string> shapes)
        {
            int overlaps = 0;
            for (int i = 0; i < objects.Count; i++)
            {
                var a = objects[i].GetComponent<Renderer>();
                if (a == null) continue;
                for (int j = i + 1; j < objects.Count; j++)
                {
                    var b = objects[j].GetComponent<Renderer>();
                    if (b == null) continue;
                    if (!a.bounds.Intersects(b.bounds)) continue;

                    overlaps++;
                    if (overlaps <= 3)
                        Debug.LogError("[ShelfCheck] " + shapes[i] + " (slot " + i + ") overlaps " + shapes[j] +
                                       " (slot " + j + ")");
                }
            }

            Debug.Log("[ShelfCheck] overlapping object pairs: " + overlaps + " of 66 (need 0) " +
                      (overlaps == 0 ? "PASS" : "FAIL"));
            return overlaps == 0;
        }

        /// <summary>
        /// The twelve-object shelf against the P7 ceilings, counted from objects rather than recipes:
        /// one draw call per renderer and one material per unique shared material, which is the
        /// pessimistic reading.
        /// </summary>
        static bool CheckCounts(List<GameObject> objects, ShelfAnchor anchor)
        {
            var materials = new HashSet<Material>();
            int renderers = 0, triangles = 0;
            foreach (var go in objects)
            {
                var renderer = go.GetComponent<Renderer>();
                if (renderer == null) continue;
                renderers++;
                if (renderer.sharedMaterial != null) materials.Add(renderer.sharedMaterial);

                var filter = go.GetComponent<MeshFilter>();
                if (filter != null && filter.sharedMesh != null)
                    triangles += filter.sharedMesh.triangles.Length / 3;
            }

            // markers: one renderer each, sharing the empty material
            int markers = 0;
            for (int i = 0; i < anchor.slotCount; i++)
            {
                Transform slot = anchor.SlotTransform(i);
                Transform marker = slot != null ? slot.Find("Marker") : null;
                if (marker == null || !marker.gameObject.activeSelf) continue;
                markers++;
                var renderer = marker.GetComponent<Renderer>();
                if (renderer != null && renderer.sharedMaterial != null) materials.Add(renderer.sharedMaterial);
            }

            // +1 plate, +1 plinth from the shelf rig
            int drawCalls = renderers + markers + 2;
            int materialCount = materials.Count + 1;    // the plate/plinth share BuildMaterials

            // Worst case is not the average: twelve copies of the heaviest silhouette.
            int worstShape = 0;
            foreach (string id in Shapes)
            {
                Mesh mesh = ShelfSilhouettes.For(id);
                if (mesh != null) worstShape = Mathf.Max(worstShape, mesh.triangles.Length / 3);
            }
            int worstTriangles = worstShape * anchor.slotCount;

            bool ok = drawCalls <= 120 && triangles <= 150000 && materialCount <= 12 &&
                      worstTriangles <= 150000;
            Debug.Log("[ShelfCheck] 12 objects: " + drawCalls + " draw calls (<= 120), " + triangles +
                      " triangles as loaded (worst case " + worstTriangles + " <= 150000), " +
                      materialCount + " materials (<= 12) " + (ok ? "PASS" : "FAIL"));
            return ok;
        }
    }
}
