using System.Reflection;
using System.Runtime.CompilerServices;

namespace SpellChecker
{
    public static class SpellFactory
    {
        private const string SPELL_DICT = "dictionary_en.txt";
        private const string BIGRAMS_DICT = "bigramdictionary_en.txt";

        public static SymSpell CreateSymSpell()
        {
            var assm = typeof(SpellFactory).Assembly;
            
            try
            {
                var stream = assm.GetManifestResourceStream(SPELL_DICT);

                try
                {
                    if (stream != null)
                    {
                        var spell = new SymSpell();
                        spell.LoadDictionary(stream);
                        return spell;
                    }
                }
                finally
                {
                    if (stream != null)
                    {
                        stream.Close();
                        stream.Dispose();
                    }
                }
            }
            catch
            { }

            return null;
        }

        public static SymSpellBigrams CreateSymSpellBigrams()
        {
            var assm = typeof(SpellFactory).Assembly;
            
            try
            {
                var stream = assm.GetManifestResourceStream(SPELL_DICT);
                var streamBigrams = assm.GetManifestResourceStream(BIGRAMS_DICT);

                try
                {
                    if (stream != null)
                    {
                        var spell = new SymSpellBigrams();
                        spell.LoadDictionary(stream);
                        spell.LoadBigramDictionary(streamBigrams);

                        return spell;
                    }
                }
                finally
                {
                    if (stream != null)
                    {
                        stream.Close();
                        stream.Dispose();
                    }

                    if (streamBigrams != null)
                    {
                        streamBigrams.Close();
                        streamBigrams.Dispose();
                    }
                }
            }
            catch
            { }
            return null;
        }
    }
}
