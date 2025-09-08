using System;
using System.Collections.Generic;

namespace SubtitleTools
{
    internal class TagTokenizer
    {
        #region Variables
        private readonly string _input;

        private char _prevToken;
        private int _currentIndex;
        private TokenResult _currentToken;
        #endregion

        #region Contructors
        public TagTokenizer(string input)
        {
            _input = input;
            _currentIndex = 0;
            _prevToken = '\0';
            _currentToken = null;
        }
        #endregion

        #region Properties
        public TokenResult Current
        {
            get => _currentToken;
        }
        #endregion

        #region Methods
        public bool NextToken()
        {
            if (string.IsNullOrEmpty(_input))
            {
                _currentToken = null;
                return false;
            }
            if (_currentIndex >= _input.Length)
            {
                _currentToken = null;
                return false;
            }

            char token = '>';
            if (_prevToken == '\0' || _prevToken == '>')
            {
                token = '<';
            }

            var result = FindNextToken(_input, _currentIndex, _prevToken, token);
            if (result.EndIndex > -1)
            {
                if (result.StartToken == '<' && result.EndToken == '>' && !result.IsComment)
                {
                    result.IsCloseTag = ParseHtmlTag(_input, result.StartIndex - 1, result.Length, out string name, out Dictionary<string, string> attributes);
                    result.TagName = name;
                    result.Attributes = attributes;
                }

                _currentToken = result;
                _currentIndex = result.EndIndex + 1;
                _prevToken = token;
                return true;
            }

            _currentToken = null;
            return false;
        }

        private TokenResult FindNextToken(string text, int start, char prevToken, char nextToken)
        {
            TokenResult result = new TokenResult()
            {
                StartIndex = start,
                StartToken = prevToken,
                EndIndex = -1,
                EndToken = nextToken
            };

            int index = start;
            while (index < text.Length)
            {
                if (text[index] == '\"')
                {
                    int idx = FindStringLiteral(text, index);
                    index = idx;
                }
                else if (text[index] == nextToken)
                {
                    var len = index - start;
                    string innerText = len > 0 ? text.Substring(start, len) : string.Empty;
                    result.InnerText = innerText.Trim();
                    result.EndIndex = index;
                    break;
                }
                index++;
            }

            if (index == text.Length)
            {
                var len = index - start;
                string innerText = len > 0 ? text.Substring(start, len) : string.Empty;
                result.InnerText = innerText.Trim();
                result.EndIndex = index;
                result.EndToken = '\0';
            }

            return result;
        }

        private int FindStringLiteral(string text, int start)
        {
            if (text[start] == '\"')
            {
                bool slash = false;
                for (int i = start + 1; i < text.Length; i++)
                {
                    if (text[i] == '\\')
                    {
                        slash = true;
                        continue;
                    }

                    if (!slash && text[i] == '\"')
                    {
                        return i;
                    }
                    slash = false;
                }
            }
            return start;
        }

        private bool ParseHtmlTag(string source, int idx, int length, out string name, out Dictionary<string, string> attributes)
        {
            idx++;
            length = length - (source[idx + length - 3] == '/' ? 3 : 2);

            // Check if is end tag
            var isClosing = false;
            if (source[idx] == '/')
            {
                idx++;
                length--;
                isClosing = true;
            }

            int spaceIdx = idx;
            while (spaceIdx < idx + length && !char.IsWhiteSpace(source, spaceIdx))
                spaceIdx++;

            // Get the name of the tag
            name = source.Substring(idx, spaceIdx - idx).ToLower();

            attributes = null;
            if (!isClosing && idx + length > spaceIdx)
            {
                ExtractAttributes(source, spaceIdx, length - (spaceIdx - idx), out attributes);
            }

            return isClosing;
        }

        private void ExtractAttributes(string source, int idx, int length, out Dictionary<string, string> attributes)
        {
            attributes = null;

            int startIdx = idx;
            while (startIdx < idx + length)
            {
                while (startIdx < idx + length && char.IsWhiteSpace(source, startIdx))
                    startIdx++;

                var endIdx = startIdx + 1;
                while (endIdx < idx + length && !char.IsWhiteSpace(source, endIdx) && source[endIdx] != '=')
                    endIdx++;

                if (startIdx < idx + length)
                {
                    var key = source.Substring(startIdx, endIdx - startIdx);

                    startIdx = endIdx + 1;
                    while (startIdx < idx + length && (char.IsWhiteSpace(source, startIdx) || source[startIdx] == '='))
                        startIdx++;

                    bool hasPChar = false;
                    char pChar = source[startIdx];
                    if (pChar == '"' || pChar == '\'')
                    {
                        hasPChar = true;
                        startIdx++;
                    }

                    endIdx = startIdx + (hasPChar ? 0 : 1);
                    while (endIdx < idx + length && (hasPChar ? source[endIdx] != pChar : !char.IsWhiteSpace(source, endIdx)))
                        endIdx++;

                    var value = source.Substring(startIdx, endIdx - startIdx);
                    value = HtmlUtils.DecodeHtml(value);

                    if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                    {
                        if (attributes == null)
                            attributes = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
                        attributes[key.ToLower()] = value;
                    }

                    startIdx = endIdx + (hasPChar ? 2 : 1);
                }
            }
        }
        #endregion

        #region Nested Types
        public class TokenResult
        {
            #region Properties
            public string InnerText { get; set; } = string.Empty;

            public int StartIndex { get; set; }

            public int EndIndex { get; set; }

            public char StartToken { get; set; }

            public char EndToken { get; set; }

            public int Length 
            {
                get => 2 + (EndIndex - StartIndex);
            }

            public bool IsEmpty 
            {
                get => string.IsNullOrEmpty(InnerText);
            }

            public bool IsComment 
            { 
                get
                {
                    return StartToken == '<' && EndToken == '>' && Length > 5 && 
                        !string.IsNullOrEmpty(InnerText) && InnerText.StartsWith("!--") && InnerText.EndsWith("--");
                }
            }

            public bool IsTag
            {
                get
                {
                    if (IsEmpty || IsComment || string.IsNullOrEmpty(TagName)) return false;
                    return StartToken == '<' && EndToken == '>';
                }
            }

            public string TagName { get; set; } = string.Empty;

            public Dictionary<string, string> Attributes { get; set; }

            public bool IsCloseTag { get; set; }
            #endregion

            #region Methods
            public override string ToString()
            {
                string text = string.IsNullOrEmpty(InnerText) ? string.Empty : InnerText;
                if (StartToken == '\0' || EndToken == '\0' || (StartToken == '>' && EndToken == '<'))
                {
                    return text;
                }
                else if (IsTag)
                {
                    if (IsCloseTag)
                    {
                        text = "/" + TagName;
                    }
                    else
                    {
                        text = TagName;
                        if (Attributes != null)
                        {
                            foreach (var kv in Attributes)
                            {
                                text += " " + kv.Key.ToLower() + "=\"" + kv.Value + "\"";
                            }
                        }
                    }
                }
                return $"{StartToken}{text}{EndToken}";
            }
            #endregion
        }
        #endregion
    }
}
