namespace NavVolume.Editor
{
    /// <summary>
    /// Base inspector for ScriptableObjects to add help button.
    /// </summary>
    /// <remarks>
    /// Derive a one-line subclass per asset type.
    /// </remarks>
    public abstract class ScriptableObjectHelpEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGuiHelpers.DrawHelpRow(target);
            DrawDefaultInspector();
        }
    }
}
