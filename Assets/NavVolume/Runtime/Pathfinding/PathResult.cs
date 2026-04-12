using System.Collections.Generic;
using UnityEngine;

namespace NavVolume.Pathfinding
{
    /// <summary>
    /// Output of a completed (or failed) A* query.
    /// </summary>
    internal readonly struct PathResult
    {
        public static PathResult Failed(PathStatus status) => new(status);

        readonly PathStatus _status;

        public readonly List<Vector3> Waypoints;

        // TODO: implement stats

        PathResult(PathStatus rawStatus)
        {
            _status = rawStatus;
            Waypoints = new();
        }

        public PathResult(List<Vector3> waypoints)
        {
            _status = PathStatus.Success;
            Waypoints = waypoints;
        }

        public bool Succeeded(out PathStatus status)
        {
            status = _status;
            return _status == PathStatus.Success;
        }
    }
}
