using UnityEngine;

namespace Signspace.Spatial
{
    /// <summary>
    /// One object on the shelf (P4.2). The component the prefab carries; the shape arrives from the lock.
    ///
    /// It is deliberately thin. It does not animate itself - breathing is a single owner (<see
    /// cref="ShelfIdle"/>) because twelve objects each running their own Update is the trap this task
    /// names. It does not move itself either: the flight is the payoff's job (LockPayoff), and the slot is
    /// the shelf's job (ShelfAnchor). What is left is the identity: which silhouette, which colour, and
    /// what the object calls itself.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class ShelfObjectView : MonoBehaviour
    {
        [Tooltip("Which handshape this object remembers. Set by the shelf when it spawns one.")]
        public string shapeId = "";

        [Tooltip("Leave empty to find the MeshRenderer on this object.")]
        public MeshRenderer body;

        [Tooltip("Material for the shelf objects. Unlit: they are read as silhouettes, not lit geometry.")]
        public Material objectMaterial;

        MeshFilter _filter;
        MaterialPropertyBlock _block;

        public string Family => ShelfSilhouettes.FamilyOf(shapeId);

        void Awake()
        {
            _filter = GetComponent<MeshFilter>();
            if (body == null) body = GetComponent<MeshRenderer>();
            _block = new MaterialPropertyBlock();
        }

        // Deliberately NO registration here. An object that registers itself starts breathing the
        // instant it exists - which is the middle of the payoff's flight, while the payoff is still
        // writing its scale every frame. Two writers, one transform. The shelf registers an object when
        // it comes to rest (ShelfAnchor.RegisterIdle); the object does not get a vote.

        /// <summary>Give this object its identity. Safe to call before or after Awake.</summary>
        public void SetShape(string id)
        {
            shapeId = (id ?? "").ToUpperInvariant();
            name = "ShelfObject_" + shapeId;

            if (_filter == null) _filter = GetComponent<MeshFilter>();
            if (body == null) body = GetComponent<MeshRenderer>();
            if (_block == null) _block = new MaterialPropertyBlock();

            if (_filter != null) _filter.sharedMesh = ShelfSilhouettes.For(shapeId);

            if (body != null)
            {
                if (objectMaterial != null) body.sharedMaterial = objectMaterial;
                body.GetPropertyBlock(_block);
                Color colour = ShelfSilhouettes.ColourFor(shapeId);
                if (body.sharedMaterial != null && body.sharedMaterial.HasProperty("_BaseColor"))
                    _block.SetColor("_BaseColor", colour);
                if (body.sharedMaterial != null && body.sharedMaterial.HasProperty("_Color"))
                    _block.SetColor("_Color", colour);
                body.SetPropertyBlock(_block);
            }
        }

        /// <summary>Dev check: what this object claims to be, for the overlay and the shelf log.</summary>
        public string Report => shapeId + " (" + Family + ", " + ShelfSilhouettes.TriangleCount(shapeId) + " tris)";
    }
}
