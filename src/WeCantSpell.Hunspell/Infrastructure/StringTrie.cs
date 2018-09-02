using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace WeCantSpell.Hunspell.Infrastructure
{
    public class StringTrie<TValue> : IEnumerable<KeyValuePair<ReadOnlyMemory<char>, TValue>>
    {
        public StringTrie()
        {
            root = new Node
            {
                KeySequence = ReadOnlyMemory<char>.Empty
            };
        }

        private Node root;

        public bool IsEmpty => !root.HasValue && !root.HasChildren;

        public TValue this[string key] => this[key.AsMemory()];

        public TValue this[ReadOnlyMemory<char> key]
        {
            get
            {
                if (TryGetValue(key.Span, out var value))
                {
                    return value;
                }

                throw new KeyNotFoundException();
            }
            set
            {
                Set(key, value);
            }
        }

        public IEnumerable<ReadOnlyMemory<char>> Keys => GetAllNodesWithValue().Select(x => x.KeySequence);

        public IEnumerable<TValue> Values => GetAllNodesWithValue().Select(x => x.Value);

        public IEnumerator<KeyValuePair<ReadOnlyMemory<char>, TValue>> GetEnumerator() => GetAllNodesWithValue()
            .Select(n => new KeyValuePair<ReadOnlyMemory<char>, TValue>(n.KeySequence, n.Value))
            .GetEnumerator();

        public IEnumerator<KeyValuePair<ReadOnlyMemory<char>, TValue>> GetEnumerator(int maxDepth) => GetAllNodesWithValue(maxDepth)
            .Select(n => new KeyValuePair<ReadOnlyMemory<char>, TValue>(n.KeySequence, n.Value))
            .GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool ContainsKey(string key) => ContainsKey(key.AsSpan());

        public bool ContainsKey(ReadOnlySpan<char> key)
        {
            var node = FindNode(key);
            return node != null && node.HasValue;
        }

        public bool TryGetValue(string key, out TValue value)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            return TryGetValue(key.AsSpan(), out value);
        }

        public bool TryGetValue(ReadOnlySpan<char> key, out TValue value)
        {
            var node = FindNode(key);
            if (node != null && node.HasValue)
            {
                value = node.Value;
                return true;
            }

            value = default;
            return false;
        }

        public void Add(string key, TValue value)
        {
            Add(key.AsMemory(), value);
        }

        public void Add(ReadOnlyMemory<char> key, TValue value)
        {
            if (ContainsKey(key.Span))
            {
                throw new ArgumentException("Key already added", nameof(key));
            }

            Set(key, value);
        }

        void Set(string key, TValue value)
        {
            Set(key.AsMemory(), value);
        }

        void Set(ReadOnlyMemory<char> key, TValue value)
        {
            var node = root;
            int searchKeyIndex = 0;
            for (; searchKeyIndex < key.Length; searchKeyIndex++)
            {
                var depth = searchKeyIndex + 1;
                var keySlice = key.Slice(0, depth);
                ref readonly var keyValue = ref key.Span[searchKeyIndex];
                var childNode = node.GetChildOrDefault(keyValue);

                // TODO: see if keyslice can overwrite some smaller matching slices to save memory

                if (childNode == null)
                {
                    childNode = new Node
                    {
                        KeySequence = keySlice,
                        Depth = depth
                    };
                    node.AddChild(keyValue, childNode);
                }

                node = childNode;
            }

            node.SetValue(value);
        }

        Node FindNode(ReadOnlySpan<char> key)
        {
            var node = root;

            foreach (var keySegment in key)
            {
                node = node.FindChild(keySegment);
                if (node == null)
                {
                    return null;
                }
            }

            return node;
        }

        IEnumerable<Node> GetAllNodesWithValue(int maxDepth = -1)
        {
            var enumerator = GetAllNodeEnumerator(maxDepth);
            while (enumerator.MoveNext())
            {
                var node = enumerator.Current;
                if (node.HasValue)
                {
                    yield return node;
                }
            }
        }

        NodeEnumerator GetAllNodeEnumerator(int maxDepth)
        {
            return new NodeEnumerator(root, maxDepth);
        }

        struct NodeEnumerator : IEnumerator<Node>
        {
            public NodeEnumerator(Node root, int maxDepth)
            {
                this.root = root;
                this.maxDepth = maxDepth;
                hasReachedEnd = false;
                visitQueue = new List<Node>();
                Current = null;
            }

            Node root;

            int maxDepth;

            bool hasReachedEnd;

            private List<Node> visitQueue;

            public Node Current { get; private set; }

            object IEnumerator.Current => Current;

            public bool MoveNext()
            {
                if (Current == null)
                {
                    if (hasReachedEnd || root == null)
                    {
                        return false;
                    }

                    Current = root;
                    return true;
                }

                if ((maxDepth == -1 || Current.Depth <= maxDepth) && Current.Children != null)
                {
                    if (HandleChildNodes())
                    {
                        return true;
                    }
                }

                if (visitQueue.Count != 0)
                {
                    Current = visitQueue.Dequeue();
                    return true;
                }

                Current = null;
                hasReachedEnd = true;
                return false;
            }

            private bool HandleChildNodes()
            {
                var childCount = Current.Children.Count;
                if (childCount > 0)
                {
                    // TODO: may need to order these for alphabetical ordering
                    using (var childrenEnumerator = Current.Children.Values.GetEnumerator())
                    {
                        if (childrenEnumerator.MoveNext())
                        {
                            var firstChild = childrenEnumerator.Current;
                            if (childCount != 1)
                            {
                                if (childCount > 3)
                                {
                                    var neededCapacity = visitQueue.Count + childCount - 1;
                                    if (visitQueue.Capacity < neededCapacity)
                                    {
                                        visitQueue.Capacity = Math.Max(visitQueue.Capacity * 2, neededCapacity);
                                    }

                                }

                                while (childrenEnumerator.MoveNext())
                                {
                                    // TODO: may need to order these (in reverse) for alphabetical ordering
                                    visitQueue.Add(childrenEnumerator.Current);
                                }
                            }

                            Current = firstChild;
                            return true;
                        }
                    }
                }

                return false;
            }

            public void Reset()
            {
                hasReachedEnd = false;
                visitQueue.Clear();
                Current = null;
            }

            public void Dispose()
            {
                root = null;
                hasReachedEnd = true;
                visitQueue = null;
            }
        }

        sealed class Node
        {
            public ReadOnlyMemory<char> KeySequence;
            public Dictionary<char, Node> Children;
            public int Depth;
            public TValue Value;
            public bool HasValue;

            public bool HasChildren => Children != null && Children.Count != 0;

            public Node FindChild(char key)
            {
                if (Children != null && Children.TryGetValue(key, out var child))
                {
                    return child;
                }

                return null;
            }

            public void SetValue(TValue value)
            {
                Value = value;
                HasValue = true;
            }

            public Node GetChildOrDefault(char key)
            {
                if (Children != null && Children.TryGetValue(key, out var child))
                {
                    return child;
                }

                return null;
            }

            public void AddChild(char key, Node childNode)
            {
                if (Children == null)
                {
                    Children = new Dictionary<char, Node>();
                }

                Children[key] = childNode;
            }
        }
    }
}
