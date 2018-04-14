using MilanWilczak.FreediveComp.Models;
using NUnit.Framework;
using System.Linq;

namespace MilanWilczak.FreediveComp.Tests.Models
{
    [TestFixture]
    public class SearchTokenizerTest
    {
        [Test]
        public void EmptyInput()
        {
            ExpectTokens("");
        }

        [Test]
        public void SingleLowercaseWord()
        {
            ExpectTokens("competition", "competition");
        }

        [Test]
        public void SeveralCasedWords()
        {
            ExpectTokens("AIDA World Championchip", "aida", "world", "championchip");
        }

        [Test]
        public void SeveralAccentedWords()
        {
            ExpectTokens("MČR 2018 Pardubice", "mcr", "2018", "pardubice");
        }

        [Test]
        public void NonspaceSeparators()
        {
            ExpectTokens("Freedivingový souboj Česko-Slovensko", "freedivingovy", "souboj", "cesko", "slovensko");
        }

        private void ExpectTokens(string query, params string[] tokens)
        {
            var actualTokens = new SearchTokenizer().GetTokens(query).ToArray();
            Assert.That(actualTokens, Is.EqualTo(tokens));
        }
    }
}
