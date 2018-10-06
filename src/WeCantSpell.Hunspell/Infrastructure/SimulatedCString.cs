using System;

#if !NO_INLINE
using System.Runtime.CompilerServices;
#endif

namespace WeCantSpell.Hunspell.Infrastructure
{
    ref struct SimulatedCString
    {
        public SimulatedCString(ReadOnlySpan<char> text)
        {
            buffer = text.ToArray();
            bufferSpan = buffer.AsSpan();
            cachedSpan = bufferSpan;
            cachedString = null;
            cacheRequiresRefresh = true;
        }

        private char[] buffer;
        private Span<char> bufferSpan;
        private string cachedString;
        private Span<char> cachedSpan;
        private bool cacheRequiresRefresh;

        public char this[int index]
        {
            get => index < 0 || index >= buffer.Length ? '\0' : buffer[index];
            set
            {
                ResetCache();
                buffer[index] = value;
            }
        }

        public int BufferLength
        {
#if !NO_INLINE
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
            get => buffer.Length;
        }

        public void WriteChars(string text, int destinationIndex)
        {
            ResetCache();

            var neededLength = text.Length + destinationIndex;
            if (buffer.Length < neededLength)
            {
                Array.Resize(ref buffer, neededLength);
                bufferSpan = buffer.AsSpan();
            }

            text.CopyTo(0, buffer, destinationIndex, text.Length);
        }

        public void WriteChars(ReadOnlySpan<char> text, int destinationIndex)
        {
            ResetCache();

            var neededLength = text.Length + destinationIndex;
            if (buffer.Length < neededLength)
            {
                Array.Resize(ref buffer, neededLength);
                bufferSpan = buffer.AsSpan();
            }

            text.CopyTo(bufferSpan.Slice(destinationIndex));
        }

        public void Assign(ReadOnlySpan<char> text)
        {
#if DEBUG
            if (text.Length > buffer.Length) throw new ArgumentOutOfRangeException(nameof(text));
#endif
            ResetCache();

            text.CopyTo(bufferSpan);

            if (text.Length < bufferSpan.Length)
            {
                bufferSpan.Slice(text.Length).Clear();
            }
        }

        public void Destroy()
        {
            ResetCache();
            buffer = null;
            bufferSpan = Span<char>.Empty;
        }

        public override string ToString() =>
            cachedString ?? (cachedString = GetTerminatedSpan().ToString());

        public Span<char> GetTerminatedSpan()
        {
            if (cacheRequiresRefresh)
            {
                cacheRequiresRefresh = false;
                cachedSpan = bufferSpan.Slice(0, FindTerminatedLength());
            }

            return cachedSpan;
        }

        private void ResetCache()
        {
            cacheRequiresRefresh = true;
            cachedString = null;
        }

#if !NO_INLINE
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private int FindTerminatedLength()
        {
            var length = Array.IndexOf(buffer, '\0');
            return length < 0 ? buffer.Length : length;
        }
    }
}
