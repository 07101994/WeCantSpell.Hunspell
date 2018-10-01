namespace WeCantSpell.Hunspell
{
    public abstract class ReplacementEntry
    {
        protected ReplacementEntry(string pattern)
        {
            Pattern = pattern ?? string.Empty;
        }

        public string Pattern { get; }

        /// <seealso cref="ReplacementValueType.Med"/>
        public abstract string Med { get; }

        /// <seealso cref="ReplacementValueType.Ini"/>
        public abstract string Ini { get; }

        /// <seealso cref="ReplacementValueType.Fin"/>
        public abstract string Fin { get; }

        /// <seealso cref="ReplacementValueType.Isol"/>
        public abstract string Isol { get; }

        public abstract string this[ReplacementValueType type] { get; }

        internal string ExtractReplacementText(int remainingCharactersToReplace, bool atStart) =>
            this[atStart
                ? FindReplacementType_AtStart(remainingCharactersToReplace)
                : FindReplacementType_Normal(remainingCharactersToReplace)];

        private ReplacementValueType FindReplacementType_AtStart(int remainingCharactersToReplace)
        {
            var type = (remainingCharactersToReplace == Pattern.Length)
                    ? (ReplacementValueType.Fin | ReplacementValueType.Ini)
                    : (ReplacementValueType.Med | ReplacementValueType.Ini);

            for (; type != ReplacementValueType.Med && string.IsNullOrEmpty(this[type]); type--) ;

            return type;
        }

        private ReplacementValueType FindReplacementType_Normal(int remainingCharactersToReplace) =>
            (remainingCharactersToReplace == Pattern.Length)
            ? (string.IsNullOrEmpty(this[ReplacementValueType.Fin]) ? ReplacementValueType.Med : ReplacementValueType.Fin)
            : ReplacementValueType.Med;
    }
}
