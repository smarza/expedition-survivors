using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectExpedition
{
    public interface IPoolableComponent
    {
        void OnReleasedToPool();
    }

    public sealed class ComponentPool<T> where T : Component, IPoolableComponent
    {
        private readonly Func<T> _factory;
        private readonly Transform _poolRoot;
        private readonly Stack<T> _available;
        private readonly HashSet<T> _active = new HashSet<T>();

        public int Created { get; private set; }
        public int Reused { get; private set; }
        public int Released { get; private set; }
        public int ActiveCount => _active.Count;
        public int AvailableCount => _available.Count;
        public float ReuseRatio => Created + Reused > 0 ? Reused / (float)(Created + Reused) : 0f;

        public ComponentPool(Func<T> factory, Transform poolRoot, int initialCapacity = 0)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _poolRoot = poolRoot;
            _available = new Stack<T>(Mathf.Max(4, initialCapacity));
            Prewarm(initialCapacity);
        }

        public T Get(Vector3 position)
        {
            T instance;
            if (_available.Count > 0)
            {
                instance = _available.Pop();
                Reused++;
            }
            else
            {
                instance = _factory();
                Created++;
            }

            instance.transform.SetParent(_poolRoot, false);
            instance.transform.position = position;
            instance.transform.rotation = Quaternion.identity;
            instance.gameObject.SetActive(true);
            _active.Add(instance);
            return instance;
        }

        public void Release(T instance)
        {
            if (instance == null || !_active.Remove(instance)) return;
            instance.OnReleasedToPool();
            instance.transform.SetParent(_poolRoot, false);
            instance.gameObject.SetActive(false);
            _available.Push(instance);
            Released++;
        }

        public void ReleaseAll()
        {
            if (_active.Count == 0) return;
            var snapshot = new T[_active.Count];
            _active.CopyTo(snapshot);
            for (var i = 0; i < snapshot.Length; i++) Release(snapshot[i]);
        }

        public void Prewarm(int count)
        {
            for (var i = 0; i < count; i++)
            {
                var instance = _factory();
                Created++;
                instance.OnReleasedToPool();
                instance.transform.SetParent(_poolRoot, false);
                instance.gameObject.SetActive(false);
                _available.Push(instance);
            }
        }
    }

    public sealed class SpatialHashGrid<T> where T : class
    {
        private readonly float _cellSize;
        private readonly Func<T, Vector2> _position;
        private readonly Dictionary<long, List<T>> _cells = new Dictionary<long, List<T>>(256);
        private readonly Dictionary<T, long> _membership = new Dictionary<T, long>();

        public long QueryCount { get; private set; }
        public int ItemCount => _membership.Count;
        public int OccupiedCellCount => _cells.Count;

        public SpatialHashGrid(float cellSize, Func<T, Vector2> position)
        {
            _cellSize = Mathf.Max(0.25f, cellSize);
            _position = position ?? throw new ArgumentNullException(nameof(position));
        }

        public void Add(T item)
        {
            if (item == null) return;
            Remove(item);
            var key = Key(_position(item));
            Cell(key).Add(item);
            _membership[item] = key;
        }

        public void Update(T item)
        {
            if (item == null) return;
            var next = Key(_position(item));
            if (!_membership.TryGetValue(item, out var current))
            {
                Cell(next).Add(item);
                _membership[item] = next;
                return;
            }
            if (current == next) return;
            RemoveFromCell(current, item);
            Cell(next).Add(item);
            _membership[item] = next;
        }

        public void Remove(T item)
        {
            if (item == null || !_membership.TryGetValue(item, out var key)) return;
            RemoveFromCell(key, item);
            _membership.Remove(item);
        }

        public void QueryRadius(Vector2 center, float radius, List<T> results)
        {
            if (results == null) throw new ArgumentNullException(nameof(results));
            results.Clear();
            QueryCount++;
            var safeRadius = Mathf.Max(0f, radius);
            var minX = Mathf.FloorToInt((center.x - safeRadius) / _cellSize);
            var maxX = Mathf.FloorToInt((center.x + safeRadius) / _cellSize);
            var minY = Mathf.FloorToInt((center.y - safeRadius) / _cellSize);
            var maxY = Mathf.FloorToInt((center.y + safeRadius) / _cellSize);
            for (var x = minX; x <= maxX; x++)
            for (var y = minY; y <= maxY; y++)
            {
                if (!_cells.TryGetValue(Key(x, y), out var cell)) continue;
                for (var i = 0; i < cell.Count; i++) results.Add(cell[i]);
            }
        }

        public T FindNearest(Vector2 origin, float maximumDistance, Predicate<T> predicate = null)
        {
            QueryCount++;
            var radius = Mathf.Max(_cellSize, maximumDistance);
            var cellRadius = Mathf.CeilToInt(radius / _cellSize);
            var originX = Mathf.FloorToInt(origin.x / _cellSize);
            var originY = Mathf.FloorToInt(origin.y / _cellSize);
            var bestDistance = radius * radius;
            T nearest = null;
            var firstHitRing = -1;
            for (var ring = 0; ring <= cellRadius; ring++)
            {
                for (var offsetX = -ring; offsetX <= ring; offsetX++)
                for (var offsetY = -ring; offsetY <= ring; offsetY++)
                {
                    if (ring > 0 && Mathf.Abs(offsetX) != ring && Mathf.Abs(offsetY) != ring) continue;
                    if (!_cells.TryGetValue(Key(originX + offsetX, originY + offsetY), out var cell)) continue;
                    for (var i = 0; i < cell.Count; i++)
                    {
                        var item = cell[i];
                        if (predicate != null && !predicate(item)) continue;
                        var distance = (_position(item) - origin).sqrMagnitude;
                        if (distance >= bestDistance) continue;
                        bestDistance = distance;
                        nearest = item;
                    }
                }
                if (nearest != null && firstHitRing < 0) firstHitRing = ring;
                // One additional ring covers the cell-boundary case without
                // turning every nearest query back into a full-area scan.
                if (firstHitRing >= 0 && ring >= firstHitRing + 1) break;
            }
            return nearest;
        }

        public void Clear()
        {
            _cells.Clear();
            _membership.Clear();
            QueryCount = 0;
        }

        private List<T> Cell(long key)
        {
            if (_cells.TryGetValue(key, out var cell)) return cell;
            cell = new List<T>(8);
            _cells.Add(key, cell);
            return cell;
        }

        private void RemoveFromCell(long key, T item)
        {
            if (!_cells.TryGetValue(key, out var cell)) return;
            cell.Remove(item);
            if (cell.Count == 0) _cells.Remove(key);
        }

        private long Key(Vector2 position) => Key(
            Mathf.FloorToInt(position.x / _cellSize),
            Mathf.FloorToInt(position.y / _cellSize));

        private static long Key(int x, int y) => ((long)x << 32) ^ (uint)y;
    }

    public sealed class RunRandom
    {
        private readonly System.Random _random;
        public int Seed { get; }

        public RunRandom(int seed)
        {
            Seed = seed == 0 ? 1 : seed;
            _random = new System.Random(Seed);
        }

        public float Value() => (float)_random.NextDouble();
        public bool Chance(float probability) => Value() < Mathf.Clamp01(probability);
        public int Range(int minimumInclusive, int maximumExclusive) =>
            maximumExclusive <= minimumInclusive ? minimumInclusive : _random.Next(minimumInclusive, maximumExclusive);
        public float Range(float minimumInclusive, float maximumInclusive) =>
            Mathf.Lerp(minimumInclusive, maximumInclusive, Value());
    }

    public sealed class PerformanceMetrics
    {
        public float FramesPerSecond { get; private set; } = 60f;
        public float FrameMilliseconds { get; private set; } = 16.67f;
        public float WorstFrameMilliseconds { get; private set; }
        public long SpatialQueries { get; private set; }

        private float _window;
        private float _worstInWindow;

        public void Tick(float unscaledDeltaTime, long spatialQueries)
        {
            if (unscaledDeltaTime <= 0f) return;
            var milliseconds = unscaledDeltaTime * 1000f;
            FrameMilliseconds = Mathf.Lerp(FrameMilliseconds, milliseconds, 0.08f);
            FramesPerSecond = 1000f / Mathf.Max(0.01f, FrameMilliseconds);
            _worstInWindow = Mathf.Max(_worstInWindow, milliseconds);
            _window += unscaledDeltaTime;
            if (_window >= 1f)
            {
                WorstFrameMilliseconds = _worstInWindow;
                _window = 0f;
                _worstInWindow = 0f;
            }
            SpatialQueries = spatialQueries;
        }
    }

    public static class ProductionFoundationChecks
    {
        private sealed class Point
        {
            public Vector2 Position;
        }

        public static bool Run(out string report)
        {
            var first = new RunRandom(7001);
            var second = new RunRandom(7001);
            for (var i = 0; i < 16; i++)
            {
                if (first.Range(-1000, 1000) == second.Range(-1000, 1000)) continue;
                report = "Deterministic random sequence mismatch.";
                return false;
            }

            var grid = new SpatialHashGrid<Point>(2f, point => point.Position);
            var near = new Point { Position = new Vector2(1f, 1f) };
            var far = new Point { Position = new Vector2(20f, 20f) };
            grid.Add(near);
            grid.Add(far);
            if (grid.FindNearest(Vector2.zero, 5f) != near)
            {
                report = "Spatial nearest query failed.";
                return false;
            }
            near.Position = new Vector2(30f, 30f);
            grid.Update(near);
            if (grid.FindNearest(Vector2.zero, 5f) != null)
            {
                report = "Spatial membership update failed.";
                return false;
            }

            if (!ProductionContentRuntime.Validate(out report)) return false;
            report = "Deterministic RNG, spatial grid and stable content IDs validated.";
            return true;
        }
    }
}
