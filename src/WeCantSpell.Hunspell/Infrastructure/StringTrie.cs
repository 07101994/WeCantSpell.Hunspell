using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace WeCantSpell.Hunspell.Infrastructure
{
    public class StringTrie<TValue> : IEnumerable<KeyValuePair<string, TValue>>
    {
        public StringTrie()
        {
            root = new Node();
        }

        private Node root;

        public bool IsEmpty => !root.HasValue && !root.HasChildren;

        public TValue this[string key] => this[key.AsSpan()];

        public TValue this[ReadOnlySpan<char> key]
        {
            get
            {
                if (TryGetValue(key, out var value))
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

        public IEnumerable<string> Keys => GetAllNodes().Select(x => x.Key);

        public IEnumerable<TValue> Values => GetAllNodes().Select(x => x.Value.Value);

        public IEnumerator<KeyValuePair<string, TValue>> GetEnumerator() => GetAllNodes()
            .Select(x => new KeyValuePair<string, TValue>(x.Key, x.Value.Value))
            .GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool ContainsKey(string key) => ContainsKey(key.AsSpan());

        public bool ContainsKey(ReadOnlySpan<char> key)
        {
            var node = TryFindNode(key);
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
            var node = TryFindNode(key);
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
            Add(key.AsSpan(), value);
        }

        public void Add(ReadOnlySpan<char> key, TValue value)
        {
            if (ContainsKey(key))
            {
                throw new ArgumentException("Key already added", nameof(key));
            }

            Set(key, value);
        }

        void Set(string key, TValue value)
        {
            Set(key.AsSpan(), value);
        }

        void Set(ReadOnlySpan<char> key, TValue value)
        {
            var node = root;
            int searchKeyIndex = 0;
            for (; searchKeyIndex < key.Length; searchKeyIndex++)
            {
                node = node.GetOrCreateChild(key[searchKeyIndex]);
            }

            node.SetValue(value);
        }

        Node TryFindNode(ReadOnlySpan<char> key)
        {
            var node = root;

            foreach (var keySegment in key)
            {
                var childIndex = node.FindChildIndex(keySegment);
                if (childIndex < 0)
                {
                    return null;
                }

                node = node.Children[childIndex].Node;
            }

            return node;
        }

        IEnumerable<KeyValuePair<string, Node>> GetAllNodes()
        {
            var searchQueue = new Queue<KeyValuePair<string, Node>>();

            var nodeAndKey = new KeyValuePair<string, Node>(string.Empty, root);
            do
            {
                if (nodeAndKey.Value.HasValue)
                {
                    yield return nodeAndKey;
                }

                if (nodeAndKey.Value.HasChildren)
                {
                    EnqueueAll(searchQueue, nodeAndKey.Value.Children, nodeAndKey.Key);
                }

                if (searchQueue.Count == 0)
                {
                    break;
                }

                nodeAndKey = searchQueue.Dequeue();
            }
            while (true);
        }

        void EnqueueAll(Queue<KeyValuePair<string, Node>> queue, KeyedNode[] items, string baseKey)
        {
            var keyBuilder = StringBuilderPool.Get(baseKey, baseKey.Length + 1);
            keyBuilder.Append(default(char));

            for (var i = 0; i < items.Length; ++i)
            {
                ref readonly var child = ref items[i];
                keyBuilder[baseKey.Length] = child.Key;
                queue.Enqueue(new KeyValuePair<string, Node>(keyBuilder.ToString(), child.Node));
            }

            StringBuilderPool.Return(keyBuilder);
        }

        readonly struct KeyedNode
        {
            public KeyedNode(char key)
            {
                Key = key;
                Node = new Node();
            }

            public readonly char Key;
            public readonly Node Node;
        }

        readonly struct KeyedNodeKeyComparable : IComparable<KeyedNode>
        {
            public KeyedNodeKeyComparable(char target)
            {
                Target = target;
            }

            public readonly char Target;

            public int CompareTo(KeyedNode other)
            {
                return Target.CompareTo(other.Key);
            }

            public int CompareTo(in KeyedNode other)
            {
                return Target.CompareTo(other.Key);
            }
        }

        sealed class Node
        {
            public KeyedNode[] Children;
            public TValue Value;
            public bool HasValue;

            public bool HasChildren => Children != null && Children.Length != 0;

            public int FindChildIndex(char key)
            {
                if (Children != null)
                {
                    var comparer = new KeyedNodeKeyComparable(key);
                    var children = new ReadOnlySpan<KeyedNode>(Children);
                    if (children.Length == 1)
                    {
                        if (children[0].Key == key)
                        {
                            return 0;
                        }
                    }
                    else if (children.Length <= 16)
                    {
                        return IndexOfKey(children, comparer);
                    }
                    else
                    {
                        var nodeIndex = children.BinarySearch(comparer);
                        if (nodeIndex >= 0)
                        {
                            return nodeIndex;
                        }
                    }
                }

                return -1;
            }

            public int IndexOfKey(ReadOnlySpan<KeyedNode> entries, KeyedNodeKeyComparable comparer)
            {
                for (var i = 0; i < entries.Length; i++)
                {
                    var cmp = comparer.CompareTo(in entries[i]);
                    if (cmp == 0)
                    {
                        return i;
                    }
                    if (cmp < 0)
                    {
                        break;
                    }
                }

                return -1;
            }

            public void SetValue(TValue value)
            {
                Value = value;
                HasValue = true;
            }

            public Node GetOrCreateChild(char key)
            {
                if (Children == null || Children.Length == 0)
                {
                    Children = new[] { new KeyedNode(key) };
                    return Children[0].Node;
                }

                var oldChildren = Children.AsSpan();
                var keyIndex = oldChildren.BinarySearch(new KeyedNodeKeyComparable(key));
                if (keyIndex >= 0)
                {
                    return oldChildren[keyIndex].Node;
                }

                keyIndex = ~keyIndex;

                var newChildren = new KeyedNode[oldChildren.Length + 1];

                if (keyIndex > 0)
                {
                    oldChildren.Slice(0, keyIndex).CopyTo(newChildren.AsSpan());
                }

                newChildren[keyIndex] = new KeyedNode(key);
                if (keyIndex < newChildren.Length + 1)
                {
                    oldChildren.Slice(keyIndex).CopyTo(newChildren.AsSpan(keyIndex + 1));
                }

                Children = newChildren;

                return Children[keyIndex].Node;
            }
        }
    }
}
