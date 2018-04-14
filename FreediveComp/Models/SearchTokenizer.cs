using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace MilanWilczak.FreediveComp.Models
{
    public class SearchTokenizer
    {
        public HashSet<string> GetTokens(string search)
        {
            var normalized = search.Normalize(NormalizationForm.FormD).ToCharArray();
            var tokens = new HashSet<string>();
            var buffer = new StringBuilder();
            int inputSize = normalized.Length;
            for (int inputIndex = 0; inputIndex <= inputSize; inputIndex++)
            {
                char ch = inputIndex == inputSize ? ' ' : normalized[inputIndex];
                var category = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (IsUsableChar(category))
                {
                    buffer.Append(Char.ToLower(ch));
                }
                else if (IsSeparatingChar(category) && buffer.Length > 0)
                {
                    tokens.Add(buffer.ToString().Normalize(NormalizationForm.FormC));
                    buffer.Clear();
                }
            }
            return tokens;
        }

        private static bool IsSeparatingChar(UnicodeCategory unicodeCategory)
        {
            switch (unicodeCategory)
            {
                case UnicodeCategory.SpaceSeparator:
                case UnicodeCategory.LineSeparator:
                case UnicodeCategory.ParagraphSeparator:
                case UnicodeCategory.OpenPunctuation:
                case UnicodeCategory.ClosePunctuation:
                case UnicodeCategory.InitialQuotePunctuation:
                case UnicodeCategory.FinalQuotePunctuation:
                case UnicodeCategory.DashPunctuation:
                case UnicodeCategory.OtherPunctuation:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsUsableChar(UnicodeCategory unicodeCategory)
        {
            switch (unicodeCategory)
            {
                case UnicodeCategory.UppercaseLetter:
                case UnicodeCategory.LowercaseLetter:
                case UnicodeCategory.TitlecaseLetter:
                case UnicodeCategory.ModifierLetter:
                case UnicodeCategory.OtherLetter:
                case UnicodeCategory.DecimalDigitNumber:
                case UnicodeCategory.LetterNumber:
                case UnicodeCategory.OtherNumber:
                    return true;
                default:
                    return false;
            }
        }
    }
}