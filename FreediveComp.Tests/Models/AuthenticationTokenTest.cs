using MilanWilczak.FreediveComp.Models;
using NUnit.Framework;

namespace MilanWilczak.FreediveComp.Tests.Models
{
    [TestFixture]
    public class AuthenticationTokenTest
    {
        [Test]
        public void WhenGenerating_NothingIsEmpty()
        {
            var token = AuthenticationToken.Generate("Race184", "Judge001");
            Assert.That(token.RaceId, Is.EqualTo("Race184"));
            Assert.That(token.JudgeId, Is.EqualTo("Judge001"));
            Assert.That(token.Token, Is.Not.Empty.Or.Null);
        }

        [Test]
        public void WhenBuilding_NothingIsEmpty()
        {
            const string authPart = "321ac354e654token321ec";
            var token = AuthenticationToken.Build("Race184", "Judge001", authPart);
            Assert.That(token.RaceId, Is.EqualTo("Race184"));
            Assert.That(token.JudgeId, Is.EqualTo("Judge001"));
            Assert.That(token.Token, Is.EqualTo(authPart));
        }

        [Test]
        public void CheckTokenEquality()
        {
            const string race1 = "race01";
            const string race2 = "race02";
            const string judge1 = "judge01";
            const string judge2 = "judge02";
            const string auth1 = "21ca5e43c2ad4fd";
            const string auth2 = "21dasd5f43sd21e";
            var token1 = AuthenticationToken.Build(race1, judge1, auth1);
            var token2 = AuthenticationToken.Build(race1, judge1, auth1);
            var token3 = AuthenticationToken.Build(race2, judge1, auth1);
            var token4 = AuthenticationToken.Build(race1, judge2, auth1);
            var token5 = AuthenticationToken.Build(race1, judge1, auth2);
            Assert.That(token1.GetHashCode(), Is.EqualTo(token2.GetHashCode()));
            Assert.That(token1, Is.EqualTo(token2));
            Assert.That(token1, Is.Not.EqualTo(token3));
            Assert.That(token1, Is.Not.EqualTo(token4));
            Assert.That(token1, Is.Not.EqualTo(token5));
        }

        [Test]
        public void ParsedTokenIsEqualToOriginal()
        {
            var token1 = AuthenticationToken.Build("race01", "judge01", "lkjdlcvjlker");
            Assert.That(AuthenticationToken.Parse(token1.ToString()), Is.EqualTo(token1));
        }

        [Test]
        public void UnparsableTokenReturnsNull()
        {
            Assert.That(AuthenticationToken.Parse(""), Is.Null);
        }
    }
}
