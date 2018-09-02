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

        public IEnumerable<ReadOnlyMemory<char>> Keys => GetAllNodes().Select(x => x.KeySequence);

        public IEnumerable<TValue> Values => GetAllNodes().Select(x => x.Value);

        public IEnumerator<KeyValuePair<ReadOnlyMemory<char>, TValue>> GetEnumerator() => GetAllNodes()
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
                var keySlice = key.Slice(0, searchKeyIndex + 1);
                ref readonly var keyValue = ref key.Span[searchKeyIndex];
                var childNode = node.GetChildOrDefault(keyValue);

                // TODO: see if keyslice can overwrite some smaller matching slices to save memory

                if (childNode == null)
                {
                    childNode = new Node
                    {
                        KeySequence = keySlice
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

        IEnumerable<Node> GetAllNodes()
        {
            var searchQueue = new Queue<Node>();
            searchQueue.Enqueue(root);

            while (searchQueue.Count != 0)
            {
                var node = searchQueue.Dequeue();

                if (node.HasValue)
                {
                    yield return node;
                }

                if (node.HasChildren)
                {
                    foreach (var childNode in node.Children.Values)
                    {
                        searchQueue.Enqueue(childNode);
                    }
                }
            }
        }

        sealed class Node
        {
            public ReadOnlyMemory<char> KeySequence;
            public Dictionary<char, Node> Children;
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

            public Node GetOrCreateChild(char key)
            {
                Node child = GetChildOrDefault(key);

                if (Children == null)
                {
                    Children = new Dictionary<char, Node>();
                }

                child = new Node();
                Children[key] = child;
                return child;
            }
        }
    }
}
