namespace NavVolume
{
    /// <summary>
    /// Defines when the SVO will be built.
    /// </summary>
    internal enum BuildMode
    {
        /// <summary>
        /// The SVO will be build in the Editor.
        /// </summary>
        Baked,

        /// <summary>
        /// The SVO will be build when game starts.
        /// </summary>
        /// <remarks>
        /// Awake is used instead of Start so other components can query the SVO on Start.
        /// </remarks>
        BuildOnAwake,

        /// <summary>
        /// The SVO will be build only when calling
        /// </summary>
        Manual,
    }
}
