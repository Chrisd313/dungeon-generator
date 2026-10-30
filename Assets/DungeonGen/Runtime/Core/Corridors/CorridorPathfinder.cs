using System.Collections.Generic;
using UnityEngine;

namespace DungeonGen.Core
{
    /// <summary>
    /// A* over a rectangular cost map. Search state is (cell, heading) so direction changes can be charged
    /// a turn penalty. Ties break on insertion order, keeping results deterministic.
    /// Reuse one instance across searches on the same map to avoid reallocating.
    /// </summary>
    public sealed class CorridorPathfinder
    {
        readonly RectInt _bounds;
        readonly bool[] _blocked;
        readonly bool[] _corridor;

        // Per-state search data, invalidated by bumping _stamp instead of clearing.
        readonly float[] _g;
        readonly int[] _parent;
        readonly int[] _steps;
        readonly int[] _seen;
        readonly int[] _closed;
        int _stamp;

        readonly MinHeap _open = new MinHeap();

        public RectInt Bounds => _bounds;

        public CorridorPathfinder(RectInt bounds)
        {
            _bounds = bounds;
            int cells = bounds.width * bounds.height;
            _blocked = new bool[cells];
            _corridor = new bool[cells];
            _g = new float[cells * 4];
            _parent = new int[cells * 4];
            _steps = new int[cells * 4];
            _seen = new int[cells * 4];
            _closed = new int[cells * 4];
        }

        public bool InBounds(Vector2Int p) => RectUtility.Contains(_bounds, p);

        int Index(Vector2Int p) => (p.y - _bounds.yMin) * _bounds.width + (p.x - _bounds.xMin);

        Vector2Int Cell(int index) =>
            new Vector2Int(index % _bounds.width + _bounds.xMin, index / _bounds.width + _bounds.yMin);

        public bool IsBlocked(Vector2Int p) => !InBounds(p) || _blocked[Index(p)];

        public void SetBlocked(Vector2Int p, bool blocked)
        {
            if (InBounds(p)) _blocked[Index(p)] = blocked;
        }

        /// <summary>Blocks every cell of a rect (clipped to bounds).</summary>
        public void Block(RectInt r)
        {
            for (int y = r.yMin; y < r.yMax; y++)
            for (int x = r.xMin; x < r.xMax; x++)
                SetBlocked(new Vector2Int(x, y), true);
        }

        public bool IsCorridor(Vector2Int p) => InBounds(p) && _corridor[Index(p)];

        public void MarkCorridor(List<Vector2Int> path)
        {
            foreach (Vector2Int p in path)
                if (InBounds(p)) _corridor[Index(p)] = true;
        }

        /// <summary>
        /// Finds the cheapest path from <paramref name="start"/> to <paramref name="goal"/>, inclusive of both.
        /// Returns null if none exists within <paramref name="maxLength"/> cells or the expansion budget.
        /// </summary>
        /// <param name="startHeading">Direction the path is already travelling at the start (the socket's facing).</param>
        public List<Vector2Int> FindPath(Vector2Int start, Direction startHeading, Vector2Int goal,
            CorridorSettings settings, int maxLength, int maxExpansions = 200000)
        {
            if (IsBlocked(start) || IsBlocked(goal)) return null;
            if (start == goal) return new List<Vector2Int> { start };

            unchecked { _stamp++; }
            _open.Clear();

            float mergeCost = settings.allowMerging ? 0.5f : 1f;
            float minStep = Mathf.Min(1f, mergeCost);

            int startState = Index(start) * 4 + (int)startHeading;
            Visit(startState, 0f, -1, 1);
            _open.Push(Heuristic(start, goal, minStep), startState);

            int expansions = 0;
            while (_open.Count > 0)
            {
                int state = _open.Pop();
                if (_closed[state] == _stamp) continue;
                _closed[state] = _stamp;

                int cellIndex = state >> 2;
                var heading = (Direction)(state & 3);
                Vector2Int cell = Cell(cellIndex);
                if (cell == goal) return Reconstruct(state);
                if (++expansions > maxExpansions) return null;

                int steps = _steps[state];
                if (steps >= maxLength) continue;

                for (int d = 0; d < 4; d++)
                {
                    var dir = (Direction)d;
                    if (dir == heading.Opposite()) continue;

                    Vector2Int next = cell + dir.ToVector();
                    if (IsBlocked(next)) continue;

                    int nextIndex = Index(next);
                    float cost = _corridor[nextIndex] ? mergeCost : 1f;
                    if (dir != heading) cost += settings.turnPenalty;

                    int nextState = nextIndex * 4 + d;
                    if (_closed[nextState] == _stamp) continue;
                    float g = _g[state] + cost;
                    if (_seen[nextState] == _stamp && g >= _g[nextState]) continue;

                    Visit(nextState, g, state, steps + 1);
                    _open.Push(g + Heuristic(next, goal, minStep), nextState);
                }
            }
            return null;
        }

        void Visit(int state, float g, int parent, int steps)
        {
            _seen[state] = _stamp;
            _g[state] = g;
            _parent[state] = parent;
            _steps[state] = steps;
        }

        static float Heuristic(Vector2Int a, Vector2Int b, float minStep) =>
            (Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y)) * minStep;

        List<Vector2Int> Reconstruct(int state)
        {
            var path = new List<Vector2Int>(_steps[state]);
            for (int s = state; s >= 0; s = _parent[s])
                path.Add(Cell(s >> 2));
            path.Reverse();
            return path;
        }

        /// <summary>Binary min-heap on (priority, insertion order).</summary>
        sealed class MinHeap
        {
            struct Entry
            {
                public float priority;
                public int order;
                public int value;
            }

            Entry[] _items = new Entry[256];
            int _order;

            public int Count { get; private set; }

            public void Clear()
            {
                Count = 0;
                _order = 0;
            }

            public void Push(float priority, int value)
            {
                if (Count == _items.Length) System.Array.Resize(ref _items, _items.Length * 2);
                var e = new Entry { priority = priority, order = _order++, value = value };
                int i = Count++;
                while (i > 0)
                {
                    int p = (i - 1) >> 1;
                    if (!Less(e, _items[p])) break;
                    _items[i] = _items[p];
                    i = p;
                }
                _items[i] = e;
            }

            public int Pop()
            {
                int result = _items[0].value;
                Entry last = _items[--Count];
                int i = 0;
                while (true)
                {
                    int c = i * 2 + 1;
                    if (c >= Count) break;
                    if (c + 1 < Count && Less(_items[c + 1], _items[c])) c++;
                    if (!Less(_items[c], last)) break;
                    _items[i] = _items[c];
                    i = c;
                }
                if (Count > 0) _items[i] = last;
                return result;
            }

            static bool Less(Entry a, Entry b) =>
                a.priority < b.priority || (a.priority == b.priority && a.order < b.order);
        }
    }
}
