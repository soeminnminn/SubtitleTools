using System;

namespace SpellChecker
{
    /// <summary>Wrapper for third party edit distance algorithms.</summary>
    public class EditDistance
    {
        #region Variables
        private DistanceAlgorithm algorithm;
        private IDistance distanceComparer;
        #endregion

        #region Constructors
        /// <summary>Create a new EditDistance object.</summary>
        /// <param name="algorithm">The desired edit distance algorithm.</param>
        public EditDistance(DistanceAlgorithm algorithm)
        {
            this.algorithm = algorithm;
            switch (algorithm)
            {
                case DistanceAlgorithm.DamerauOSA: this.distanceComparer = new DamerauOSA(); break;
                case DistanceAlgorithm.Levenshtein: this.distanceComparer = new Levenshtein(); break;
                default: throw new ArgumentException("Unknown distance algorithm.");
            }
        }
        #endregion

        #region Methods
        /// <summary>
        /// Compare a string to the base string to determine the edit distance,
        /// using the previously selected algorithm.
        /// </summary>
        /// <param name="string2">The string to compare.</param>
        /// <param name="maxDistance">The maximum distance allowed.</param>
        /// <returns>The edit distance (or -1 if maxDistance exceeded).</returns>
        public int Compare(string string1, string string2, int maxDistance)
        {
            return (int)this.distanceComparer.Distance(string1, string2, maxDistance);
        }
        #endregion

        #region Nested Types
        /// <summary>Supported edit distance algorithms.</summary>
        public enum DistanceAlgorithm
        {
            /// <summary>Levenshtein algorithm.</summary>
            Levenshtein,
            /// <summary>Damerau optimal string alignment algorithm.</summary>
            DamerauOSA
        }
        #endregion
    }
}
