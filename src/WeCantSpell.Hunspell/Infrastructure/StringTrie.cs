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

        public IEnumerable<ReadOnlyMemory<char>> Keys
        {
            get
            {
                using (var enumerator = GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        yield return enumerator.Current.Key;
                    }
                }
            }
        }

        public IEnumerable<TValue> Values
        {
            get
            {
                using (var enumerator = GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        yield return enumerator.Current.Value;
                    }
                }
            }
        }

        public Enumerator GetEnumerator() => GetEnumerator(-1);

        public Enumerator GetEnumerator(int maxDepth) => new Enumerator(GetAllNodeEnumerator(maxDepth));

        IEnumerator<KeyValuePair<ReadOnlyMemory<char>, TValue>> IEnumerable<KeyValuePair<ReadOnlyMemory<char>, TValue>>.GetEnumerator() => GetEnumerator(-1);

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

        NodeEnumerator GetAllNodeEnumerator(int maxDepth)
        {
            return new NodeEnumerator(root, maxDepth);
        }

        public struct Enumerator : IEnumerator<KeyValuePair<ReadOnlyMemory<char>, TValue>>
        {
            internal Enumerator(NodeEnumerator enumerator)
            {
                this.enumerator = enumerator;
                Current = default;
            }

            NodeEnumerator enumerator;

            public KeyValuePair<ReadOnlyMemory<char>, TValue> Current { get; private set; }

            object IEnumerator.Current => Current;

            public void Dispose()
            {
                enumerator.Dispose();
            }

            public bool MoveNext()
            {
                while (enumerator.MoveNext())
                {
                    var node = enumerator.Current;
                    if (node.HasValue)
                    {
                        Current = new KeyValuePair<ReadOnlyMemory<char>, TValue>(node.KeySequence, node.Value);
                        return true;
                    }
                }

                Current = default;
                return false;
            }

            public void Reset()
            {
                enumerator.Reset();
            }
        }

        internal struct NodeEnumerator : IEnumerator<Node>
        {
            public NodeEnumerator(Node root, int maxDepth)
            {
                this.root = root;
                this.maxDepth = maxDepth;
                hasReachedEnd = false;
                visitQueue = new List<Node>();
                Current = default;
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
                if (childCount != 0)
                {
                    // TODO: may need to order these for alphabetical ordering
                    using (var childrenEnumerator = Current.Children.Values.GetEnumerator())
                    {
                        if (childrenEnumerator.MoveNext())
                        {
                            Current = childrenEnumerator.Current;

                            if (childCount > 5)
                            {
                                visitQueue.PrepareCapacity(visitQueue.Count + childCount - 1);
                            }

                            while (childrenEnumerator.MoveNext())
                            {
                                // TODO: may need to order these (in reverse) for alphabetical ordering
                                visitQueue.Add(childrenEnumerator.Current);
                            }

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

        internal sealed class Node
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
