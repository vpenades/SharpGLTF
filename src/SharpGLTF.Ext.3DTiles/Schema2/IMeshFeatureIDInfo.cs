namespace SharpGLTF.Schema2.Tiles3D
{
    /// <summary>
    /// Mesh Feature Ids
    /// </summary>
    /// <remarks>
    /// Implemented by <see cref="MeshExtInstanceFeatureID"/> and <see cref="MeshExtMeshFeatureID"/>
    /// </remarks>
    public interface IMeshFeatureIDInfo
    {
        /// <summary>
        /// The number of unique features in the attribute or texture.
        /// </summary>
        public int FeatureCount { get; set; }

        /// <summary>
        /// A value that indicates that no feature is associated with this vertex or texel.
        /// </summary>
        public int? NullFeatureId { get; set; }

        /// <summary>
        /// An attribute containing feature IDs. When `attribute` and `texture` are omitted the 
        /// feature IDs are assigned to vertices by their index.
        /// </summary>
        public int? Attribute { get; set; }

        /// <summary>
        /// A label assigned to this feature ID set. Labels must be alphanumeric identifiers 
        /// matching the regular expression `^[a-zA-Z_][a-zA-Z0-9_]*$`.
        /// </summary>
        public string Label { get; set; }

        /// <summary>
        /// The index of the property table containing per-feature property values. Only applicable when using the `EXT_structural_metadata` extension.
        /// </summary>
        public int? PropertyTableIndex { get; set; }        
    }
}
