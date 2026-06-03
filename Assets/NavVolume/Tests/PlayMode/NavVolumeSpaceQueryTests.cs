using System.Collections;
using NavVolume.Runtime.Pathfinding;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace NavVolume.Tests.PlayMode
{
    public class NavVolumeSpaceQueryTests
    {
        const string _SCENE_PATH = "Assets/NavVolume/Tests/PlayMode/Scenes/SVOValidationTest.unity";

        // Volume in the test scene spans roughly x in [-5.27, 17.79], y/z in [-11.53, 11.53].
        // Keep a small margin so floored voxel coords never fall on the gridDim boundary.
        const float _SAMPLE_X_MIN = -5f;
        const float _SAMPLE_X_MAX = 17f;
        const float _SAMPLE_YZ_MIN = -11f;
        const float _SAMPLE_YZ_MAX = 11f;

        const float _PATH_HEURISTIC_WEIGHT = 1.5f;

        const int _PATH_MAX_NODES_BUDGET = 100_000;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync(_SCENE_PATH, LoadSceneMode.Single);
            yield return null;
        }

        [UnityTest]
        public IEnumerator TrySnapToNavigable_OnNonNavigableSample_YieldsNavigablePoint(
            [Random(_SAMPLE_X_MIN, _SAMPLE_X_MAX, 3)] float x,
            [Random(_SAMPLE_YZ_MIN, _SAMPLE_YZ_MAX, 3)] float y,
            [Random(_SAMPLE_YZ_MIN, _SAMPLE_YZ_MAX, 3)] float z
        )
        {
            var space = Object.FindFirstObjectByType<NavVolumeSpace>();
            Assert.IsNotNull(space, "Scene must contain a NavVolumeSpace.");
            Assert.IsTrue(space.IsReady, "NavVolumeSpace failed to build.");

            var sample = new Vector3(x, y, z);

            if (space.IsNavigable(sample))
            {
                yield break;
            }

            var snapped = space.TrySnapToNavigable(sample, float.MaxValue, out var result);

            Assert.IsTrue(
                snapped,
                $"TrySnapToNavigable returned false for non-navigable sample {sample} "
            );
            Assert.IsTrue(
                space.IsNavigable(result),
                $"Snapped point {result} (from non-navigable sample {sample}) "
                    + "is itself reported as not navigable."
            );

            yield return null;
        }

        [UnityTest]
        public IEnumerator FindPath_BetweenTwoSnappedRandomSamples_Succeeds(
            [Random(_SAMPLE_X_MIN, _SAMPLE_X_MAX, 2)] float sx,
            [Random(_SAMPLE_YZ_MIN, _SAMPLE_YZ_MAX, 2)] float sy,
            [Random(_SAMPLE_YZ_MIN, _SAMPLE_YZ_MAX, 2)] float sz,
            [Random(_SAMPLE_X_MIN, _SAMPLE_X_MAX, 2)] float gx,
            [Random(_SAMPLE_YZ_MIN, _SAMPLE_YZ_MAX, 2)] float gy,
            [Random(_SAMPLE_YZ_MIN, _SAMPLE_YZ_MAX, 2)] float gz
        )
        {
            var space = Object.FindFirstObjectByType<NavVolumeSpace>();
            Assert.IsNotNull(space, "Scene must contain a NavVolumeSpace.");
            Assert.IsTrue(space.IsReady, "NavVolumeSpace failed to build.");

            if (!TryResolveNavigable(space, new Vector3(sx, sy, sz), out var start))
            {
                Assert.Inconclusive(
                    $"Could not snap start sample ({sx}, {sy}, {sz}) to a navigable point."
                );
            }

            if (!TryResolveNavigable(space, new Vector3(gx, gy, gz), out var goal))
            {
                Assert.Inconclusive(
                    $"Could not snap goal sample ({gx}, {gy}, {gz}) to a navigable point."
                );
            }

            var request = new PathRequest(
                start,
                goal,
                _PATH_HEURISTIC_WEIGHT,
                _PATH_MAX_NODES_BUDGET
            );
            var result = space.FindPath(request);

            Assert.IsTrue(
                result.Succeeded,
                $"FindPath from {start} to {goal} failed with status {result.Status}."
            );

            yield return null;
        }

        static bool TryResolveNavigable(NavVolumeSpace space, Vector3 sample, out Vector3 result)
        {
            if (space.IsNavigable(sample))
            {
                result = sample;
                return true;
            }

            return space.TrySnapToNavigable(sample, float.MaxValue, out result);
        }
    }
}
