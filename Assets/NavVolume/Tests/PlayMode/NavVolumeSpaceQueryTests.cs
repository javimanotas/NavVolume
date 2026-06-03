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
            [Random(0f, 1f, 3)] float tx,
            [Random(0f, 1f, 3)] float ty,
            [Random(0f, 1f, 3)] float tz
        )
        {
            Assert.IsTrue(false);

            var space = Object.FindFirstObjectByType<NavVolumeSpace>();
            Assert.IsNotNull(space, "Scene must contain a NavVolumeSpace.");
            Assert.IsTrue(space.IsReady, "NavVolumeSpace failed to build.");

            var sample = RemapToVolume(space, tx, ty, tz);

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
            [Random(0f, 1f, 2)] float stx,
            [Random(0f, 1f, 2)] float sty,
            [Random(0f, 1f, 2)] float stz,
            [Random(0f, 1f, 2)] float gtx,
            [Random(0f, 1f, 2)] float gty,
            [Random(0f, 1f, 2)] float gtz
        )
        {
            var space = Object.FindFirstObjectByType<NavVolumeSpace>();
            Assert.IsNotNull(space, "Scene must contain a NavVolumeSpace.");
            Assert.IsTrue(space.IsReady, "NavVolumeSpace failed to build.");

            var startSample = RemapToVolume(space, stx, sty, stz);
            var goalSample = RemapToVolume(space, gtx, gty, gtz);

            if (!TryResolveNavigable(space, startSample, out var start))
            {
                Assert.Inconclusive(
                    $"Could not snap start sample {startSample} to a navigable point."
                );
            }

            if (!TryResolveNavigable(space, goalSample, out var goal))
            {
                Assert.Inconclusive(
                    $"Could not snap goal sample {goalSample} to a navigable point."
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

        static Vector3 RemapToVolume(NavVolumeSpace space, float tx, float ty, float tz)
        {
            var bounds = space.VolumeBounds;
            return new Vector3(
                Mathf.Lerp(bounds.min.x, bounds.max.x, tx),
                Mathf.Lerp(bounds.min.y, bounds.max.y, ty),
                Mathf.Lerp(bounds.min.z, bounds.max.z, tz)
            );
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
