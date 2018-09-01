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

        IEnumerable<KeyValuePair<string, Node>> GetAllNodes()
        {
            var searchQueue = new Queue<KeyValuePair<string, Node>>();
            searchQueue.Enqueue(new KeyValuePair<string, Node>(string.Empty, root));

            while (searchQueue.Count != 0)
            {
                var nodeAndKey = searchQueue.Dequeue();

                if (nodeAndKey.Value.HasValue)
                {
                    yield return nodeAndKey;
                }

                if (nodeAndKey.Value.HasChildren)
                {
                    var keyBuilder = StringBuilderPool.Get(nodeAndKey.Key, nodeAndKey.Key.Length + 1);
                    keyBuilder.Append(default(char));
                    foreach (var child in nodeAndKey.Value.Children)
                    {
                        keyBuilder[nodeAndKey.Key.Length] = child.Key;
                        searchQueue.Enqueue(new KeyValuePair<string, Node>(keyBuilder.ToString(), child.Value));
                    }

                    StringBuilderPool.Return(keyBuilder);
                }
            }
        }

        sealed class Node
        {
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

            public Node GetOrCreateChild(char key)
            {
                Node child;
                if (Children != null)
                {
                    if (Children.TryGetValue(key, out child))
                    {
                        return child;
                    }
                }
                else
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
