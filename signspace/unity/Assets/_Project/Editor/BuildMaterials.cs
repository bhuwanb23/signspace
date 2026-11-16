using UnityEditor;
using UnityEngine;

namespace Signspace.Editor
{
    /// <summary>
    /// Materials the builders need, created on demand and reused afterwards.
    ///
    /// Builders exist so that one command produces a scene that runs; a builder that references a
    /// material nobody has created yet produces a scene with a magenta or default-grey object, which
    /// looks like a bug in the feature rather than a missing asset. So the material is created here,
    /// at the path the builder wants, the first time it is asked for - and left alone after that, so
    /// a hand-tuned material is not overwritten by the next build.
    /// </summary>
    public static class BuildMaterials
    {
        public const string Folder = "Assets/_Project/Materials";

        /// <summary>The material at `path`, created in a URP-compatible unlit shader if it is missing.</summary>
        public static Material Get(string path, Color color)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            EnsureFolder(Folder);
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Standard");

            var material = new Material(shader) { name = System.IO.Path.GetFileNameWithoutExtension(path) };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);

            AssetDatabase.CreateAsset(material, path);
            AssetDatabase.SaveAssets();
            Debug.Log("[BuildMaterials] created " + path);
            return material;
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = System.IO.Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
