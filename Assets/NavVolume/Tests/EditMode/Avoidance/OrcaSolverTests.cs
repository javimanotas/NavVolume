using System;
using NavVolume.Runtime.Avoidance;
using NUnit.Framework;
using Unity.Mathematics;

namespace NavVolume.Tests.EditMode.Avoidance
{
    public class OrcaSolverTests
    {
        const float _TOLERANCE = 1e-3f;
        const float _TIME_STEP = 1f / 60f;

        static float3 Solve(
            OrcaPlane[] planes,
            int staticPlaneCount,
            float maxSpeed,
            float3 prefVelocity
        )
        {
            Span<OrcaPlane> scratch = stackalloc OrcaPlane[planes.Length + 1];

            return OrcaSolver.Solve(
                planes,
                planes.Length,
                staticPlaneCount,
                maxSpeed,
                prefVelocity,
                scratch
            );
        }

        static void AssertSatisfies(OrcaPlane plane, float3 velocity)
        {
            var violation = math.dot(plane.Normal, plane.Point - velocity);
            Assert.LessOrEqual(violation, _TOLERANCE, "Velocity violates the ORCA plane.");
        }

        [Test]
        public void Solve_WithoutPlanes_ShouldReturnPreferredVelocity()
        {
            var prefVelocity = new float3(1f, 2f, -0.5f);

            var result = Solve(Array.Empty<OrcaPlane>(), 0, 5f, prefVelocity);

            Assert.Less(math.distance(result, prefVelocity), _TOLERANCE);
        }

        [Test]
        public void Solve_WithoutPlanes_WhenPreferredExceedsMaxSpeed_ShouldClampToMaxSpeed()
        {
            var prefVelocity = new float3(10f, 0f, 0f);

            var result = Solve(Array.Empty<OrcaPlane>(), 0, 2f, prefVelocity);

            Assert.AreEqual(2f, math.length(result), _TOLERANCE);
            Assert.Greater(result.x, 0f);
        }

        [Test]
        public void Solve_WithSinglePlane_ShouldProjectOntoConstraint()
        {
            // Permitted half-space: x <= 1.
            var planes = new[]
            {
                new OrcaPlane { Normal = new float3(-1f, 0f, 0f), Point = new float3(1f, 0f, 0f) },
            };

            var result = Solve(planes, 0, 5f, new float3(3f, 0f, 0f));

            Assert.AreEqual(1f, result.x, _TOLERANCE);
            Assert.AreEqual(0f, result.y, _TOLERANCE);
            Assert.AreEqual(0f, result.z, _TOLERANCE);
        }

        [Test]
        public void Solve_WhenInfeasible_ShouldDegradeToBoundedVelocity()
        {
            // x >= 0.5 and x <= -0.5 cannot both hold. The fallback must still produce a finite
            // velocity inside the max-speed sphere, moving only along the conflict axis.
            var planes = new[]
            {
                new OrcaPlane { Normal = new float3(1f, 0f, 0f), Point = new float3(0.5f, 0f, 0f) },
                new OrcaPlane
                {
                    Normal = new float3(-1f, 0f, 0f),
                    Point = new float3(-0.5f, 0f, 0f),
                },
            };

            var result = Solve(planes, 0, 1f, new float3(0f, 0f, 0f));

            Assert.IsFalse(math.any(math.isnan(result)), "Result must be finite.");
            Assert.LessOrEqual(math.length(result), 1f + _TOLERANCE);
            Assert.AreEqual(0f, result.y, _TOLERANCE);
            Assert.AreEqual(0f, result.z, _TOLERANCE);
        }

        [Test]
        public void Solve_WhenInfeasible_ShouldKeepStaticPlanesHard()
        {
            // The static plane (x >= 0.5) conflicts with the agent plane (x <= -0.5). Only the
            // agent plane may be relaxed, so the result must still satisfy the static one.
            var planes = new[]
            {
                new OrcaPlane { Normal = new float3(1f, 0f, 0f), Point = new float3(0.5f, 0f, 0f) },
                new OrcaPlane
                {
                    Normal = new float3(-1f, 0f, 0f),
                    Point = new float3(-0.5f, 0f, 0f),
                },
            };

            var result = Solve(planes, 1, 1f, new float3(0f, 0f, 0f));

            AssertSatisfies(planes[0], result);
        }

        [Test]
        public void AgentPlane_WithHeadOnConflict_ShouldDodgeToComplementarySides()
        {
            // Two identical agents flying straight at each other along X. The exact symmetry is
            // degenerate; the deterministic tie-break must send them to opposite lateral sides.
            var positionA = new float3(0f, 0f, 0f);
            var positionB = new float3(4f, 0f, 0f);
            var velocityA = new float3(1f, 0f, 0f);
            var velocityB = new float3(-1f, 0f, 0f);

            var planeA = OrcaMath.AgentPlane(
                positionA,
                velocityA,
                0.5f,
                positionB,
                velocityB,
                0.5f,
                2f,
                _TIME_STEP
            );
            var planeB = OrcaMath.AgentPlane(
                positionB,
                velocityB,
                0.5f,
                positionA,
                velocityA,
                0.5f,
                2f,
                _TIME_STEP
            );

            var resultA = Solve(new[] { planeA }, 0, 1f, velocityA);
            var resultB = Solve(new[] { planeB }, 0, 1f, velocityB);

            AssertSatisfies(planeA, resultA);
            AssertSatisfies(planeB, resultB);

            var lateralA = new float2(resultA.y, resultA.z);
            var lateralB = new float2(resultB.y, resultB.z);

            Assert.Greater(math.length(lateralA), _TOLERANCE, "Agent A did not sidestep.");
            Assert.Greater(math.length(lateralB), _TOLERANCE, "Agent B did not sidestep.");
            Assert.Less(
                math.dot(lateralA, lateralB),
                0f,
                "Agents sidestepped to the same side and would mirror forever."
            );
        }

        [Test]
        public void StaticPlane_WithClosingVelocity_ShouldLimitApproachSpeed()
        {
            // Sphere obstacle 2 m ahead with combined radius 1: the surface gap is 1 m, so within a
            // 1 s horizon the approach speed along X must not exceed 1 m/s.
            var velocity = new float3(1.5f, 0f, 0f);

            var plane = OrcaMath.StaticPlane(
                new float3(2f, 0f, 0f),
                1f,
                velocity,
                float3.zero,
                1f,
                _TIME_STEP
            );

            var result = Solve(new[] { plane }, 1, 2f, velocity);

            AssertSatisfies(plane, result);
            Assert.AreEqual(1f, result.x, _TOLERANCE);
        }

        [Test]
        public void StaticPlane_WhenOverlapping_ShouldPushOut()
        {
            // Agent center 0.5 m inside the combined radius: the collision branch must produce a
            // velocity moving away from the obstacle.
            var plane = OrcaMath.StaticPlane(
                new float3(0.5f, 0f, 0f),
                1f,
                float3.zero,
                float3.zero,
                1f,
                _TIME_STEP
            );

            var result = Solve(new[] { plane }, 1, 5f, float3.zero);

            Assert.Less(result.x, 0f, "Velocity should point away from the overlapping obstacle.");
        }
    }
}
