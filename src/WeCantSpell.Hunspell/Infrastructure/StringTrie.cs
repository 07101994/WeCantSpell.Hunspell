using System;
using System.Collections;
using System.Collections.Generic;

namespace WeCantSpell.Hunspell.Infrastructure
{
    sealed class StringTrie<TValue> : IEnumerable<KeyValuePair<ReadOnlyMemory<char>, TValue>>
    {
        public StringTrie()
        {
            root = new Node(ReadOnlyMemory<char>.Empty, 0, default);
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

        public bool ContainsKey(ReadOnlyMemory<char> key) => ContainsKey(key.Span);

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

        public bool TryGetValue(ReadOnlyMemory<char> key, out TValue value) => TryGetValue(key.Span, out value);

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
                var childNodeIndex = node.FindChildIndex(keyValue);
                if (childNodeIndex >= 0)
                {
                    node = node.Children[childNodeIndex];
                }
                else
                {
                    // TODO: see if keyslice can overwrite some smaller matching slices to save memory

                    var childNode = new Node(keySequence: keySlice, depth: depth, keyValue: keyValue);
                    node.InsertChild(~childNodeIndex, childNode);
                    node = childNode;
                }
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

                if ((maxDepth == -1 || Current.Depth <= maxDepth) && Current.HasChildren)
                {
                    HandleChildNodes();
                    return true;
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

            private void HandleChildNodes()
            {
                var children = Current.Children;
                if (children.Length > 5)
                {
                    visitQueue.PrepareCapacity(visitQueue.Count + children.Length - 1);
                }

                // TODO: may need to order these (in reverse) for alphabetical ordering
                visitQueue.AddRange(new ArraySegment<Node>(children, 1, children.Length - 1));

                Current = children[0];
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

        private readonly struct NodeKeyComparable : IComparable<Node>
        {
            public NodeKeyComparable(char key)
            {
                Key = key;
            }

            public readonly char Key;

            public int CompareTo(Node other) => Key.CompareTo(other.KeyValue);
        }

        internal sealed class Node
        {
            public ReadOnlyMemory<char> KeySequence;
            public Node[] Children;
            public int Depth;
            public TValue Value;
            public char KeyValue;
            public bool HasValue;

            public Node(ReadOnlyMemory<char> keySequence, int depth, char keyValue)
            {
                KeySequence = keySequence;
                Depth = depth;
                KeyValue = keyValue;
            }

            public bool HasChildren => Children != null && Children.Length != 0;

            public Node FindChild(char key)
            {
                var index = FindChildIndex(key);
                return index < 0 ? default : Children[index];
            }

            public int FindChildIndex(char key)
            {
                if (Children != null && Children.Length != 0)
                {
                    return Children.AsSpan().BinarySearch(new NodeKeyComparable(key));
                }

                return -1;
            }

            public void SetValue(TValue value)
            {
                Value = value;
                HasValue = true;
            }

            public void InsertChild(int index, Node childNode)
            {
                if (Children == null)
                {
                    Children = new [] { childNode };
                    return;
                }

                var newChildren = new Node[Children.Length + 1];
                if (index > 0)
                {
                    Children.AsSpan(0, index).CopyTo(newChildren.AsSpan());
                }
                newChildren[index] = childNode;
                if (index < Children.Length)
                {
                    Children.AsSpan(index).CopyTo(newChildren.AsSpan(index + 1));
                }

                Children = newChildren;
            }
        }
    }
}
