using System;
using System.Collections;
using System.Diagnostics.Metrics;
using System.IO;
using System.Text;

namespace SpellChecker
{
    public class SpellDictionary : IDisposable
    {
        #region Variables
        protected const int defaultMaxEditDistance = 2;
        protected const int defaultPrefixLength = 7;
        protected const int defaultCountThreshold = 1;
        protected const int defaultInitialCapacity = 16;
        protected const int defaultCompactLevel = 5;

        protected readonly int initialCapacity;
        protected readonly int maxDictionaryEditDistance;
        //prefix length  5..7
        protected readonly int prefixLength;
        protected readonly uint compactMask;
        // a treshold might be specifid, when a term occurs so frequently in the corpus that it is considered a valid word for spelling correction
        protected readonly long countThreshold;

        // maximum dictionary term length
        protected int maxDictionaryWordLength;

        // Dictionary of unique correct spelling words, and the frequency count for each word.
        protected readonly Dictionary<string, long> words;

        // Dictionary that contains a mapping of lists of suggested correction words to the hashCodes
        // of the original words and the deletes derived from them. Collisions of hashCodes is tolerated,
        // because suggestions are ultimately verified via an edit distance function.
        // A list of suggestions might have a single suggestion, or multiple suggestions. 
        protected Dictionary<int, string[]> deletes = null;

        // Dictionary of unique words that are below the count threshold for being considered correct spellings.
        protected Dictionary<string, long> belowThresholdWords = new Dictionary<string, long>();
        #endregion

        #region Constructor
        public SpellDictionary(int initialCapacity = defaultInitialCapacity, 
            int maxDictionaryEditDistance = defaultMaxEditDistance, int prefixLength = defaultPrefixLength, 
            int countThreshold = defaultCountThreshold, byte compactLevel = defaultCompactLevel)
        {
            if (initialCapacity < 0) throw new ArgumentOutOfRangeException(nameof(initialCapacity));
            if (maxDictionaryEditDistance < 0) throw new ArgumentOutOfRangeException(nameof(maxDictionaryEditDistance));
            if (prefixLength < 1 || prefixLength <= maxDictionaryEditDistance) throw new ArgumentOutOfRangeException(nameof(prefixLength));
            if (countThreshold < 0) throw new ArgumentOutOfRangeException(nameof(countThreshold));
            if (compactLevel > 16) throw new ArgumentOutOfRangeException(nameof(compactLevel));

            this.initialCapacity = initialCapacity;
            this.words = new Dictionary<string, long>(initialCapacity);
            this.maxDictionaryEditDistance = maxDictionaryEditDistance;
            this.prefixLength = prefixLength;
            this.countThreshold = countThreshold;
            if (compactLevel > 16) compactLevel = 16;
            this.compactMask = (uint.MaxValue >> (3 + compactLevel)) << 2;
        }
        #endregion

        #region Properties
        /// <summary>
        /// Maximum edit distance for dictionary precalculation.
        /// </summary>
        public int MaxDictionaryEditDistance
        {
            get => maxDictionaryEditDistance;
        }

        /// <summary>
        /// Length of prefix, from which deletes are generated.
        /// </summary>
        public int PrefixLength
        {
            get => prefixLength;
        }

        /// <summary>
        /// Length of longest word in the dictionary.
        /// </summary>
        public int MaxLength
        {
            get => maxDictionaryWordLength;
        }

        /// <summary>
        /// Count threshold for a word to be considered a valid word for spelling correction.
        /// </summary>
        public long CountThreshold
        {
            get => countThreshold;
        }

        /// <summary>
        /// Number of unique words in the dictionary.
        /// </summary>
        public int WordCount
        {
            get => words.Count;
        }

        /// <summary>
        /// Number of word prefixes and intermediate word deletes encoded in the dictionary.
        /// </summary>
        public int EntryCount
        {
            get => deletes.Count;
        }
        #endregion

        #region Methods
        public bool LoadDictionary(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return false;
            if (!File.Exists(filePath)) return false;

            try
            {
                bool result = false;
                using (var stream = File.OpenRead(filePath))
                {
                    result = LoadDictionary(stream);
                }
                return result;
            }
            catch(Exception)
            { }
            
            return false;
        }

        public bool LoadDictionary(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException();
            if (!stream.CanRead) throw new IOException();

            words.Clear();

            using (StreamReader sr = new StreamReader(stream, Encoding.UTF8, false))
            {
                string line;
                while((line = sr.ReadLine()) != null)
                {
                    string[] parts = line.Split();
                    if (parts.Length > 1)
                    {
                        int countIndex = parts.Length - 1;
                        if (long.TryParse(parts[countIndex], out long count))
                        {
                            parts.SetValue(string.Empty, countIndex);
                            string key = string.Join(' ', parts).Trim();

                            CreateDictionaryEntry(key, count);
                        }
                    }
                }
            }

            return true;
        }

        public bool SaveAs(Stream stream)
        {
            try
            {
                if (stream == null) throw new ArgumentNullException();
                if (!stream.CanWrite) throw new IOException();

                if (words.Count > 0)
                {
                    if (stream.CanSeek)
                    {
                        stream.Seek(0, SeekOrigin.Begin);
                    }

                    using (var writer = new StreamWriter(stream, Encoding.UTF8))
                    {
                        foreach (var word in words)
                        {
                            writer.WriteLine("{0}\t{1}", word.Key, word.Value);
                        }
                        writer.Flush();
                    }
                }
            }
            catch (Exception)
            { }
            return false;
        }

        protected bool CreateDictionaryEntry(string key, long count, SuggestionStage staging = null)
        {
            if (count <= 0)
            {
                if (this.countThreshold > 0) return false; // no point doing anything if count is zero, as it can't change anything
                count = 0;
            }
            long countPrevious;

            // look first in below threshold words, update count, and allow promotion to correct spelling word if count reaches threshold
            // threshold must be >1 for there to be the possibility of low threshold words
            if (countThreshold > 1 && belowThresholdWords.TryGetValue(key, out countPrevious))
            {
                // calculate new count for below threshold word
                count = (long.MaxValue - countPrevious > count) ? countPrevious + count : long.MaxValue;
                // has reached threshold - remove from below threshold collection (it will be added to correct words below)
                if (count >= countThreshold)
                {
                    belowThresholdWords.Remove(key);
                }
                else
                {
                    belowThresholdWords[key] = count;
                    return false;
                }
            }
            else if (words.TryGetValue(key, out countPrevious))
            {
                // just update count if it's an already added above threshold word
                count = (long.MaxValue - countPrevious > count) ? countPrevious + count : long.MaxValue;
                words[key] = count;
                return false;
            }
            else if (count < countThreshold)
            {
                // new or existing below threshold word
                belowThresholdWords[key] = count;
                return false;
            }

            // what we have at this point is a new, above threshold word 
            words.Add(key, count);

            //edits/suggestions are created only once, no matter how often word occurs
            //edits/suggestions are created only as soon as the word occurs in the corpus, 
            //even if the same term existed before in the dictionary as an edit from another word
            if (key.Length > maxDictionaryWordLength) maxDictionaryWordLength = key.Length;

            if (deletes == null)
            {
                this.deletes = new Dictionary<int, string[]>(initialCapacity); //initialisierung
            }

            //create deletes
            var edits = EditsPrefix(key);
            // if not staging suggestions, put directly into main data structure
            if (staging != null)
            {
                foreach (string delete in edits)
                {
                    staging.Add(GetStringHash(delete), key);
                }
            }
            else
            {
                foreach (string delete in edits)
                {
                    int deleteHash = GetStringHash(delete);
                    if (deletes.TryGetValue(deleteHash, out string[] suggestions))
                    {
                        var newSuggestions = new string[suggestions.Length + 1];
                        Array.Copy(suggestions, newSuggestions, suggestions.Length);
                        deletes[deleteHash] = suggestions = newSuggestions;
                    }
                    else
                    {
                        suggestions = new string[1];
                        deletes.Add(deleteHash, suggestions);
                    }
                    suggestions[suggestions.Length - 1] = key;
                }
            }
            return true;
        }

        protected HashSet<string> EditsPrefix(string key)
        {
            HashSet<string> hashSet = new HashSet<string>();
            if (key.Length <= maxDictionaryEditDistance) hashSet.Add("");
            if (key.Length > prefixLength) key = key.Substring(0, prefixLength);
            hashSet.Add(key);
            return Edits(key, 0, hashSet);
        }

        //inexpensive and language independent: only deletes, no transposes + replaces + inserts
        //replaces and inserts are expensive and language dependent (Chinese has 70,000 Unicode Han characters)
        protected HashSet<string> Edits(string word, int editDistance, HashSet<string> deleteWords)
        {
            editDistance++;
            if (word.Length > 1)
            {
                for (int i = 0; i < word.Length; i++)
                {
                    string delete = word.Remove(i, 1);
                    if (deleteWords.Add(delete))
                    {
                        //recursion, if maximum edit distance not yet reached
                        if (editDistance < maxDictionaryEditDistance) Edits(delete, editDistance, deleteWords);
                    }
                }
            }
            return deleteWords;
        }

        protected int GetStringHash(string s)
        {
            int len = s.Length;
            int lenMask = len;
            if (lenMask > 3) lenMask = 3;

            uint hash = 2166136261;
            for (var i = 0; i < len; i++)
            {
                unchecked
                {
                    hash ^= s[i];
                    hash *= 16777619;
                }
            }

            hash &= this.compactMask;
            hash |= (uint)lenMask;
            return (int)hash;
        }

        public void Dispose()
        {
            this.words.Clear();
            this.belowThresholdWords.Clear();
            
            this.deletes.Clear();
            this.deletes = null;
        }
        #endregion

        #region Nested Types
        /// <summary>
        /// A growable list of elements that's optimized to support adds, but not deletes,
        /// of large numbers of elements, storing data in a way that's friendly to the garbage
        /// collector (not backed by a monolithic array object), and can grow without needing
        /// to copy the entire backing array contents from the old backing array to the new.
        /// </summary>
        protected class ChunkArray<T>
        {
            #region Variables
            private const int ChunkSize = 4096; //this must be a power of 2, otherwise can't optimize Row and Col functions
            private const int DivShift = 12; // number of bits to shift right to do division by ChunkSize (the bit position of ChunkSize)
            #endregion

            #region Properties
            public T[][] Values { get; private set; }

            public int Count { get; private set; }

            private int Capacity { get { return Values.Length * ChunkSize; } }

            public T this[int index]
            {
                get { return Values[Row(index)][Col(index)]; }
                set { Values[Row(index)][Col(index)] = value; }
            }
            #endregion

            #region Constructor
            public ChunkArray(int initialCapacity)
            {
                int chunks = (initialCapacity + ChunkSize - 1) / ChunkSize;
                Values = new T[chunks][];
                for (int i = 0; i < Values.Length; i++) Values[i] = new T[ChunkSize];
            }
            #endregion

            #region Methods
            public int Add(T value)
            {
                if (Count == Capacity)
                {
                    var newValues = new T[Values.Length + 1][];
                    // only need to copy the list of array blocks, not the data in the blocks
                    Array.Copy(Values, newValues, Values.Length);
                    newValues[Values.Length] = new T[ChunkSize];
                    Values = newValues;
                }
                Values[Row(Count)][Col(Count)] = value;
                Count++;
                return Count - 1;
            }

            public void Clear()
            {
                Count = 0;
            }

            private int Row(int index)
            {
                return index >> DivShift; // same as index / ChunkSize
            }

            private int Col(int index)
            {
                return index & (ChunkSize - 1); //same as index % ChunkSize
            }
            #endregion
        }

        /// <summary>
        /// An intentionally opacque class used to temporarily stage
        /// dictionary data during the adding of many words. By staging the
        /// data during the building of the dictionary data, significant savings
        /// of time can be achieved, as well as a reduction in final memory usage.
        /// </summary>
        protected class SuggestionStage
        {
            #region Properties
            private Dictionary<int, Entry> Deletes { get; set; }
            private ChunkArray<Node> Nodes { get; set; }

            /// <summary>Gets the count of unique delete words.</summary>
            public int DeleteCount { get { return Deletes.Count; } }

            /// <summary>Gets the total count of all suggestions for all deletes.</summary>
            public int NodeCount { get { return Nodes.Count; } }
            #endregion

            #region Constructors
            /// <summary>Create a new instance of SuggestionStage.</summary>
            /// <remarks>Specifying ann accurate initialCapacity is not essential, 
            /// but it can help speed up processing by aleviating the need for 
            /// data restructuring as the size grows.</remarks>
            /// <param name="initialCapacity">The expected number of words that will be added.</param>
            public SuggestionStage(int initialCapacity)
            {
                Deletes = new Dictionary<int, Entry>(initialCapacity);
                Nodes = new ChunkArray<Node>(initialCapacity * 2);
            }
            #endregion

            #region Methods
            /// <summary>Clears all the data from the SuggestionStaging.</summary>
            public void Clear()
            {
                Deletes.Clear();
                Nodes.Clear();
            }

            internal void Add(int deleteHash, string suggestion)
            {
                if (!Deletes.TryGetValue(deleteHash, out Entry entry)) entry = new Entry { count = 0, first = -1 };
                int next = entry.first;
                entry.count++;
                entry.first = Nodes.Count;
                Deletes[deleteHash] = entry;
                Nodes.Add(new Node { suggestion = suggestion, next = next });
            }

            internal void CommitTo(Dictionary<int, string[]> permanentDeletes)
            {
                foreach (var keyPair in Deletes)
                {
                    int i;
                    if (permanentDeletes.TryGetValue(keyPair.Key, out string[] suggestions))
                    {
                        i = suggestions.Length;
                        var newSuggestions = new string[suggestions.Length + keyPair.Value.count];
                        Array.Copy(suggestions, newSuggestions, suggestions.Length);
                        permanentDeletes[keyPair.Key] = suggestions = newSuggestions;
                    }
                    else
                    {
                        i = 0;
                        suggestions = new string[keyPair.Value.count];
                        permanentDeletes.Add(keyPair.Key, suggestions);
                    }
                    int next = keyPair.Value.first;
                    while (next >= 0)
                    {
                        var node = Nodes[next];
                        suggestions[i] = node.suggestion;
                        next = node.next;
                        i++;
                    }
                }
            }
            #endregion

            #region Nested Types
            private struct Node
            {
                public string suggestion;
                public int next;
            }

            private struct Entry
            {
                public int count;
                public int first;
            }
            #endregion
        }
        #endregion
    }
}