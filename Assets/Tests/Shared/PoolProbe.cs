using UnityEngine;

namespace ProjectExpedition.Tests
{
    public sealed class PoolProbe : MonoBehaviour, IPoolableComponent
    {
        public int ReleaseCount { get; private set; }

        public void OnReleasedToPool() => ReleaseCount++;
    }
}
