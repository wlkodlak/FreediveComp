using MilanWilczak.FreediveComp.Models;
using NUnit.Framework;

namespace MilanWilczak.FreediveComp.Tests
{
    [TestFixture]
    public class JudgePrincipalTest
    {
        [Test]
        public void AdminJudgeProperties()
        {
            var judge = new Judge
            {
                IsAdmin = true,
                JudgeId = "adm0",
                Name = "John Admin Specialist"
            };

            var principal = new JudgePrincipal(judge);
            Assert.That(principal.Judge, Is.SameAs(judge));
            Assert.That(principal.Identity.IsAuthenticated, Is.True);
            Assert.That(principal.Identity.Name, Is.EqualTo(judge.Name));
            Assert.That(principal.Identity.AuthenticationType, Is.Not.Empty.Or.Null);
            Assert.That(principal.IsInRole("Admin"), Is.True);
            Assert.That(principal.IsInRole("Judge"), Is.True);
        }

        [Test]
        public void RegularJudgeProperties()
        {
            var judge = new Judge
            {
                JudgeId = "judge01",
                Name = "Peter Just"
            };

            var principal = new JudgePrincipal(judge);
            Assert.That(principal.Judge, Is.SameAs(judge));
            Assert.That(principal.Identity.IsAuthenticated, Is.True);
            Assert.That(principal.Identity.Name, Is.EqualTo(judge.Name));
            Assert.That(principal.Identity.AuthenticationType, Is.Not.Empty.Or.Null);
            Assert.That(principal.IsInRole("Admin"), Is.False);
            Assert.That(principal.IsInRole("Judge"), Is.True);
        }
    }
}
