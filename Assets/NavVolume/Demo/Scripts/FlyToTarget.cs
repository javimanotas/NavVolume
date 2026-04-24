using UnityEngine;

namespace NavVolume.Demo
{
    /// <summary>
    /// Very simple demo script that makes the NavVolumeAgent fly towards a target Transform.
    /// </summary>
    /// <remarks>
    /// The position of the target can be changed at runtime, and the NavVolumeAgent will update its path accordingly.
    /// </remarks>
    [AddComponentMenu("NavVolume/Demo/Fly To Target")]
    [RequireComponent(typeof(NavVolumeAgent))]
    [DisallowMultipleComponent]
    public class FlyToTarget : MonoBehaviour
    {
        [SerializeField]
        Transform _target;

        Vector3 _targetedPosition = Vector3.positiveInfinity;

        NavVolumeAgent _agent;

        void Awake()
        {
            _agent = GetComponent<NavVolumeAgent>();
        }

        void Update()
        {
            if (_targetedPosition != _target.position)
            {
                _targetedPosition = _target.position;
                _agent.MoveTo(_target.position);
            }
        }
    }
}
