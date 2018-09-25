using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using WeCantSpell.Hunspell.Infrastructure;

namespace WeCantSpell.Hunspell
{
    public abstract class AffixCollection<TEntry> :
        IEnumerable<AffixEntryGroup<TEntry>>
        where TEntry : AffixEntry
    {
        internal delegate TResult Constructor<TResult>(
            Dictionary<FlagValue, AffixEntryGroup<TEntry>> affixesByFlag,
            Dictionary<char, AffixEntryGroupCollection<TEntry>> affixesByIndexedByKey,
            AffixEntryGroupCollection<TEntry> affixesWithDots,
            AffixEntryGroupCollection<TEntry> affixesWithEmptyKeys,
            FlagSet contClasses)
            where TResult : AffixCollection<TEntry>;

        internal static TResult Create<TResult>(List<AffixEntryGroup<TEntry>.Builder> builders, Constructor<TResult> constructor)
            where TResult : AffixCollection<TEntry>
        {
            var affixesByFlag = new Dictionary<FlagValue, AffixEntryGroup<TEntry>>(builders.Count);
            var affixesByIndexedByKeyBuilders = new Dictionary<char, Dictionary<FlagValue, AffixEntryGroup<TEntry>.Builder>>();
            var affixesWithEmptyKeys = new List<AffixEntryGroup<TEntry>>();
            var affixesWithDots = new List<AffixEntryGroup<TEntry>>();
            var contClasses = new HashSet<FlagValue>();

            if (builders != null)
            {
                foreach (var builder in builders)
                {
                    var group = builder.ToGroup();
                    affixesByFlag.Add(group.AFlag, group);

                    var entriesWithNoKey = new List<TEntry>();
                    var entriesWithDots = new List<TEntry>();

                    foreach (var entry in group.Entries)
                    {
                        var key = entry.Key;

                        contClasses.UnionWith(entry.ContClass);

                        if (string.IsNullOrEmpty(key))
                        {
                            entriesWithNoKey.Add(entry);
                        }
                        else if (key.Contains('.'))
                        {
                            entriesWithDots.Add(entry);
                        }
                        else
                        {
                            var indexedKey = key[0];
                            if (!affixesByIndexedByKeyBuilders.TryGetValue(indexedKey, out var keyedAffixes))
                            {
                                keyedAffixes = new Dictionary<FlagValue, AffixEntryGroup<TEntry>.Builder>();
                                affixesByIndexedByKeyBuilders.Add(indexedKey, keyedAffixes);
                            }

                            if (!keyedAffixes.TryGetValue(group.AFlag, out var groupBuilder))
                            {
                                groupBuilder = new AffixEntryGroup<TEntry>.Builder
                                {
                                    AFlag = group.AFlag,
                                    Options = group.Options,
                                    Entries = new List<TEntry>()
                                };
                                keyedAffixes.Add(group.AFlag, groupBuilder);
                            }

                            groupBuilder.Entries.Add(entry);
                        }
                    }

                    if (entriesWithNoKey.Count > 0)
                    {
                        affixesWithEmptyKeys.Add(new AffixEntryGroup<TEntry>(group.AFlag, group.Options, AffixEntryCollection<TEntry>.Create(entriesWithNoKey)));
                    }
                    if (entriesWithDots.Count > 0)
                    {
                        affixesWithDots.Add(new AffixEntryGroup<TEntry>(group.AFlag, group.Options, AffixEntryCollection<TEntry>.Create(entriesWithDots)));
                    }
                }
            }

            var affixesByIndexedByKey = new Dictionary<char, AffixEntryGroupCollection<TEntry>>(affixesByIndexedByKeyBuilders.Count);
            foreach (var keyedBuilder in affixesByIndexedByKeyBuilders)
            {
                var indexedAffixGroup = new AffixEntryGroup<TEntry>[keyedBuilder.Value.Count];
                var writeIndex = 0;
                foreach(var builderPair in keyedBuilder.Value)
                {
                    indexedAffixGroup[writeIndex++] = builderPair.Value.ToGroup();
                }

                affixesByIndexedByKey.Add(keyedBuilder.Key, AffixEntryGroupCollection<TEntry>.TakeArray(indexedAffixGroup));
            }

            return constructor
            (
                affixesByFlag,
                affixesByIndexedByKey,
                AffixEntryGroupCollection<TEntry>.Create(affixesWithDots),
                AffixEntryGroupCollection<TEntry>.Create(affixesWithEmptyKeys),
                FlagSet.Create(contClasses)
            );
        }

        internal AffixCollection(
            Dictionary<FlagValue, AffixEntryGroup<TEntry>> affixesByFlag,
            Dictionary<char, AffixEntryGroupCollection<TEntry>> affixesByIndexedByKey,
            AffixEntryGroupCollection<TEntry> affixesWithDots,
            AffixEntryGroupCollection<TEntry> affixesWithEmptyKeys,
            FlagSet contClasses)
        {
            AffixesByFlag = affixesByFlag;
            AffixesByIndexedByKey = affixesByIndexedByKey;
            AffixesWithDots = affixesWithDots;
            AffixesWithEmptyKeys = affixesWithEmptyKeys;
            ContClasses = contClasses;
            HasAffixes = affixesByFlag.Count != 0;
        }

        protected Dictionary<FlagValue, AffixEntryGroup<TEntry>> AffixesByFlag { get; }

        protected Dictionary<char, AffixEntryGroupCollection<TEntry>> AffixesByIndexedByKey { get; }

        public AffixEntryGroupCollection<TEntry> AffixesWithDots { get; }

        public AffixEntryGroupCollection<TEntry> AffixesWithEmptyKeys { get; }

        public FlagSet ContClasses { get; }

        public bool HasAffixes { get; }

        public IEnumerable<FlagValue> FlagValues => AffixesByFlag.Keys;

        public AffixEntryGroup<TEntry> GetByFlag(FlagValue flag)
        {
            AffixesByFlag.TryGetValue(flag, out AffixEntryGroup<TEntry> result);
            return result;
        }

        public IEnumerator<AffixEntryGroup<TEntry>> GetEnumerator() => AffixesByFlag.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => AffixesByFlag.Values.GetEnumerator();

        internal GroupsForFlagsFilterEnumerable GetByFlags(FlagSet flags)
        {
#if DEBUG
            if (flags == null) throw new ArgumentNullException(nameof(flags));
#endif

            return new GroupsForFlagsFilterEnumerable(flags.items, AffixesByFlag);
        }

        internal readonly struct GroupsForFlagsFilterEnumerable : IEnumerable<AffixEntryGroup<TEntry>>
        {
            internal GroupsForFlagsFilterEnumerable(FlagValue[] flags, Dictionary<FlagValue, AffixEntryGroup<TEntry>> affixesByFlag)
            {
                this.flags = flags;
                this.affixesByFlag = affixesByFlag;
            }

            private readonly FlagValue[] flags;

            private readonly Dictionary<FlagValue, AffixEntryGroup<TEntry>> affixesByFlag;

            public Enumerator GetEnumerator() => new Enumerator(flags, affixesByFlag);

            IEnumerator<AffixEntryGroup<TEntry>> IEnumerable<AffixEntryGroup<TEntry>>.GetEnumerator() => GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public struct Enumerator : IEnumerator<AffixEntryGroup<TEntry>>
            {
                internal Enumerator(FlagValue[] flags, Dictionary<FlagValue, AffixEntryGroup<TEntry>> affixesByFlag)
                {
                    this.flags = flags;
                    this.affixesByFlag = affixesByFlag;
                    flagEnumeratorIndex = -1;
                    Current = null;
                }

                private int flagEnumeratorIndex;

                private readonly FlagValue[] flags;

                private readonly Dictionary<FlagValue, AffixEntryGroup<TEntry>> affixesByFlag;

                public AffixEntryGroup<TEntry> Current { get; private set; }

                object IEnumerator.Current => Current;

                public void Dispose()
                {
                }

                public bool MoveNext()
                {
                    while ((++flagEnumeratorIndex) < flags.Length)
                    {
                        if (affixesByFlag.TryGetValue(flags[flagEnumeratorIndex], out var result))
                        {
                            Current = result;
                            return true;
                        }
                    }

                    Current = null;
                    return false;
                }

                public void Reset()
                {
                    flagEnumeratorIndex = -1;
                    Current = null;
                }
            }
        }

        internal EmptyKeyGroupsForFlagsFilterEnumerable GetAffixesWithEmptyKeysAndFlag(FlagSet flags)
        {
#if DEBUG
            if (flags == null) throw new ArgumentNullException(nameof(flags));
#endif

            return new EmptyKeyGroupsForFlagsFilterEnumerable(flags, AffixesWithEmptyKeys.items);
        }

        internal readonly struct EmptyKeyGroupsForFlagsFilterEnumerable : IEnumerable<AffixEntryGroup<TEntry>>
        {
            internal EmptyKeyGroupsForFlagsFilterEnumerable(FlagSet flags, AffixEntryGroup<TEntry>[] affixesWithEmptyKeys)
            {
                this.flags = flags;
                this.affixesWithEmptyKeys = affixesWithEmptyKeys;
            }

            private readonly FlagSet flags;

            private readonly AffixEntryGroup<TEntry>[] affixesWithEmptyKeys;

            public Enumerator GetEnumerator() => new Enumerator(flags, affixesWithEmptyKeys);

            IEnumerator<AffixEntryGroup<TEntry>> IEnumerable<AffixEntryGroup<TEntry>>.GetEnumerator() => GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public struct Enumerator : IEnumerator<AffixEntryGroup<TEntry>>
            {
                internal Enumerator(FlagSet flags, AffixEntryGroup<TEntry>[] affixesWithEmptyKeys)
                {
                    this.flags = flags;
                    this.affixesWithEmptyKeys = affixesWithEmptyKeys;
                    groupIndex = -1;
                    Current = null;
                }

                private int groupIndex;

                private readonly FlagSet flags;

                private readonly AffixEntryGroup<TEntry>[] affixesWithEmptyKeys;

                public AffixEntryGroup<TEntry> Current { get; private set; }

                object IEnumerator.Current => Current;

                public void Dispose()
                {
                }

                public bool MoveNext()
                {
                    while ((++groupIndex) < affixesWithEmptyKeys.Length)
                    {
                        ref readonly var group = ref affixesWithEmptyKeys[groupIndex];
                        if (flags.Contains(group.AFlag))
                        {
                            Current = group;
                            return true;
                        }
                    }

                    Current = null;
                    return false;
                }

                public void Reset()
                {
                    groupIndex = -1;
                    Current = null;
                }
            }
        }

        internal IEnumerable<Affix<TEntry>> GetMatchingWithDotAffixes(string word, Func<string, string, bool> predicate) =>
            AffixesWithDots.SelectMany(group =>
                group.Entries
                    .Where(entry => predicate(entry.Key, word))
                    .Select(entry => Affix<TEntry>.Create(entry, group)));
    }

    public sealed class SuffixCollection : AffixCollection<SuffixEntry>
    {
        public static readonly SuffixCollection Empty = new SuffixCollection(
            new Dictionary<FlagValue, AffixEntryGroup<SuffixEntry>>(0),
            new Dictionary<char, AffixEntryGroupCollection<SuffixEntry>>(0),
            AffixEntryGroupCollection<SuffixEntry>.Empty,
            AffixEntryGroupCollection<SuffixEntry>.Empty,
            FlagSet.Empty);

        public static SuffixCollection Create(List<AffixEntryGroup<SuffixEntry>.Builder> builders)
        {
            if (builders == null || builders.Count == 0)
            {
                return Empty;
            }

            return Create(
                builders,
                (affixesByFlag, affixesByIndexedByKey, affixesWithDots, affixesWithEmptyKeys, contClasses) =>
                    new SuffixCollection(affixesByFlag, affixesByIndexedByKey, affixesWithDots, affixesWithEmptyKeys, contClasses));
        }

        private SuffixCollection(
            Dictionary<FlagValue, AffixEntryGroup<SuffixEntry>> affixesByFlag,
            Dictionary<char, AffixEntryGroupCollection<SuffixEntry>> affixesByIndexedByKey,
            AffixEntryGroupCollection<SuffixEntry> affixesWithDots,
            AffixEntryGroupCollection<SuffixEntry> affixesWithEmptyKeys,
            FlagSet contClasses)
            : base(affixesByFlag, affixesByIndexedByKey, affixesWithDots, affixesWithEmptyKeys, contClasses) { }

        internal List<Affix<SuffixEntry>> GetMatchingAffixes(string word, FlagSet groupFlagFilter = null)
        {
            var results = new List<Affix<SuffixEntry>>();

            if (!string.IsNullOrEmpty(word))
            {
                if (AffixesByIndexedByKey.TryGetValue(word[word.Length - 1], out AffixEntryGroupCollection<SuffixEntry> indexedGroups))
                {
                    foreach (var group in indexedGroups)
                    {
                        if (groupFlagFilter == null || groupFlagFilter.Contains(group.AFlag))
                        {
                            foreach (var entry in group.Entries)
                            {
                                if (HunspellTextFunctions.IsReverseSubset(entry.Key, word))
                                {
                                    results.Add(Affix<SuffixEntry>.Create(entry, group));
                                }
                            }
                        }
                    }
                }

                if (AffixesWithDots.HasItems)
                {
                    results.AddRange(GetMatchingWithDotAffixes(word, HunspellTextFunctions.IsReverseSubset));
                }
            }

            return results;
        }
    }

    public sealed class PrefixCollection : AffixCollection<PrefixEntry>
    {
        public static readonly PrefixCollection Empty = new PrefixCollection(
            new Dictionary<FlagValue, AffixEntryGroup<PrefixEntry>>(0),
            new Dictionary<char, AffixEntryGroupCollection<PrefixEntry>>(0),
            AffixEntryGroupCollection<PrefixEntry>.Empty,
            AffixEntryGroupCollection<PrefixEntry>.Empty,
            FlagSet.Empty);

        public static PrefixCollection Create(List<AffixEntryGroup<PrefixEntry>.Builder> builders)
        {
            if (builders == null || builders.Count == 0)
            {
                return Empty;
            }

            return Create(
                builders,
                (affixesByFlag, affixesByIndexedByKey, affixesWithDots, affixesWithEmptyKeys, contClasses) =>
                    new PrefixCollection(affixesByFlag, affixesByIndexedByKey, affixesWithDots, affixesWithEmptyKeys, contClasses));
        }

        private PrefixCollection(
            Dictionary<FlagValue, AffixEntryGroup<PrefixEntry>> affixesByFlag,
            Dictionary<char, AffixEntryGroupCollection<PrefixEntry>> affixesByIndexedByKey,
            AffixEntryGroupCollection<PrefixEntry> affixesWithDots,
            AffixEntryGroupCollection<PrefixEntry> affixesWithEmptyKeys,
            FlagSet contClasses)
            : base(affixesByFlag, affixesByIndexedByKey, affixesWithDots, affixesWithEmptyKeys, contClasses) { }

        internal List<Affix<PrefixEntry>> GetMatchingAffixes(string word)
        {
            var results = new List<Affix<PrefixEntry>>();

            if (!string.IsNullOrEmpty(word))
            {
                if (AffixesByIndexedByKey.TryGetValue(word[0], out AffixEntryGroupCollection<PrefixEntry> indexedGroups))
                {
                    foreach (var group in indexedGroups)
                    {
                        foreach (var entry in group.Entries)
                        {
                            if (HunspellTextFunctions.IsSubset(entry.Key, word))
                            {
                                results.Add(Affix<PrefixEntry>.Create(entry, group));
                            }
                        }
                    }
                }

                if (AffixesWithDots.HasItems)
                {
                    results.AddRange(GetMatchingWithDotAffixes(word, HunspellTextFunctions.IsSubset));
                }
            }

            return results;
        }
    }
}
