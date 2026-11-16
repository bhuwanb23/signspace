using System.IO;
using UnityEditor;
using UnityEngine;

namespace Signspace.Editor
{
    /// <summary>
    /// Builds the plinth as a designed object, not a placeholder (P4.4).
    ///
    /// A judge on a sofa, a bed or an airplane tray must never see "no surface detected". They see a
    /// platform that catches light, has an edge and drops a shadow - the same visual weight as a real
    /// table - with the shelf standing on it at exactly the height the shelf would have had.
    ///
    /// The prefab's local Y=0 is its TOP surface, so instantiating it 1 cm under the shelf plate puts
    /// the plate in contact with it. The 4° tilt is deliberate: a perfectly level slab reads as UI,
    /// a slightly tilted one reads as an object somebody put there.
    ///
    /// The gradient and the shadow are generated textures, saved as assets, because a gradient that
    /// only exists in a builder's memory would vanish from the build.
    /// </summary>
    public static class PlinthBuilder
    {
        public const string PrefabPath = "Assets/_Project/Prefabs/Plinth.prefab";
        const string MaterialsFolder = "Assets/_Project/Materials";
        const string GradientPath = MaterialsFolder + "/PlinthGradient.png";
        const string ShadowPath = MaterialsFolder + "/PlinthShadow.png";

        [MenuItem("SIGNSPACE/P4.4 Build plinth prefab")]
        public static void BuildMenu() { Build(); }

        public static GameObject Build()
        {
            BuildMaterials.EnsureFolder("Assets/_Project/Prefabs");
            BuildMaterials.EnsureFolder(MaterialsFolder);

            var root = new GameObject("Plinth");
            root.transform.rotation = Quaternion.Euler(4f, 0f, 0f);   // a slab, not a UI panel

            // ---- the top: a soft gradient that reads as light falling across a surface ----------
            var top = GameObject.CreatePrimitive(PrimitiveType.Cube);
            top.name = "PlinthTop";
            top.transform.SetParent(root.transform, false);
            top.transform.localScale = new Vector3(0.46f, 0.02f, 0.14f);
            top.transform.localPosition = new Vector3(0f, -0.01f, 0f);   // top surface at y = 0
            var topRenderer = top.GetComponent<Renderer>();
            var topMaterial = BuildMaterials.Get(MaterialsFolder + "/PlinthTop.mat",
                                                 new Color(0.34f, 0.32f, 0.30f));
            SetBaseMap(topMaterial, GradientTexture());
            if (topRenderer != null) topRenderer.sharedMaterial = topMaterial;

            // ---- the edge: a darker lip, so the platform has thickness ---------------------------
            var edge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            edge.name = "PlinthEdge";
            edge.transform.SetParent(root.transform, false);
            edge.transform.localScale = new Vector3(0.44f, 0.014f, 0.122f);
            edge.transform.localPosition = new Vector3(0f, -0.022f, 0f);
            var edgeRenderer = edge.GetComponent<Renderer>();
            var edgeMaterial = BuildMaterials.Get(MaterialsFolder + "/PlinthEdge.mat",
                                                  new Color(0.16f, 0.15f, 0.14f));
            if (edgeRenderer != null) edgeRenderer.sharedMaterial = edgeMaterial;

            // ---- the shadow: what makes it sit on the floor instead of float ---------------------
            var shadow = GameObject.CreatePrimitive(PrimitiveType.Quad);
            shadow.name = "PlinthShadow";
            shadow.transform.SetParent(root.transform, false);
            shadow.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            shadow.transform.localScale = new Vector3(0.68f, 0.30f, 1f);
            shadow.transform.localPosition = new Vector3(0f, -0.0295f, 0.01f);
            var shadowCollider = shadow.GetComponent<Collider>();
            if (shadowCollider != null) Object.DestroyImmediate(shadowCollider);
            var shadowRenderer = shadow.GetComponent<Renderer>();
            var shadowMaterial = MakeTransparent(MaterialsFolder + "/PlinthShadow.mat",
                                                 new Color(0f, 0f, 0f, 0.55f));
            SetBaseMap(shadowMaterial, ShadowTexture());
            // Set the queue last: importing the texture above can reimport the material and reset a
            // custom queue to the shader's default, which would leave an opaque black quad on the
            // floor. Re-assert it, save, then assert again. An opaque shadow is worse than none.
            if (shadowMaterial != null)
            {
                shadowMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                EditorUtility.SetDirty(shadowMaterial);
                AssetDatabase.SaveAssets();
                if (shadowMaterial.renderQueue < 3000)
                    Debug.LogWarning("[PlinthBuilder] the shadow material kept queue " +
                                     shadowMaterial.renderQueue + "; it may render opaque.");
            }
            if (shadowRenderer != null) shadowRenderer.sharedMaterial = shadowMaterial;

            // ---- a dim seam on the front lip: the one line that catches the light ----------------
            var seam = GameObject.CreatePrimitive(PrimitiveType.Cube);
            seam.name = "PlinthSeam";
            seam.transform.SetParent(root.transform, false);
            seam.transform.localScale = new Vector3(0.44f, 0.0015f, 0.0015f);
            seam.transform.localPosition = new Vector3(0f, -0.0135f, 0.0705f);
            var seamCollider = seam.GetComponent<Collider>();
            if (seamCollider != null) Object.DestroyImmediate(seamCollider);
            var seamRenderer = seam.GetComponent<Renderer>();
            var seamMaterial = BuildMaterials.Get(MaterialsFolder + "/PlinthSeam.mat",
                                                  new Color(0.62f, 0.58f, 0.52f));
            if (seamRenderer != null) seamRenderer.sharedMaterial = seamMaterial;

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PlinthBuilder] " + PrefabPath + " built: 4 parts, top surface at local y=0, 4° tilt");
            return prefab;
        }

        static void SetBaseMap(Material material, Texture2D texture)
        {
            if (material == null || texture == null) return;
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
            EditorUtility.SetDirty(material);
        }

        /// <summary>Unlit + transparent so the shadow darkens the floor without glowing or sorting wrong.</summary>
        static Material MakeTransparent(string path, Color color)
        {
            var material = BuildMaterials.Get(path, color);
            if (material == null) return null;

            material.SetOverrideTag("RenderType", "Transparent");
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
            if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
            if (material.HasProperty("_Cull")) material.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_ALPHATEST_ON");
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            EditorUtility.SetDirty(material);
            return material;
        }

        // ---- generated textures ---------------------------------------------------------------
        static Texture2D GradientTexture()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(GradientPath);
            if (existing != null) return existing;

            const int w = 64, h = 64;
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
            {
                // light at the front (v=0), falling off toward the back, with a soft vignette at the ends
                float v = y / (float)(h - 1);
                for (int x = 0; x < w; x++)
                {
                    float u = x / (float)(w - 1);
                    float edgeX = Mathf.SmoothStep(0f, 1f, Mathf.Min(u, 1f - u) * 6f);
                    float shade = Mathf.Lerp(0.62f, 1f, 1f - v) * Mathf.Lerp(0.72f, 1f, edgeX);
                    texture.SetPixel(x, y, new Color(shade, shade * 0.98f, shade * 0.95f, 1f));
                }
            }
            texture.Apply();
            File.WriteAllBytes(GradientPath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(GradientPath);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(GradientPath);
        }

        static Texture2D ShadowTexture()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(ShadowPath);
            if (existing != null) return existing;

            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (x / (float)(size - 1)) * 2f - 1f;
                    float v = (y / (float)(size - 1)) * 2f - 1f;
                    float r = Mathf.Sqrt(u * u + v * v);
                    float alpha = Mathf.Pow(Mathf.Clamp01(1f - r), 2.2f) * 0.85f;
                    texture.SetPixel(x, y, new Color(0f, 0f, 0f, alpha));
                }
            }
            texture.Apply();
            File.WriteAllBytes(ShadowPath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(ShadowPath);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(ShadowPath);
        }
    }
}
