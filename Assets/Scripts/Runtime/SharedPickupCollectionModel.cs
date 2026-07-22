using System.Collections.Generic;
using UnityEngine;

namespace ProjectExpedition
{
    public static class SharedPickupCollectionModel
    {
        public const float DefaultCollectionRadiusSqr = 0.24f;

        public static int FindCollectingPlayerIndex(
            Vector2 pickupPosition,
            IReadOnlyList<Vector2> playerPositions,
            IReadOnlyList<bool> playerAlive,
            float collectionRadiusSqr = DefaultCollectionRadiusSqr)
        {
            var bestIndex = -1;
            var bestDistance = collectionRadiusSqr;

            for (var i = 0; i < playerPositions.Count; i++)
            {
                if (i >= playerAlive.Count || !playerAlive[i])
                {
                    continue;
                }

                var sqrDistance = (playerPositions[i] - pickupPosition).sqrMagnitude;
                if (sqrDistance > bestDistance)
                {
                    continue;
                }

                bestDistance = sqrDistance;
                bestIndex = i;
            }

            return bestIndex;
        }

        public static int FindMagnetTargetIndex(
            Vector2 pickupPosition,
            IReadOnlyList<Vector2> playerPositions,
            IReadOnlyList<bool> playerAlive,
            IReadOnlyList<float> magnetRadii)
        {
            var bestIndex = -1;
            var bestDistance = float.MaxValue;

            for (var i = 0; i < playerPositions.Count; i++)
            {
                if (i >= playerAlive.Count || !playerAlive[i])
                {
                    continue;
                }

                var magnet = i < magnetRadii.Count ? magnetRadii[i] : 0f;
                var sqrDistance = (playerPositions[i] - pickupPosition).sqrMagnitude;
                if (sqrDistance >= magnet * magnet)
                {
                    continue;
                }

                if (sqrDistance >= bestDistance)
                {
                    continue;
                }

                bestDistance = sqrDistance;
                bestIndex = i;
            }

            return bestIndex;
        }
    }
}
