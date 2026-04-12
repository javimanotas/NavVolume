using NavVolume;
using UnityEngine;

/// <summary>
/// Very simple demo script that makes the NavVolumeAgent fly towards a target Transform.
/// </summary>
/// <remarks>
/// The position of the target can be changed at runtime, and the NavVolumeAgent will update its path accordingly.
/// </remarks>
[RequireComponent(typeof(NavVolumeAgent))]
public class FlyToTarget : MonoBehaviour
{
    [SerializeField]
    Transform Target;

    Vector3 _targetedPosition = Vector3.positiveInfinity;

    NavVolumeAgent _agent;

    void Awake()
    {
        _agent = GetComponent<NavVolumeAgent>();
    }

    void Update()
    {
        if (_targetedPosition != Target.position)
        {
            _targetedPosition = Target.position;
            _agent.MoveTo(Target.position);
        }
    }
}
