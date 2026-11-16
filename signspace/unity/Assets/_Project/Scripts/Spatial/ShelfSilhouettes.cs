using System.Collections.Generic;
using UnityEngine;

namespace Signspace.Spatial
{
    /// <summary>
    /// One placeholder silhouette per shape FAMILY, not per shape (P4.2 step 1).
    ///
    /// Six unique models on day five is three hours of modelling for a screen that shows them at 16 cm,
    /// arm's length, in passthrough. What the object has to do is read as *the shape you learned*, so the
    /// recipe is a silhouette plus one parameter that carries the difference inside a family:
    ///
    ///   fist   B            palm + thumb across it
    ///   pair   U, V         palm + two fingers - the spread IS the difference between U and V
    ///   triple W            palm + three fingers
    ///   curve  C, O         an arc of segments - the gap IS the difference between C and O
    ///
    /// Every shape is ONE mesh, built once and cached. That is the whole draw-call story: an object
    /// is one renderer, so twelve objects are twelve draw calls, not fifty.
    ///
    /// The meshes are built from box primitives at runtime rather than imported: a box is 12 triangles and
    /// it is the only shape whose triangle count does not depend on a Unity version. `scripts/shelf_budget.py`
    /// reads the recipes below out of this file, so the budget in `phases/p4-shelf/budget-check.md` is
    /// arithmetic on the shipped numbers rather than a memory of them.
    /// </summary>
    public static class ShelfSilhouettes
    {
        /// <summary>The boxes each shape is made of, in local metres. Read by the budget script.</summary>
        public const int BoxesFist = 2;     // palm, thumb
        public const int BoxesPair = 3;     // palm, finger, finger
        public const int BoxesTriple = 4;   // palm, finger, finger, finger
        public const int BoxesCurve = 6;    // palm, five arc segments
        public const int TrianglesPerBox = 12;

        static readonly Dictionary<string, Mesh> Cache = new Dictionary<string, Mesh>();

        public static string FamilyOf(string shapeId)
        {
            switch ((shapeId ?? "").ToUpperInvariant())
            {
                case "B": return "fist";
                case "U":
                case "V": return "pair";
                case "W": return "triple";
                case "C":
                case "O": return "curve";
                default: return "fist";     // an unknown shape still gets an object rather than nothing
            }
        }

        public static int BoxesFor(string shapeId)
        {
            switch (FamilyOf(shapeId))
            {
                case "pair": return BoxesPair;
                case "triple": return BoxesTriple;
                case "curve": return BoxesCurve;
                default: return BoxesFist;
            }
        }

        public static int TriangleCount(string shapeId) => BoxesFor(shapeId) * TrianglesPerBox;

        /// <summary>
        /// The colour is per family, and the difference inside a family is carried by geometry: two shapes
        /// that differ only in colour would be a legend, not a silhouette.
        /// </summary>
        public static Color ColourFor(string shapeId)
        {
            switch (FamilyOf(shapeId))
            {
                case "pair": return new Color(0.16f, 0.65f, 0.63f);     // teal, the Close colour
                case "triple": return new Color(0.45f, 0.58f, 0.78f);
                case "curve": return new Color(0.95f, 0.89f, 0.76f);    // the locked tone
                default: return new Color(0.78f, 0.46f, 0.16f);          // warming amber
            }
        }

        /// <summary>The mesh for a shape, built on first use and shared by every object of that shape.</summary>
        public static Mesh For(string shapeId)
        {
            string key = (shapeId ?? "?").ToUpperInvariant();
            if (Cache.TryGetValue(key, out Mesh cached) && cached != null) return cached;

            Mesh mesh = Build(FamilyOf(key), key);
            Cache[key] = mesh;
            return mesh;
        }

        static Mesh Build(string family, string shapeId)
        {
            // A unit box at 12 triangles, placed by translation/rotation/scale. Combined into one mesh.
            var parts = new List<CombineInstance>();
            Add(parts, new Vector3(0f, 0f, 0f), new Vector3(0.060f, 0.045f, 0.016f));   // palm

            switch (family)
            {
                case "fist":
                    Add(parts, new Vector3(0f, 0.004f, 0.026f), new Vector3(0.064f, 0.016f, 0.014f));
                    break;

                case "pair":
                {
                    // U's fingers touch, V's are apart: one number, read from the shape id.
                    float spread = shapeId == "V" ? 0.020f : 0.000f;
                    Add(parts, new Vector3(-0.009f - spread * 0.5f, 0.038f, 0.004f), new Vector3(0.016f, 0.062f, 0.014f));
                    Add(parts, new Vector3(0.009f + spread * 0.5f, 0.038f, 0.004f), new Vector3(0.016f, 0.062f, 0.014f));
                    break;
                }

                case "triple":
                    Add(parts, new Vector3(-0.016f, 0.034f, 0.004f), new Vector3(0.014f, 0.056f, 0.014f));
                    Add(parts, new Vector3(0.000f, 0.040f, 0.004f), new Vector3(0.014f, 0.064f, 0.014f));
                    Add(parts, new Vector3(0.016f, 0.030f, 0.004f), new Vector3(0.014f, 0.050f, 0.014f));
                    break;

                case "curve":
                {
                    // An arc, segment by segment. C stops short of the thumb; O closes onto it.
                    float sweep = shapeId == "O" ? 300f : 230f;
                    const int segments = 5;
                    float radius = 0.024f;
                    for (int i = 0; i < segments; i++)
                    {
                        float t = (i + 0.5f) / segments;
                        float a = Mathf.Lerp(-90f, -90f + sweep, t) * Mathf.Deg2Rad;
                        var at = new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f);
                        Add(parts, at, new Vector3(0.014f, 0.014f, 0.016f), Quaternion.Euler(0f, 0f, (a * Mathf.Rad2Deg) + 90f));
                    }
                    break;
                }
            }

            var combined = new CombineInstance[parts.Count];
            for (int i = 0; i < parts.Count; i++) combined[i] = parts[i];

            var mesh = new Mesh { name = "ShelfSilhouette_" + shapeId };
            mesh.CombineMeshes(combined, true, true);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Destroy in play mode, destroy immediately in the editor - the builder calls For().</summary>
        static void Kill(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Object.Destroy(o); else Object.DestroyImmediate(o);
        }

        static void Add(List<CombineInstance> parts, Vector3 at, Vector3 size, Quaternion rotation = default)
        {
            if (rotation == default) rotation = Quaternion.identity;

            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var probe = go.GetComponent<MeshFilter>();
            Mesh box = probe != null ? probe.sharedMesh : null;

            parts.Add(new CombineInstance
            {
                mesh = box,
                transform = Matrix4x4.TRS(at, rotation, size)
            });

            // CreatePrimitive also made a collider; on the probe object it is pure waste.
            var collider = go.GetComponent<Collider>();
            if (collider != null) Kill(collider);
            Kill(go);
        }
    }
}
