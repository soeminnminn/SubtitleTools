using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace SpellChecker
{
    public class SymSpellBigrams : SymSpell
    {
        #region Variables
        public Dictionary<string, long> bigrams = new Dictionary<string, long>();
        public long bigramCountMin = long.MaxValue;
        #endregion

        #region Constructors
        /// <summary>Create a new instanc of SymSpell.</summary>
        /// <remarks>Specifying ann accurate initialCapacity is not essential, 
        /// but it can help speed up processing by aleviating the need for 
        /// data restructuring as the size grows.</remarks>
        /// <param name="initialCapacity">The expected number of words in dictionary.</param>
        /// <param name="maxDictionaryEditDistance">Maximum edit distance for doing lookups.</param>
        /// <param name="prefixLength">The length of word prefixes used for spell checking.</param>
        /// <param name="countThreshold">The minimum frequency count for dictionary words to be considered correct spellings.</param>
        /// <param name="compactLevel">Degree of favoring lower memory use over speed (0=fastest,most memory, 16=slowest,least memory).</param>
        public SymSpellBigrams(int initialCapacity = defaultInitialCapacity, int maxDictionaryEditDistance = defaultMaxEditDistance
            , int prefixLength = defaultPrefixLength, int countThreshold = defaultCountThreshold
            , byte compactLevel = defaultCompactLevel)
            : base(initialCapacity, maxDictionaryEditDistance, prefixLength, countThreshold, compactLevel)
        { }
        #endregion

        #region Methods

        #region LoadBigramDictionary Methods
        /// <summary>Load multiple dictionary entries from a file of word/frequency count pairs.</summary>
        /// <remarks>Merges with any dictionary data already loaded.</remarks>
        /// <param name="filePath">The path+filename of the file.</param>
        /// <returns>True if file loaded, or false if file not found.</returns>
        public bool LoadBigramDictionary(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return false;
            if (!File.Exists(filePath)) return false;

            try
            {
                bool result = false;
                using (var stream = File.OpenRead(filePath))
                {
                    result = LoadBigramDictionary(stream);
                }
                return result;
            }
            catch (Exception)
            { }

            return false;
        }

        /// <summary>Load multiple dictionary entries from a file of word/frequency count pairs.</summary>
        /// <remarks>Merges with any dictionary data already loaded.</remarks>
        /// <param name="stream">The Stream.</param>
        /// <returns>True if file loaded, or false if file not found.</returns>
        public bool LoadBigramDictionary(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException();
            if (!stream.CanRead) throw new IOException();

            words.Clear();

            using (StreamReader sr = new StreamReader(stream, Encoding.UTF8, false))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    string[] parts = line.Split();
                    if (parts.Length > 1)
                    {
                        int countIndex = parts.Length - 1;
                        if (long.TryParse(parts[countIndex], out long count))
                        {
                            parts.SetValue(string.Empty, countIndex);
                            string key = string.Join(' ', parts).Trim();

                            bigrams.Add(key, count);
                            if (count < bigramCountMin) bigramCountMin = count;
                        }
                    }
                }
            }

            return true;
        }
        #endregion

        //create a non-unique wordlist from sample text
        //language independent (e.g. works with Chinese characters)
        private string[] ParseWords(string text)
        {
            // \w Alphanumeric characters (including non-latin characters, umlaut characters and digits) plus "_" 
            // \d Digits
            // Compatible with non-latin characters, does not split words at apostrophes
            MatchCollection mc = Regex.Matches(text.ToLower(), @"['’\w-[_]]+");

            //for benchmarking only: with CreateDictionary("big.txt","") and the text corpus from http://norvig.com/big.txt  the Regex below provides the exact same number of dictionary items as Norvigs regex "[a-z]+" (which splits words at apostrophes & incompatible with non-latin characters)     
            //MatchCollection mc = Regex.Matches(text.ToLower(), @"[\w-[\d_]]+");

            var matches = new string[mc.Count];
            for (int i = 0; i < matches.Length; i++) matches[i] = mc[i].ToString();
            return matches;
        }

        #region LookupCompound Methods
        //######################

        //LookupCompound supports compound aware automatic spelling correction of multi-word input strings with three cases:
        //1. mistakenly inserted space into a correct word led to two incorrect terms 
        //2. mistakenly omitted space between two correct words led to one incorrect combined term
        //3. multiple independent input terms with/without spelling errors

        /// <summary>Find suggested spellings for a multi-word input string (supports word splitting/merging).</summary>
        /// <param name="input">The string being spell checked.</param>																										   
        /// <returns>A List of SuggestItem object representing suggested correct spellings for the input string.</returns> 
        public List<SuggestItem> LookupCompound(string input)
        {
            return LookupCompound(input, this.maxDictionaryEditDistance);
        }

        /// <summary>Find suggested spellings for a multi-word input string (supports word splitting/merging).</summary>
        /// <param name="input">The string being spell checked.</param>
        /// <param name="maxEditDistance">The maximum edit distance between input and suggested words.</param>																											   
        /// <returns>A List of SuggestItem object representing suggested correct spellings for the input string.</returns> 
        public List<SuggestItem> LookupCompound(string input, int editDistanceMax)
        {
            //parse input string into single terms
            string[] termList1 = ParseWords(input);

            List<SuggestItem> suggestions = new List<SuggestItem>();     //suggestions for a single term
            List<SuggestItem> suggestionParts = new List<SuggestItem>(); //1 line with separate parts
            var distanceComparer = new EditDistance(this.distanceAlgorithm);

            //translate every term to its best suggestion, otherwise it remains unchanged
            bool lastCombi = false;
            for (int i = 0; i < termList1.Length; i++)
            {
                suggestions = Lookup(termList1[i], Verbosity.Top, editDistanceMax);

                //combi check, always before split
                if ((i > 0) && !lastCombi)
                {
                    List<SuggestItem> suggestionsCombi = Lookup(termList1[i - 1] + termList1[i], Verbosity.Top, editDistanceMax);

                    if (suggestionsCombi.Count > 0)
                    {
                        SuggestItem best1 = suggestionParts[suggestionParts.Count - 1];
                        SuggestItem best2 = new SuggestItem();
                        if (suggestions.Count > 0)
                        {
                            best2 = suggestions[0];
                        }
                        else
                        {
                            //unknown word
                            best2.term = termList1[i];
                            //estimated edit distance
                            best2.distance = editDistanceMax + 1;
                            //estimated word occurrence probability P=10 / (N * 10^word length l)
                            best2.count = (long)((double)10 / Math.Pow((double)10, (double)best2.term.Length)); // 0;
                        }

                        //distance1=edit distance between 2 split terms und their best corrections : als comparative value for the combination
                        int distance1 = best1.distance + best2.distance;
                        if ((distance1 >= 0) && ((suggestionsCombi[0].distance + 1 < distance1) || ((suggestionsCombi[0].distance + 1 == distance1) && ((double)suggestionsCombi[0].count > (double)best1.count / (double)SymSpell.N * (double)best2.count))))
                        {
                            suggestionsCombi[0].distance++;
                            suggestionParts[suggestionParts.Count - 1] = suggestionsCombi[0];
                            lastCombi = true;
                            goto nextTerm;
                        }
                    }
                }
                lastCombi = false;

                //alway split terms without suggestion / never split terms with suggestion ed=0 / never split single char terms
                if ((suggestions.Count > 0) && ((suggestions[0].distance == 0) || (termList1[i].Length == 1)))
                {
                    //choose best suggestion
                    suggestionParts.Add(suggestions[0]);
                }
                else
                {
                    //if no perfect suggestion, split word into pairs
                    SuggestItem suggestionSplitBest = null;

                    //add original term 
                    if (suggestions.Count > 0) suggestionSplitBest = suggestions[0];

                    if (termList1[i].Length > 1)
                    {
                        for (int j = 1; j < termList1[i].Length; j++)
                        {
                            string part1 = termList1[i].Substring(0, j);
                            string part2 = termList1[i].Substring(j);
                            SuggestItem suggestionSplit = new SuggestItem();
                            List<SuggestItem> suggestions1 = Lookup(part1, Verbosity.Top, editDistanceMax);
                            if (suggestions1.Count > 0)
                            {
                                List<SuggestItem> suggestions2 = Lookup(part2, Verbosity.Top, editDistanceMax);
                                if (suggestions2.Count > 0)
                                {
                                    //select best suggestion for split pair
                                    suggestionSplit.term = suggestions1[0].term + " " + suggestions2[0].term;

                                    int distance2 = distanceComparer.Compare(termList1[i], suggestionSplit.term, editDistanceMax);
                                    if (distance2 < 0) distance2 = editDistanceMax + 1;

                                    if (suggestionSplitBest != null)
                                    {
                                        if (distance2 > suggestionSplitBest.distance) continue;
                                        if (distance2 < suggestionSplitBest.distance) suggestionSplitBest = null;
                                    }

                                    suggestionSplit.distance = distance2;
                                    //if bigram exists in bigram dictionary
                                    if (bigrams.TryGetValue(suggestionSplit.term, out long bigramCount))
                                    {
                                        suggestionSplit.count = bigramCount;

                                        //increase count, if split.corrections are part of or identical to input  
                                        //single term correction exists
                                        if (suggestions.Count > 0)
                                        {
                                            //alternatively remove the single term from suggestionsSplit, but then other splittings could win
                                            if ((suggestions1[0].term + suggestions2[0].term == termList1[i]))
                                            {
                                                //make count bigger than count of single term correction
                                                suggestionSplit.count = Math.Max(suggestionSplit.count, suggestions[0].count + 2);
                                            }
                                            else if ((suggestions1[0].term == suggestions[0].term) || (suggestions2[0].term == suggestions[0].term))
                                            {
                                                //make count bigger than count of single term correction
                                                suggestionSplit.count = Math.Max(suggestionSplit.count, suggestions[0].count + 1);
                                            }
                                        }
                                        //no single term correction exists
                                        else if ((suggestions1[0].term + suggestions2[0].term == termList1[i]))
                                        {
                                            suggestionSplit.count = Math.Max(suggestionSplit.count, Math.Max(suggestions1[0].count, suggestions2[0].count) + 2);
                                        }

                                    }
                                    else
                                    {
                                        //The Naive Bayes probability of the word combination is the product of the two word probabilities: P(AB) = P(A) * P(B)
                                        //use it to estimate the frequency count of the combination, which then is used to rank/select the best splitting variant  
                                        suggestionSplit.count = Math.Min(bigramCountMin, (long)((double)suggestions1[0].count / (double)SymSpell.N * (double)suggestions2[0].count));
                                    }

                                    if ((suggestionSplitBest == null) || (suggestionSplit.count > suggestionSplitBest.count)) suggestionSplitBest = suggestionSplit;
                                }
                            }
                        }

                        if (suggestionSplitBest != null)
                        {
                            //select best suggestion for split pair
                            suggestionParts.Add(suggestionSplitBest);
                        }
                        else
                        {
                            SuggestItem si = new SuggestItem();
                            si.term = termList1[i];
                            //estimated word occurrence probability P=10 / (N * 10^word length l)
                            si.count = (long)((double)10 / Math.Pow((double)10, (double)si.term.Length));
                            si.distance = editDistanceMax + 1;
                            suggestionParts.Add(si);
                        }
                    }
                    else
                    {
                        SuggestItem si = new SuggestItem();
                        si.term = termList1[i];
                        //estimated word occurrence probability P=10 / (N * 10^word length l)
                        si.count = (long)((double)10 / Math.Pow((double)10, (double)si.term.Length));
                        si.distance = editDistanceMax + 1;
                        suggestionParts.Add(si);
                    }
                }
            nextTerm:;
            }

            SuggestItem suggestion = new SuggestItem();

            double count = SymSpell.N;
            StringBuilder s = new StringBuilder();
            foreach (SuggestItem si in suggestionParts) { s.Append(si.term + " "); count *= (double)si.count / (double)SymSpell.N; }
            suggestion.count = (long)count;

            suggestion.term = s.ToString().TrimEnd();
            suggestion.distance = distanceComparer.Compare(input, suggestion.term, int.MaxValue);

            List<SuggestItem> suggestionsLine = new List<SuggestItem>();
            suggestionsLine.Add(suggestion);
            return suggestionsLine;
        }
        #endregion

        #endregion
    }
}
