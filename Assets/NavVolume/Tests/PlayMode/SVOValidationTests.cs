using System.Collections;
using NavVolume;
using NavVolume.Runtime.Validation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class SVOValidatorTests
{
    const string ScenePath = "Assets/NavVolume/Tests/PlayMode/Scenes/SVOValidationTest.unity";

    [UnitySetUp]
    public IEnumerator LoadScene()
    {
        yield return SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Single);
        yield return null;
    }

    [UnityTest]
    public IEnumerator BuiltSVO_PassesAllValidationChecks()
    {
        var _ = new Collider[1];
        Assert.IsTrue(
            Physics.OverlapBoxNonAlloc(Vector3.zero, Vector3.one * 100, _) > 0,
            "No colliders found in the validation area."
        );

        var ctx = Object.FindFirstObjectByType<NavVolumeSpace>().NavCtx;

        if (!SVOValidator.IsValid(ctx, out var errors))
        {
            Assert.Fail(
                $"SVO validation failed with {errors.Count} error(s):\n{string.Join("\n", errors)}"
            );
        }

        yield return null;
    }
}
