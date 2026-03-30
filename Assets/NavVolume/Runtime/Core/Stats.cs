namespace NavVolume.Runtime.Core
{
    internal partial class SVO
    {
        /// <summary>
        /// At the moment this is empty.
        /// </summary>
        /// <remarks>
        /// This is defined inside the SVO to access private data.
        /// </remarks>
        public struct Stats
        {
            // TODO: implement SVO stats (memory used, nodes per layer...)

            public override string ToString() => "SVO stats not implemented yet";
        }
    }
}
