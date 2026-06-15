namespace NavVolume
{
    /// <summary>
    /// Defines when the SVO will be built.
    /// </summary>
    internal enum BuildMode
    {
        /// <summary>
        /// The SVO will be built in the Editor.
        /// </summary>
        Baked,

        /// <summary>
        /// The SVO will be built when the game starts.
        /// </summary>
        /// <remarks>
        /// Awake is used instead of Start so other components can query the SVO on Start.
        /// </remarks>
        BuildOnAwake,

        /// <summary>
        /// The SVO will be built only when explicitly requested in code.
        /// </summary>
        Manual,
    }
}
