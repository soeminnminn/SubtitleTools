// SymSpell: 1 million times faster through Symmetric Delete spelling correction algorithm
//
// The Symmetric Delete spelling correction algorithm reduces the complexity of edit candidate generation and dictionary lookup
// for a given Damerau-Levenshtein distance. It is six orders of magnitude faster and language independent.
// Opposite to other algorithms only deletes are required, no transposes + replaces + inserts.
// Transposes + replaces + inserts of the input term are transformed into deletes of the dictionary term.
// Replaces and inserts are expensive and language dependent: e.g. Chinese has 70,000 Unicode Han characters!
//
// SymSpell supports compound splitting / decompounding of multi-word input strings with three cases:
// 1. mistakenly inserted space into a correct word led to two incorrect terms 
// 2. mistakenly omitted space between two correct words led to one incorrect combined term
// 3. multiple independent input terms with/without spelling errors

// Copyright (C) 2022 Wolf Garbe
// Version: 6.7.2
// Author: Wolf Garbe wolf.garbe@seekstorm.com
// Maintainer: Wolf Garbe wolf.garbe@seekstorm.com
// URL: https://github.com/wolfgarbe/symspell
// Description: https://seekstorm.com/blog/1000x-spelling-correction/
//
// MIT License
// Copyright (c) 2022 Wolf Garbe
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated 
// documentation files (the "Software"), to deal in the Software without restriction, including without limitation 
// the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, 
// and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// https://opensource.org/licenses/MIT

using System;
using System.Text;
using System.Text.RegularExpressions;

namespace SpellChecker
{
    public class SymSpell : SpellDictionary
    {
        #region Variables
        protected readonly EditDistance.DistanceAlgorithm distanceAlgorithm = EditDistance.DistanceAlgorithm.DamerauOSA;

        //######

        //WordSegmentation divides a string into words by inserting missing spaces at the appropriate positions
        //misspelled words are corrected and do not affect segmentation
        //existing spaces are allowed and considered for optimum segmentation

        //SymSpell.WordSegmentation uses a novel approach *without* recursion.
        //https://seekstorm.com/blog/fast-word-segmentation-noisy-text/
        //While each string of length n can be segmentend in 2^n−1 possible compositions https://en.wikipedia.org/wiki/Composition_(combinatorics)
        //SymSpell.WordSegmentation has a linear runtime O(n) to find the optimum composition

        //number of all words in the corpus used to generate the frequency dictionary
        //this is used to calculate the word occurrence probability p from word counts c : p=c/N
        //N equals the sum of all counts c in the dictionary only if the dictionary is complete, but not if the dictionary is truncated or filtered
        public static long N = 1024908267229L;
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
        public SymSpell(int initialCapacity = defaultInitialCapacity, int maxDictionaryEditDistance = defaultMaxEditDistance
            , int prefixLength = defaultPrefixLength, int countThreshold = defaultCountThreshold
            , byte compactLevel = defaultCompactLevel)
            : base(initialCapacity, maxDictionaryEditDistance, prefixLength, countThreshold, compactLevel)
        { }
        #endregion

        #region Methods
        //check whether all delete chars are present in the suggestion prefix in correct order, otherwise this is just a hash collision
        private bool DeleteInSuggestionPrefix(string delete, int deleteLen, string suggestion, int suggestionLen)
        {
            if (deleteLen == 0) return true;
            if (prefixLength < suggestionLen) suggestionLen = prefixLength;
            int j = 0;
            for (int i = 0; i < deleteLen; i++)
            {
                char delChar = delete[i];
                while (j < suggestionLen && delChar != suggestion[j]) j++;
                if (j == suggestionLen) return false;
            }
            return true;
        }

        #region Lookup Methods
        /// <summary>Find suggested spellings for a given input word, using the maximum
        /// edit distance specified during construction of the SymSpell dictionary.</summary>
        /// <param name="input">The word being spell checked.</param>
        /// <param name="verbosity">The value controlling the quantity/closeness of the retuned suggestions.</param>
        /// <returns>A List of SuggestItem object representing suggested correct spellings for the input word, 
        /// sorted by edit distance, and secondarily by count frequency.</returns>
        public List<SuggestItem> Lookup(string input, Verbosity verbosity)
        {
            return Lookup(input, verbosity, this.maxDictionaryEditDistance, false);
        }

        /// <summary>Find suggested spellings for a given input word, using the maximum
        /// edit distance specified during construction of the SymSpell dictionary.</summary>
        /// <param name="input">The word being spell checked.</param>
        /// <param name="verbosity">The value controlling the quantity/closeness of the retuned suggestions.</param>
        /// <param name="maxEditDistance">The maximum edit distance between input and suggested words.</param>
        /// <returns>A List of SuggestItem object representing suggested correct spellings for the input word, 
        /// sorted by edit distance, and secondarily by count frequency.</returns>
        public List<SuggestItem> Lookup(string input, Verbosity verbosity, int maxEditDistance)
        {
            return Lookup(input, verbosity, maxEditDistance, false);
        }

        /// <summary>Find suggested spellings for a given input word.</summary>
        /// <param name="input">The word being spell checked.</param>
        /// <param name="verbosity">The value controlling the quantity/closeness of the retuned suggestions.</param>
        /// <param name="maxEditDistance">The maximum edit distance between input and suggested words.</param>
        /// <param name="includeUnknown">Include input word in suggestions, if no words within edit distance found.</param>																													   
        /// <returns>A List of SuggestItem object representing suggested correct spellings for the input word, 
        /// sorted by edit distance, and secondarily by count frequency.</returns>
        public List<SuggestItem> Lookup(string input, Verbosity verbosity, int maxEditDistance, bool includeUnknown)
        {
            //verbosity=Top: the suggestion with the highest term frequency of the suggestions of smallest edit distance found
            //verbosity=Closest: all suggestions of smallest edit distance found, the suggestions are ordered by term frequency 
            //verbosity=All: all suggestions <= maxEditDistance, the suggestions are ordered by edit distance, then by term frequency (slower, no early termination)

            // maxEditDistance used in Lookup can't be bigger than the maxDictionaryEditDistance
            // used to construct the underlying dictionary structure.
            if (maxEditDistance > MaxDictionaryEditDistance) throw new ArgumentOutOfRangeException(nameof(maxEditDistance));

            List<SuggestItem> suggestions = new List<SuggestItem>();
            int inputLen = input.Length;
            // early exit - word is too big to possibly match any words
            if (inputLen - maxEditDistance > maxDictionaryWordLength) goto end;

            // quick look for exact match
            long suggestionCount = 0;
            if (words.TryGetValue(input, out suggestionCount))
            {
                suggestions.Add(new SuggestItem(input, 0, suggestionCount));
                // early exit - return exact match, unless caller wants all matches
                if (verbosity != Verbosity.All) goto end;
            }

            //early termination, if we only want to check if word in dictionary or get its frequency e.g. for word segmentation
            if (maxEditDistance == 0) goto end;

            // deletes we've considered already
            HashSet<string> hashset1 = new HashSet<string>();
            // suggestions we've considered already
            HashSet<string> hashset2 = new HashSet<string>();
            // we considered the input already in the word.TryGetValue above		
            hashset2.Add(input);

            int maxEditDistance2 = maxEditDistance;
            int candidatePointer = 0;
            var singleSuggestion = new string[1] { string.Empty };
            List<string> candidates = new List<string>();

            //add original prefix
            int inputPrefixLen = inputLen;
            if (inputPrefixLen > prefixLength)
            {
                inputPrefixLen = prefixLength;
                candidates.Add(input.Substring(0, inputPrefixLen));
            }
            else
            {
                candidates.Add(input);
            }

            var distanceComparer = new EditDistance(this.distanceAlgorithm);
            while (candidatePointer < candidates.Count)
            {
                string candidate = candidates[candidatePointer++];
                int candidateLen = candidate.Length;
                int lengthDiff = inputPrefixLen - candidateLen;

                //save some time - early termination
                //if canddate distance is already higher than suggestion distance, than there are no better suggestions to be expected
                if (lengthDiff > maxEditDistance2)
                {
                    // skip to next candidate if Verbosity.All, look no further if Verbosity.Top or Closest 
                    // (candidates are ordered by delete distance, so none are closer than current)
                    if (verbosity == Verbosity.All) continue;
                    break;
                }

                //read candidate entry from dictionary
                if (deletes.TryGetValue(GetStringHash(candidate), out string[] dictSuggestions))
                {
                    //iterate through suggestions (to other correct dictionary items) of delete item and add them to suggestion list
                    for (int i = 0; i < dictSuggestions.Length; i++)
                    {
                        var suggestion = dictSuggestions[i];
                        int suggestionLen = suggestion.Length;
                        if (suggestion == input) continue;
                        if ((Math.Abs(suggestionLen - inputLen) > maxEditDistance2) // input and sugg lengths diff > allowed/current best distance
                            || (suggestionLen < candidateLen) // sugg must be for a different delete string, in same bin only because of hash collision
                            || (suggestionLen == candidateLen && suggestion != candidate)) // if sugg len = delete len, then it either equals delete or is in same bin only because of hash collision
                            continue;
                        var suggPrefixLen = Math.Min(suggestionLen, prefixLength);
                        if (suggPrefixLen > inputPrefixLen && (suggPrefixLen - candidateLen) > maxEditDistance2) continue;

                        //True Damerau-Levenshtein Edit Distance: adjust distance, if both distances>0
                        //We allow simultaneous edits (deletes) of maxEditDistance on on both the dictionary and the input term. 
                        //For replaces and adjacent transposes the resulting edit distance stays <= maxEditDistance.
                        //For inserts and deletes the resulting edit distance might exceed maxEditDistance.
                        //To prevent suggestions of a higher edit distance, we need to calculate the resulting edit distance, if there are simultaneous edits on both sides.
                        //Example: (bank==bnak and bank==bink, but bank!=kanb and bank!=xban and bank!=baxn for maxEditDistance=1)
                        //Two deletes on each side of a pair makes them all equal, but the first two pairs have edit distance=1, the others edit distance=2.
                        int distance = 0;
                        int min = 0;
                        if (candidateLen == 0)
                        {
                            //suggestions which have no common chars with input (inputLen<=maxEditDistance && suggestionLen<=maxEditDistance)
                            distance = Math.Max(inputLen, suggestionLen);
                            if (distance > maxEditDistance2 || !hashset2.Add(suggestion)) continue;
                        }
                        else if (suggestionLen == 1)
                        {
                            if (input.IndexOf(suggestion[0]) < 0) distance = inputLen; else distance = inputLen - 1;
                            if (distance > maxEditDistance2 || !hashset2.Add(suggestion)) continue;
                        }
                        else
                        //number of edits in prefix ==maxediddistance  AND no identic suffix
                        //, then editdistance>maxEditDistance and no need for Levenshtein calculation  
                        //      (inputLen >= prefixLength) && (suggestionLen >= prefixLength) 
                        if ((prefixLength - maxEditDistance == candidateLen)
                            && (((min = Math.Min(inputLen, suggestionLen) - prefixLength) > 1)
                                && (input.Substring(inputLen + 1 - min) != suggestion.Substring(suggestionLen + 1 - min)))
                               || ((min > 0) && (input[inputLen - min] != suggestion[suggestionLen - min])
                                   && ((input[inputLen - min - 1] != suggestion[suggestionLen - min])
                                       || (input[inputLen - min] != suggestion[suggestionLen - min - 1]))))
                        {
                            continue;
                        }
                        else
                        {
                            // DeleteInSuggestionPrefix is somewhat expensive, and only pays off when verbosity is Top or Closest.
                            if ((verbosity != Verbosity.All && !DeleteInSuggestionPrefix(candidate, candidateLen, suggestion, suggestionLen))
                                || !hashset2.Add(suggestion)) continue;
                            distance = distanceComparer.Compare(input, suggestion, maxEditDistance2);
                            if (distance < 0) continue;
                        }

                        //save some time
                        //do not process higher distances than those already found, if verbosity<All (note: maxEditDistance2 will always equal maxEditDistance when Verbosity.All)
                        if (distance <= maxEditDistance2)
                        {
                            suggestionCount = words[suggestion];
                            SuggestItem si = new SuggestItem(suggestion, distance, suggestionCount);
                            if (suggestions.Count > 0)
                            {
                                switch (verbosity)
                                {
                                    case Verbosity.Closest:
                                        {
                                            //we will calculate DamLev distance only to the smallest found distance so far
                                            if (distance < maxEditDistance2) suggestions.Clear();
                                            break;
                                        }
                                    case Verbosity.Top:
                                        {
                                            if (distance < maxEditDistance2 || suggestionCount > suggestions[0].count)
                                            {
                                                maxEditDistance2 = distance;
                                                suggestions[0] = si;
                                            }
                                            continue;
                                        }
                                }
                            }
                            if (verbosity != Verbosity.All) maxEditDistance2 = distance;
                            suggestions.Add(si);
                        }
                    }//end foreach
                }//end if         

                //add edits 
                //derive edits (deletes) from candidate (input) and add them to candidates list
                //this is a recursive process until the maximum edit distance has been reached
                if ((lengthDiff < maxEditDistance) && (candidateLen <= prefixLength))
                {
                    //save some time
                    //do not create edits with edit distance smaller than suggestions already found
                    if (verbosity != Verbosity.All && lengthDiff >= maxEditDistance2) continue;

                    for (int i = 0; i < candidateLen; i++)
                    {
                        string delete = candidate.Remove(i, 1);

                        if (hashset1.Add(delete)) { candidates.Add(delete); }
                    }
                }
            }

            //sort by ascending edit distance, then by descending word frequency
            if (suggestions.Count > 1) suggestions.Sort();
            end: if (includeUnknown && (suggestions.Count == 0)) suggestions.Add(new SuggestItem(input, maxEditDistance + 1, 0));
            return suggestions;
        }
        #endregion

        #region WordSegmentation Methods
        /// <summary>Find suggested spellings for a multi-word input string (supports word splitting/merging).</summary>
        /// <param name="input">The string being spell checked.</param>
        /// <returns>The word segmented string, 
        /// the word segmented and spelling corrected string, 
        /// the Edit distance sum between input string and corrected string, 
        /// the Sum of word occurence probabilities in log scale (a measure of how common and probable the corrected segmentation is).</returns> 
        public Segmentation WordSegmentation(string input)
        {
            return WordSegmentation(input, this.MaxDictionaryEditDistance, this.maxDictionaryWordLength);
        }

        /// <summary>Find suggested spellings for a multi-word input string (supports word splitting/merging).</summary>
        /// <param name="input">The string being spell checked.</param>
        /// <param name="maxEditDistance">The maximum edit distance between input and corrected words 
        /// (0=no correction/segmentation only).</param>	
        /// <returns>The word segmented string, 
        /// the word segmented and spelling corrected string, 
        /// the Edit distance sum between input string and corrected string, 
        /// the Sum of word occurence probabilities in log scale (a measure of how common and probable the corrected segmentation is).</returns> 
        public Segmentation WordSegmentation(string input, int maxEditDistance)
        {
            return WordSegmentation(input, maxEditDistance, this.maxDictionaryWordLength);
        }

        /// <summary>Find suggested spellings for a multi-word input string (supports word splitting/merging).</summary>
        /// <param name="input">The string being spell checked.</param>
        /// <param name="maxSegmentationWordLength">The maximum word length that should be considered.</param>	
        /// <param name="maxEditDistance">The maximum edit distance between input and corrected words 
        /// (0=no correction/segmentation only).</param>	
        /// <returns>The word segmented string, 
        /// the word segmented and spelling corrected string, 
        /// the Edit distance sum between input string and corrected string, 
        /// the Sum of word occurence probabilities in log scale (a measure of how common and probable the corrected segmentation is).</returns> 
        public Segmentation WordSegmentation(string input, int maxEditDistance, int maxSegmentationWordLength)
        {
            //v6.7
            //normalize ligatures: 
            //"scientific"
            //"scientiﬁc" "ﬁelds" "ﬁnal"
            input = input.Normalize(NormalizationForm.FormKC).Replace("\u002D", "");//.Replace("\uC2AD","");

            int arraySize = Math.Min(maxSegmentationWordLength, input.Length);
            Segmentation[] compositions = new Segmentation[arraySize];
            int circularIndex = -1;

            //outer loop (column): all possible part start positions
            for (int j = 0; j < input.Length; j++)
            {
                //inner loop (row): all possible part lengths (from start position): part can't be bigger than longest word in dictionary (other than long unknown word)
                int imax = Math.Min(input.Length - j, maxSegmentationWordLength);
                for (int i = 1; i <= imax; i++)
                {
                    //get top spelling correction/ed for part
                    string part = input.Substring(j, i);
                    int separatorLength = 0;
                    int topEd = 0;
                    decimal topProbabilityLog = 0;
                    string topResult = "";

                    if (char.IsWhiteSpace(part[0]))
                    {
                        //remove space for levensthein calculation
                        part = part.Substring(1);
                    }
                    else
                    {
                        //add ed+1: space did not exist, had to be inserted
                        separatorLength = 1;
                    }

                    //remove space from part1, add number of removed spaces to topEd                
                    topEd += part.Length;
                    //remove space
                    part = part.Replace(" ", ""); //=System.Text.RegularExpressions.Regex.Replace(part1, @"\s+", "");
                                                  //add number of removed spaces to ed
                    topEd -= part.Length;

                    //v6.7
                    //Lookup against the lowercase term
                    List<SuggestItem> results = this.Lookup(part.ToLower(), Verbosity.Top, maxEditDistance);
                    if (results.Count > 0)
                    {
                        topResult = results[0].term;
                        //v6.7
                        //retain/preserve upper case 
                        if ((part.Length > 0) && char.IsUpper(part[0]))
                        {
                            char[] a = topResult.ToCharArray();
                            a[0] = char.ToUpper(topResult[0]);
                            topResult = new string(a);
                        }

                        topEd += results[0].distance;
                        //Naive Bayes Rule
                        //we assume the word probabilities of two words to be independent
                        //therefore the resulting probability of the word combination is the product of the two word probabilities

                        //instead of computing the product of probabilities we are computing the sum of the logarithm of probabilities
                        //because the probabilities of words are about 10^-10, the product of many such small numbers could exceed (underflow) the floating number range and become zero
                        //log(ab)=log(a)+log(b)
                        topProbabilityLog = (decimal)Math.Log10((double)results[0].count / (double)N);
                    }
                    else
                    {
                        topResult = part;
                        //default, if word not found
                        //otherwise long input text would win as long unknown word (with ed=edmax+1 ), although there there should many spaces inserted 
                        topEd += part.Length;
                        topProbabilityLog = (decimal)Math.Log10(10.0 / (N * Math.Pow(10.0, part.Length)));
                    }

                    int destinationIndex = ((i + circularIndex) % arraySize);

                    //set values in first loop
                    if (j == 0)
                    {
                        compositions[destinationIndex] = new Segmentation(part, topResult, topEd, topProbabilityLog);
                    }
                    else if ((i == maxSegmentationWordLength)
                        //replace values if better probabilityLogSum, if same edit distance OR one space difference 
                        || (((compositions[circularIndex].distanceSum + topEd == compositions[destinationIndex].distanceSum)
                        || (compositions[circularIndex].distanceSum + separatorLength + topEd == compositions[destinationIndex].distanceSum))
                        && (compositions[destinationIndex].probabilityLogSum < compositions[circularIndex].probabilityLogSum + topProbabilityLog))
                        //replace values if smaller edit distance     
                        || (compositions[circularIndex].distanceSum + separatorLength + topEd < compositions[destinationIndex].distanceSum))
                    {
                        //v6.7
                        //keep punctuation or spostrophe adjacent to previous word
                        if (((topResult.Length == 1) && char.IsPunctuation(topResult[0])) || ((topResult.Length == 2) && topResult.StartsWith("’")))
                        {
                            compositions[destinationIndex] = new Segmentation(
                                compositions[circularIndex].segmentedString + part,
                                compositions[circularIndex].correctedString + topResult,
                                compositions[circularIndex].distanceSum + topEd,
                                compositions[circularIndex].probabilityLogSum + topProbabilityLog
                            );
                        }
                        else
                        {
                            compositions[destinationIndex] = new Segmentation(
                                compositions[circularIndex].segmentedString + " " + part,
                                compositions[circularIndex].correctedString + " " + topResult,
                                compositions[circularIndex].distanceSum + separatorLength + topEd,
                                compositions[circularIndex].probabilityLogSum + topProbabilityLog
                            );
                        }
                    }
                }
                circularIndex++;
                if (circularIndex == arraySize) circularIndex = 0;
            }
            return compositions[circularIndex];
        }
        #endregion

        #endregion

        #region Nested Types
        /// <summary>Controls the closeness/quantity of returned spelling suggestions.</summary>
        public enum Verbosity
        {
            /// <summary>Top suggestion with the highest term frequency of the suggestions of smallest edit distance found.</summary>
            Top,
            /// <summary>All suggestions of smallest edit distance found, suggestions ordered by term frequency.</summary>
            Closest,
            /// <summary>All suggestions within maxEditDistance, suggestions ordered by edit distance
            /// , then by term frequency (slower, no early termination).</summary>
            All
        };

        /// <summary>Spelling suggestion returned from Lookup.</summary>
        public class SuggestItem : IComparable<SuggestItem>
        {
            #region Variables
            /// <summary>The suggested correctly spelled word.</summary>
            public string term = "";
            /// <summary>Edit distance between searched for word and suggestion.</summary>
            public int distance = 0;
            /// <summary>Frequency of suggestion in the dictionary (a measure of how common the word is).</summary>
            public long count = 0;
            #endregion

            #region Constructors
            /// <summary>Create a new instance of SuggestItem.</summary>
            /// <param name="term">The suggested word.</param>
            /// <param name="distance">Edit distance from search word.</param>
            /// <param name="count">Frequency of suggestion in dictionary.</param>
            public SuggestItem()
            {
            }

            public SuggestItem(string term, int distance, long count)
            {
                this.term = term;
                this.distance = distance;
                this.count = count;
            }
            #endregion

            #region Methods
            public int CompareTo(SuggestItem other)
            {
                // order by distance ascending, then by frequency count descending
                if (this.distance == other.distance) return other.count.CompareTo(this.count);
                return this.distance.CompareTo(other.distance);
            }

            public override bool Equals(object obj)
            {
                return Equals(term, ((SuggestItem)obj).term);
            }

            public override int GetHashCode()
            {
                return term.GetHashCode();
            }
            public override string ToString()
            {
                return "{" + term + ", " + distance + ", " + count + "}";
            }

            public SuggestItem ShallowCopy()
            {
                return (SuggestItem)MemberwiseClone();
            }
            #endregion
        }

        public struct Segmentation
        {
            #region Variables
            public string segmentedString;
            public string correctedString;
            public int distanceSum;
            public decimal probabilityLogSum;
            #endregion

            #region Constructor
            internal Segmentation(string segmentedString, string correctedString, int distanceSum, decimal probabilityLogSum)
            {
                this.segmentedString = segmentedString;
                this.correctedString = correctedString;
                this.distanceSum = distanceSum;
                this.probabilityLogSum = probabilityLogSum;
            }
            #endregion
        }
        #endregion
    }
}
