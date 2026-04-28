using System.Collections.Generic;
using UnityEngine;

namespace NavVolume.Runtime.Pathfinding
{
    /// <summary>
    /// Output of a completed (or failed) A* query.
    /// </summary>
    internal readonly struct PathResult
    {
        public static PathResult Success(List<Vector3> waypoints) => new(waypoints);

        public static PathResult Failure(PathResultStatus status) => new(status);

        public readonly PathResultStatus Status;

        public readonly List<Vector3> Waypoints;

        public bool Succeeded => Status == PathResultStatus.Sucess;

        // TODO: implement stats

        PathResult(PathResultStatus rawStatus)
        {
            Status = rawStatus;
            Waypoints = null;
        }

        PathResult(List<Vector3> rawWaypoints)
        {
            Status = PathResultStatus.Sucess;
            Waypoints = rawWaypoints;
        }
    }
}
