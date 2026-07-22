using System.Collections.Generic;
using UnityEngine;

namespace ProjectExpedition
{
    public static class SharedMovementCollision
    {
        private const float MinimumDistanceSquared = 0.0001f;
        private const float MinimumWorthwhileDetourProgressFraction = 0.12f;
        private const int DetourSampleCount = 12;

        public static bool OverlapsCircle(ObstacleDefinition obstacle, Vector2 position, float radius)
        {
            if (obstacle.Shape == ObstacleShapeKind.Circle)
            {
                var combinedRadius = radius + obstacle.Radius;
                return (position - obstacle.Center).sqrMagnitude <= combinedRadius * combinedRadius;
            }

            var closest = ClosestPointOnBox(obstacle.Center, obstacle.HalfExtents, position);
            var delta = position - closest;
            return delta.sqrMagnitude <= radius * radius;
        }

        public static bool SegmentBlockedByObstacles(Vector2 from, Vector2 to, float radius,
            IReadOnlyList<ObstacleDefinition> obstacles)
        {
            if (obstacles == null || obstacles.Count == 0)
            {
                return false;
            }

            var delta = to - from;
            var distance = delta.magnitude;

            if (distance <= MinimumDistanceSquared)
            {
                return IsBlocked(from, radius, obstacles);
            }

            var step = Mathf.Max(radius * 0.45f, 0.08f);
            var steps = Mathf.Max(1, Mathf.CeilToInt(distance / step));

            for (var i = 0; i <= steps; i++)
            {
                var t = i / (float)steps;
                var sample = Vector2.Lerp(from, to, t);

                if (IsBlocked(sample, radius, obstacles))
                {
                    return true;
                }
            }

            return false;
        }

        public static Vector2 AdvanceCircleTowardsTarget(Vector2 currentPosition, float radius,
            Vector2 targetPosition, float maxStep, IReadOnlyList<ObstacleDefinition> obstacles)
        {
            if (maxStep <= 0f)
            {
                return currentPosition;
            }

            if (obstacles == null || obstacles.Count == 0)
            {
                return MoveTowardsUnblocked(currentPosition, targetPosition, maxStep);
            }

            var toTarget = targetPosition - currentPosition;

            if (toTarget.sqrMagnitude <= MinimumDistanceSquared)
            {
                return PushOutFromObstacles(currentPosition, radius, obstacles);
            }

            var travel = toTarget.sqrMagnitude <= maxStep * maxStep
                ? toTarget
                : toTarget.normalized * maxStep;
            var desired = currentPosition + travel;
            var forwardPathBlocked = false;

            if (SegmentBlockedByObstacles(currentPosition, desired, radius, obstacles))
            {
                forwardPathBlocked = true;
                var maximumDistance = travel.magnitude;
                var safeDistance = 0f;

                for (var iteration = 0; iteration < 8; iteration++)
                {
                    var candidateDistance = (safeDistance + maximumDistance) * 0.5f;
                    var candidate = currentPosition + travel.normalized * candidateDistance;

                    if (SegmentBlockedByObstacles(currentPosition, candidate, radius, obstacles))
                    {
                        maximumDistance = candidateDistance;
                    }
                    else
                    {
                        safeDistance = candidateDistance;
                    }
                }

                travel = travel.normalized * safeDistance;
                desired = currentPosition + travel;
            }

            var minimumWorthwhileProgress = maxStep * MinimumWorthwhileDetourProgressFraction;
            var resolved = ResolveCircleMovement(currentPosition, radius, desired, obstacles);
            var resolvedMovement = resolved - currentPosition;

            if (resolvedMovement.sqrMagnitude > MinimumDistanceSquared)
            {
                var forwardProgress = Vector2.Dot(resolvedMovement, toTarget.normalized);

                if (!forwardPathBlocked || forwardProgress >= minimumWorthwhileProgress)
                {
                    return resolved;
                }
            }

            if (forwardPathBlocked)
            {
                var sidestep = TrySelectSidestepFallback(currentPosition, radius, maxStep, toTarget, obstacles);

                if (sidestep.HasValue)
                {
                    return sidestep.Value;
                }
            }
            else
            {
                var bestDetour = TrySelectBestDetour(currentPosition, radius, targetPosition, maxStep, toTarget,
                    obstacles);

                if (bestDetour.HasValue)
                {
                    return bestDetour.Value;
                }
            }

            var pushed = PushOutFromObstacles(currentPosition, radius, obstacles);
            var slideTowardTarget = ResolveCircleMovement(pushed, radius, pushed + travel, obstacles);

            if ((slideTowardTarget - currentPosition).sqrMagnitude > MinimumDistanceSquared)
            {
                return slideTowardTarget;
            }

            return pushed;
        }

        private static Vector2 MoveTowardsUnblocked(Vector2 currentPosition, Vector2 targetPosition, float maxStep)
        {
            var toTarget = targetPosition - currentPosition;
            var travel = toTarget.sqrMagnitude <= maxStep * maxStep
                ? toTarget
                : toTarget.normalized * maxStep;

            return currentPosition + travel;
        }

        private static Vector2? TrySelectBestDetour(Vector2 currentPosition, float radius, Vector2 targetPosition,
            float maxStep, Vector2 toTarget, IReadOnlyList<ObstacleDefinition> obstacles)
        {
            Vector2? bestPosition = null;
            var bestProgress = float.MinValue;
            var baseAngle = Mathf.Atan2(toTarget.y, toTarget.x);
            var perpendicular = new Vector2(-toTarget.y, toTarget.x).normalized;
            var minimumWorthwhileProgress = maxStep * MinimumWorthwhileDetourProgressFraction;

            for (var attempt = 1; attempt <= DetourSampleCount; attempt++)
            {
                var sign = attempt % 2 == 0 ? 1f : -1f;
                var angleOffset = sign * Mathf.Ceil(attempt / 2f) * (Mathf.PI / 8f);
                var detourDirection = new Vector2(Mathf.Cos(baseAngle + angleOffset),
                    Mathf.Sin(baseAngle + angleOffset));
                ConsiderDetour(currentPosition, radius, toTarget, obstacles,
                    currentPosition + detourDirection * maxStep, ref bestPosition, ref bestProgress);
            }

            for (var side = 1; side >= -1; side -= 2)
            {
                ConsiderDetour(currentPosition, radius, toTarget, obstacles,
                    currentPosition + perpendicular * (side * maxStep), ref bestPosition, ref bestProgress);
                ConsiderDetour(currentPosition, radius, toTarget, obstacles,
                    currentPosition + toTarget.normalized * (maxStep * 0.5f) +
                    perpendicular * (side * maxStep * 0.75f), ref bestPosition, ref bestProgress);
            }

            ConsiderAxisAlignedDetours(currentPosition, radius, targetPosition, maxStep, toTarget, obstacles,
                ref bestPosition, ref bestProgress);

            if (bestPosition.HasValue && bestProgress >= minimumWorthwhileProgress)
            {
                return bestPosition;
            }

            return TrySelectSidestepFallback(currentPosition, radius, maxStep, toTarget, obstacles);
        }

        private static Vector2? TrySelectSidestepFallback(Vector2 currentPosition, float radius, float maxStep,
            Vector2 toTarget, IReadOnlyList<ObstacleDefinition> obstacles)
        {
            var delta = toTarget;
            var lateralStep = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y)
                ? new Vector2(0f, maxStep)
                : new Vector2(maxStep, 0f);
            var lateralDirections = new[]
            {
                lateralStep,
                -lateralStep
            };

            for (var i = 0; i < lateralDirections.Length; i++)
            {
                var sidestep = TryResolveDetour(currentPosition, radius, obstacles,
                    currentPosition + lateralDirections[i]);

                if (sidestep.HasValue)
                {
                    return sidestep;
                }
            }

            var perpendicular = new Vector2(-toTarget.y, toTarget.x).normalized;

            for (var side = 1; side >= -1; side -= 2)
            {
                var sidestep = TryResolveDetour(currentPosition, radius, obstacles,
                    currentPosition + perpendicular * (side * maxStep));

                if (sidestep.HasValue)
                {
                    return sidestep;
                }
            }

            return null;
        }

        private static Vector2? TryResolveDetour(Vector2 currentPosition, float radius,
            IReadOnlyList<ObstacleDefinition> obstacles, Vector2 detourDesired)
        {
            if (SegmentBlockedByObstacles(currentPosition, detourDesired, radius, obstacles))
            {
                return null;
            }

            var detourResolved = ResolveCircleMovement(currentPosition, radius, detourDesired, obstacles);
            var movement = detourResolved - currentPosition;

            if (movement.sqrMagnitude <= MinimumDistanceSquared)
            {
                return null;
            }

            return detourResolved;
        }

        private static void ConsiderDetour(Vector2 currentPosition, float radius, Vector2 toTarget,
            IReadOnlyList<ObstacleDefinition> obstacles, Vector2 detourDesired, ref Vector2? bestPosition,
            ref float bestProgress)
        {
            if (SegmentBlockedByObstacles(currentPosition, detourDesired, radius, obstacles))
            {
                return;
            }

            var detourResolved = ResolveCircleMovement(currentPosition, radius, detourDesired, obstacles);
            var movement = detourResolved - currentPosition;

            if (movement.sqrMagnitude <= MinimumDistanceSquared)
            {
                return;
            }

            var progress = Vector2.Dot(movement, toTarget.normalized);

            if (progress <= bestProgress)
            {
                return;
            }

            bestProgress = progress;
            bestPosition = detourResolved;
        }

        private static void ConsiderAxisAlignedDetours(Vector2 currentPosition, float radius,
            Vector2 targetPosition, float maxStep, Vector2 toTarget, IReadOnlyList<ObstacleDefinition> obstacles,
            ref Vector2? bestPosition, ref float bestProgress)
        {
            var delta = targetPosition - currentPosition;
            var stepX = new Vector2(Mathf.Sign(delta.x) * maxStep, 0f);
            var stepY = new Vector2(0f, Mathf.Sign(delta.y) * maxStep);

            if (Mathf.Abs(delta.x) >= Mathf.Abs(delta.y))
            {
                ConsiderDetour(currentPosition, radius, toTarget, obstacles, currentPosition + stepX,
                    ref bestPosition, ref bestProgress);
                ConsiderDetour(currentPosition, radius, toTarget, obstacles, currentPosition + stepY,
                    ref bestPosition, ref bestProgress);
            }
            else
            {
                ConsiderDetour(currentPosition, radius, toTarget, obstacles, currentPosition + stepY,
                    ref bestPosition, ref bestProgress);
                ConsiderDetour(currentPosition, radius, toTarget, obstacles, currentPosition + stepX,
                    ref bestPosition, ref bestProgress);
            }
        }

        public static Vector2 ResolveCircleMovement(Vector2 currentPosition, float radius,
            Vector2 desiredPosition, IReadOnlyList<ObstacleDefinition> obstacles)
        {
            if (obstacles == null || obstacles.Count == 0)
            {
                return desiredPosition;
            }

            if (!IsBlocked(desiredPosition, radius, obstacles))
            {
                return desiredPosition;
            }

            var delta = desiredPosition - currentPosition;
            var axisX = new Vector2(desiredPosition.x, currentPosition.y);
            var axisY = new Vector2(currentPosition.x, desiredPosition.y);

            if (!IsBlocked(axisX, radius, obstacles))
            {
                if (!IsBlocked(new Vector2(axisX.x, axisY.y), radius, obstacles))
                {
                    return new Vector2(axisX.x, axisY.y);
                }

                return axisX;
            }

            if (!IsBlocked(axisY, radius, obstacles))
            {
                return axisY;
            }

            if (delta.sqrMagnitude <= MinimumDistanceSquared)
            {
                return currentPosition;
            }

            var tangent = new Vector2(-delta.y, delta.x).normalized;
            var left = currentPosition + tangent * delta.magnitude;
            var right = currentPosition - tangent * delta.magnitude;

            if (!IsBlocked(left, radius, obstacles))
            {
                return left;
            }

            if (!IsBlocked(right, radius, obstacles))
            {
                return right;
            }

            return currentPosition;
        }

        public static Vector2 ResolveCircleKnockback(Vector2 currentPosition, float radius,
            Vector2 knockbackDelta, IReadOnlyList<ObstacleDefinition> obstacles)
        {
            if (knockbackDelta.sqrMagnitude <= MinimumDistanceSquared)
            {
                return currentPosition;
            }

            return ResolveCircleMovement(currentPosition, radius, currentPosition + knockbackDelta, obstacles);
        }

        public static Vector2 PushOutFromObstacles(Vector2 position, float radius,
            IReadOnlyList<ObstacleDefinition> obstacles)
        {
            if (obstacles == null || obstacles.Count == 0)
            {
                return position;
            }

            var corrected = position;

            for (var iteration = 0; iteration < 4; iteration++)
            {
                var moved = false;

                for (var i = 0; i < obstacles.Count; i++)
                {
                    var obstacle = obstacles[i];

                    if (obstacle.Shape == ObstacleShapeKind.Circle)
                    {
                        var away = corrected - obstacle.Center;
                        var minimumDistance = radius + obstacle.Radius;

                        if (away.sqrMagnitude >= minimumDistance * minimumDistance)
                        {
                            continue;
                        }

                        if (away.sqrMagnitude <= MinimumDistanceSquared)
                        {
                            away = Vector2.up;
                        }

                        corrected = obstacle.Center + away.normalized * minimumDistance;
                        moved = true;
                        continue;
                    }

                    var closest = ClosestPointOnBox(obstacle.Center, obstacle.HalfExtents, corrected);
                    var push = corrected - closest;

                    if (push.sqrMagnitude >= radius * radius)
                    {
                        continue;
                    }

                    if (push.sqrMagnitude <= MinimumDistanceSquared)
                    {
                        push = (corrected - obstacle.Center).sqrMagnitude > MinimumDistanceSquared
                            ? corrected - obstacle.Center
                            : Vector2.up;
                    }

                    corrected = closest + push.normalized * radius;
                    moved = true;
                }

                if (!moved)
                {
                    break;
                }
            }

            return corrected;
        }

        private static bool IsBlocked(Vector2 position, float radius, IReadOnlyList<ObstacleDefinition> obstacles)
        {
            for (var i = 0; i < obstacles.Count; i++)
            {
                if (OverlapsCircle(obstacles[i], position, radius))
                {
                    return true;
                }
            }

            return false;
        }

        private static Vector2 ClosestPointOnBox(Vector2 center, Vector2 halfExtents, Vector2 point)
        {
            var local = point - center;
            var clampedX = Mathf.Clamp(local.x, -halfExtents.x, halfExtents.x);
            var clampedY = Mathf.Clamp(local.y, -halfExtents.y, halfExtents.y);
            return center + new Vector2(clampedX, clampedY);
        }
    }
}
