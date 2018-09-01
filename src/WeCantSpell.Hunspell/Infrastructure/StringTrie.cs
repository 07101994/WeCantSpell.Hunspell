using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WeCantSpell.Hunspell.Infrastructure
{
    public class StringTrie<TValue> : IEnumerable<KeyValuePair<string, TValue>>
    {
        public StringTrie()
        {
            root = new Node();
        }

        private Node root;

        public bool IsEmpty => !root.HasValue && root.Children.Count == 0;

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

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

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
            var exisingNode = FindNode(key);
            if (exisingNode != null)
            {
                if (exisingNode.HasValue)
                {
                    throw new ArgumentException("Key already added", nameof(key));
                }

                exisingNode.SetValue(value);
                return;
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

                if (nodeAndKey.Value.Children != null)
                {
                    foreach (var child in nodeAndKey.Value.Children)
                    {
                        searchQueue.Enqueue(new KeyValuePair<string, Node>(nodeAndKey.Key.ConcatString(child.Key), child.Value));
                    }
                }
            }
        }

        class Node
        {
            public Dictionary<char, Node> Children;
            public TValue Value;
            public bool HasValue;

            public Node FindChild(char key)
            {
                if (Children == null)
                {
                    return null;
                }

                Children.TryGetValue(key, out var child);
                return child;
            }

            public void SetValue(TValue value)
            {
                Value = value;
                HasValue = true;
            }

            public Node GetOrCreateChild(char key)
            {
                if (Children.TryGetValue(key, out var child))
                {
                    return child;
                }

                child = new Node();
                if (Children == null)
                {
                    Children = new Dictionary<char, Node>();
                }

                Children[key] = child;
                return child;
            }
        }
    }
}
