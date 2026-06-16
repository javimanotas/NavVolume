using NavVolume.Runtime;
using UnityEditor;

namespace NavVolume.Editor
{
    /// <summary>
    /// Help inspector for <see cref="AgentType"/> assets.
    /// </summary>
    [CustomEditor(typeof(AgentType))]
    public class AgentTypeEditor : ScriptableObjectHelpEditor { }
}
