using MilanWilczak.FreediveComp.Models;
using NUnit.Framework;
using System;
using System.Linq;

namespace MilanWilczak.FreediveComp.Tests.Models
{
    public abstract class RulesBaseTest
    {
        protected abstract IRules Rule { get; }
        protected abstract string RuleName { get; }
        protected abstract bool ConvertsToPoints { get; }
        protected abstract PerformanceComponent PrimaryComponent { get; }
        protected abstract bool HasSupplementaryDuration { get; }
        protected abstract bool IsPerformanceDifferenceRelevantForSorting { get; }
        protected abstract bool IsDurationDifferenceRelevantForSorting { get; }

        [Test]
        public void RuleAvailableInRepository()
        {
            var repository = new RulesRepository();
            Rules.AddTo(repository);
            Assert.That(repository.Get(RuleName), Is.Not.Null);
            Assert.That(repository.GetAll(), Has.One.Matches(Has.Property("Name").EqualTo(RuleName)));
        }

        [Test]
        public void RuleHasProperties()
        {
            Assert.That(Rule.Name, Is.EqualTo(RuleName));
            Assert.That(Rule.HasPoints, Is.EqualTo(ConvertsToPoints));
            Assert.That(Rule.PrimaryComponent, Is.EqualTo(PrimaryComponent));
            if (ConvertsToPoints)
            {
                Assert.That(Rule.PenalizationsTarget, Is.EqualTo(PerformanceComponent.Points));
                Assert.That(Rule.PointsCalculation, Is.Not.Null);
            }
            else
            {
                Assert.That(Rule.PenalizationsTarget, Is.EqualTo(PrimaryComponent));
            }
            Assert.That(Rule.ShortCalculation, Is.Not.Null);
            Assert.That(Rule.Penalizations, Is.Not.Null);
            Assert.That(Rule.ResultsComparer, Is.Not.Null);
            Assert.That(Rule.HasDuration, Is.EqualTo(
                PrimaryComponent == PerformanceComponent.Duration ||
                HasSupplementaryDuration));
            Assert.That(Rule.HasDistance, Is.EqualTo(PrimaryComponent == PerformanceComponent.Distance));
            Assert.That(Rule.HasDepth, Is.EqualTo(PrimaryComponent == PerformanceComponent.Depth));
        }

        [Test]
        public void VerifyComparer()
        {
            var perfectPerformance = new SortableResult(Rule, 150, 150, 150, 150, 0);
            var perfectCopy = new SortableResult(Rule, 150, 150, 150, 150, 0);
            var announcedTimeDiffers = new SortableResult(Rule, 150, 155, 150, 145, 0);
            var announcedPerformanceDiffers = new SortableResult(Rule, 150, 120, 100, 120, 0);
            var performanceLowerButWhite = new SortableResult(Rule, 120, 120, 120, 120, 0);
            var performanceLowerAndYellow = new SortableResult(Rule, 120, 120, 120, 120, 10);

            VerifyComparer(perfectPerformance, perfectCopy, 0);
            VerifyComparer(perfectPerformance, announcedTimeDiffers, IsDurationDifferenceRelevantForSorting ? -1 : 0);
            VerifyComparer(announcedTimeDiffers, announcedPerformanceDiffers, IsPerformanceDifferenceRelevantForSorting ? -1 : 0);
            VerifyComparer(announcedPerformanceDiffers, performanceLowerButWhite, -1);
            VerifyComparer(performanceLowerButWhite, performanceLowerAndYellow, -1);
        }

        [TestCase(100, 150, false)]
        [TestCase(130, 130, false)]
        [TestCase(150, 130, true)]
        public void ShortPenalizationApplied(double announced, double realized, bool penaltyApplied)
        {
            var announcedPerformance = new Performance();
            Rule.PrimaryComponent.Modify(announcedPerformance, announced);
            var realizedPerformance = new Performance();
            Rule.PrimaryComponent.Modify(realizedPerformance, realized);
            var penalty = Rule.BuildShortPenalization(announcedPerformance, realizedPerformance);
            if (penaltyApplied)
            {
                Assert.That(penalty, Is.Not.Null);
                Assert.That(penalty.IsShortPerformance, Is.True);
                Assert.That(penalty.PenalizationId, Is.Not.Empty);
                Assert.That(penalty.Reason, Is.Not.Empty);
                Assert.That(penalty.ShortReason, Is.Empty);
                Assert.That(penalty.Performance, Is.Not.Null);
                Assert.That(Rule.PenalizationsTarget.Get(penalty.Performance), Is.GreaterThan(0));
            }
            else
            {
                Assert.That(penalty, Is.Null);
            }
        }

        public virtual void GetPointsValue(double performance, double points)
        {
            Assume.That(Rule.HasPoints, Is.True);
            var realizedPerformance = new Performance();
            Rule.PrimaryComponent.Modify(realizedPerformance, performance);
            var actualPoints = Rule.GetPoints(realizedPerformance);
            Assert.That(actualPoints, Is.EqualTo((double?)points).Within(0.01));
        }

        public virtual void ShortPenalizationValue(double announced, double realized, double penalty)
        {
            Assume.That(announced, Is.GreaterThan(realized));
            Assume.That(penalty, Is.GreaterThan(0));
            var announcedPerformance = new Performance();
            Rule.PrimaryComponent.Modify(announcedPerformance, announced);
            var realizedPerformance = new Performance();
            Rule.PrimaryComponent.Modify(realizedPerformance, realized);
            var penalizationPerformance = Rule.BuildShortPenalization(announcedPerformance, realizedPerformance).Performance;
            var actualPenalty = Rule.PenalizationsTarget.Get(penalizationPerformance);
            Assert.That(actualPenalty, Is.EqualTo(penalty).Within(0.01));
        }

        public virtual void RedPenalizationListed(string id, string reason, string shortReason, string cardResult)
        {
            RedPenalizationListed(id, reason, shortReason, CardResult.Parse(cardResult));
        }

        public void RedPenalizationListed(string id, string reason, string shortReason, CardResult cardResult)
        {
            Assert.That(Rule.Penalizations, Has.One.Property("Id").EqualTo(id));
            var rulePenalization = Rule.Penalizations.Single(p => p.Id == id);
            Assert.That(rulePenalization.Reason, Is.EqualTo(reason));
            Assert.That(rulePenalization.ShortReason, Is.EqualTo(shortReason));
            Assert.That(rulePenalization.HasInput, Is.False);
            Assert.That(rulePenalization.CardResult, Is.EqualTo(cardResult));
            Assert.That(rulePenalization.PenaltyCalculation, Is.Null);
        }

        public virtual void RedPenalizationBuilds(string id)
        {
            Assume.That(Rule.Penalizations, Has.One.Property("Id").EqualTo(id));
            var rulePenalization = Rule.Penalizations.Single(p => p.Id == id);
            var penalization = rulePenalization.BuildPenalization(0, null);
            Assert.That(penalization, Is.Not.Null);
            Assert.That(penalization.IsShortPerformance, Is.False);
            Assert.That(penalization.PenalizationId, Is.EqualTo(id));
            Assert.That(penalization.Reason, Is.EqualTo(rulePenalization.Reason));
            Assert.That(penalization.ShortReason, Is.EqualTo(rulePenalization.ShortReason));
            Assert.That(penalization.RuleInput, Is.Null);
            Assert.That(penalization.Performance, new EmptyPerformanceConstraint());
        }

        public virtual void YellowPenalizationListed(string id, string reason, string shortReason, string inputName, string inputUnit)
        {
            Assert.That(Rule.Penalizations, Has.One.Property("Id").EqualTo(id));
            var rulePenalization = Rule.Penalizations.Single(p => p.Id == id);
            Assert.That(rulePenalization.Reason, Is.EqualTo(reason));
            Assert.That(rulePenalization.ShortReason, Is.EqualTo(shortReason));
            Assert.That(rulePenalization.CardResult, Is.EqualTo(CardResult.Yellow));
            Assert.That(rulePenalization.PenaltyCalculation, Is.Not.Null);
            if (inputName == null)
            {
                Assert.That(rulePenalization.HasInput, Is.False);
            }
            else
            {
                Assert.That(rulePenalization.HasInput, Is.True);
                Assert.That(rulePenalization.InputName, Is.EqualTo(inputName));
                Assert.That(rulePenalization.InputUnit, Is.EqualTo(inputUnit));
            }
        }

        public virtual void YellowPenalizationCalculates(string id, double input, double performanceValue, double penaltyValue)
        {
            Assume.That(Rule.Penalizations, Has.One.Property("Id").EqualTo(id));
            var rulePenalization = Rule.Penalizations.Single(p => p.Id == id);
            var realizedPerformance = new Performance();
            if (performanceValue > 0)
            {
                Rule.PrimaryComponent.Modify(realizedPerformance, performanceValue);
            }
            else
            {
                realizedPerformance = null;
            }
            var penalization = rulePenalization.BuildPenalization(input, realizedPerformance);
            Assert.That(penalization, Is.Not.Null);
            Assert.That(penalization.IsShortPerformance, Is.False);
            Assert.That(penalization.PenalizationId, Is.EqualTo(id));
            Assert.That(penalization.Reason, Is.EqualTo(rulePenalization.Reason));
            Assert.That(penalization.ShortReason, Is.EqualTo(rulePenalization.ShortReason));
            if (rulePenalization.HasInput)
            {
                Assert.That(penalization.RuleInput, Is.EqualTo(input));
            }
            else
            {
                Assert.That(penalization.RuleInput, Is.Null);
            }
            Assert.That(penalization.Performance, Is.Not.Null);
            var actualPenaltyValue = Rule.PenalizationsTarget.Get(penalization.Performance);
            Assert.That(actualPenaltyValue, Is.EqualTo(penaltyValue).Within(0.01));
        }

        protected void VerifyComparer(ICombinedResult a, ICombinedResult b, int expected)
        {
            Assert.That(Rule.ResultsComparer.Compare(a, b), Is.EqualTo(expected));
            Assert.That(Rule.ResultsComparer.Compare(b, a), Is.EqualTo(-expected));
        }

        protected class SortableResult : ICombinedResult
        {
            private readonly Performance announced, realized, final;

            public SortableResult(IRules Rule, double realizedPrimary, double realizedTime, double announcedPrimary, double announcedTime, double penalty)
            {
                announced = new Performance();
                realized = new Performance();
                final = new Performance();
                if (Rule.HasDuration)
                {
                    PerformanceComponent.Duration.Modify(announced, announcedTime);
                    PerformanceComponent.Duration.Modify(realized, realizedTime);
                    PerformanceComponent.Duration.Modify(final, realizedTime);
                }
                Rule.PrimaryComponent.Modify(announced, announcedPrimary);
                Rule.PrimaryComponent.Modify(realized, realizedPrimary);
                Rule.PrimaryComponent.Modify(final, realizedPrimary);
                if (Rule.HasPoints)
                {
                    PerformanceComponent.Points.Modify(realized, Rule.GetPoints(realized));
                    PerformanceComponent.Points.Modify(final, Rule.GetPoints(final));
                }
                Rule.PenalizationsTarget.Modify(final, Rule.PenalizationsTarget.Get(final) - penalty);
            }

            public IPerformance Announcement => announced;
            public IPerformance Realized => realized;
            public IPerformance Final => final;
        }
    }

    [TestFixture]
    public class RulesAidaStaTest : RulesBaseTest
    {
        protected override IRules Rule => Rules.AidaSta;
        protected override string RuleName => "AIDA_STA";
        protected override bool ConvertsToPoints => true;
        protected override PerformanceComponent PrimaryComponent => PerformanceComponent.Duration;
        protected override bool HasSupplementaryDuration => false;
        protected override bool IsPerformanceDifferenceRelevantForSorting => true;
        protected override bool IsDurationDifferenceRelevantForSorting => false;

        [TestCase(100.0, 20.0)]
        [TestCase(61, 12.2)]
        public override void GetPointsValue(double performance, double points)
        {
            base.GetPointsValue(performance, points);
        }

        [TestCase(120.0, 100.0, 4.0)]
        [TestCase(120.0, 112.0, 2.0)]
        public override void ShortPenalizationValue(double announced, double realized, double penalty)
        {
            base.ShortPenalizationValue(announced, realized, penalty);
        }

        [TestCase("Blackout", "Blackout", "BO", "Red")]
        [TestCase("SurfaceProtocol", "Surface protocol", "SP", "Red")]
        [TestCase("SupportiveTouch", "Supportive touch", "Touch", "Red")]
        public override void RedPenalizationListed(string id, string reason, string shortReason, string cardResult)
        {
            base.RedPenalizationListed(id, reason, shortReason, cardResult);
        }

        [TestCase("Blackout")]
        [TestCase("SurfaceProtocol")]
        [TestCase("SupportiveTouch")]
        public override void RedPenalizationBuilds(string id)
        {
            base.RedPenalizationBuilds(id);
        }

        [TestCase("EarlyStart", "Early start", "Early", "Time (seconds)", "s")]
        [TestCase("LateStart", "Late start", "Late", "Seconds after OT", "s")]
        public override void YellowPenalizationListed(string id, string reason, string shortReason, string inputName, string inputUnit)
        {
            base.YellowPenalizationListed(id, reason, shortReason, inputName, inputUnit);
        }

        [TestCase("EarlyStart", 7.0, 120.0, 2.0)]
        [TestCase("LateStart", 19.0, 100.0, 2.0)]
        public override void YellowPenalizationCalculates(string id, double input, double performanceValue, double penaltyValue)
        {
            base.YellowPenalizationCalculates(id, input, performanceValue, penaltyValue);
        }
    }

    [TestFixture]
    public class RulesAidaDynTest : RulesBaseTest
    {
        protected override IRules Rule => Rules.AidaDyn;
        protected override string RuleName => "AIDA_DYN";
        protected override bool ConvertsToPoints => true;
        protected override PerformanceComponent PrimaryComponent => PerformanceComponent.Distance;
        protected override bool HasSupplementaryDuration => false;
        protected override bool IsPerformanceDifferenceRelevantForSorting => true;
        protected override bool IsDurationDifferenceRelevantForSorting => false;

        [TestCase(100.0, 50.0)]
        [TestCase(61, 30.5)]
        public override void GetPointsValue(double performance, double points)
        {
            base.GetPointsValue(performance, points);
        }

        [TestCase(120.0, 100.0, 10.0)]
        [TestCase(120.0, 113.0, 3.5)]
        public override void ShortPenalizationValue(double announced, double realized, double penalty)
        {
            base.ShortPenalizationValue(announced, realized, penalty);
        }

        [TestCase("Blackout", "Blackout", "BO", "Red")]
        [TestCase("SurfaceProtocol", "Surface protocol", "SP", "Red")]
        [TestCase("SupportiveTouch", "Supportive touch", "Touch", "Red")]
        public override void RedPenalizationListed(string id, string reason, string shortReason, string cardResult)
        {
            base.RedPenalizationListed(id, reason, shortReason, cardResult);
        }

        [TestCase("Blackout")]
        [TestCase("SurfaceProtocol")]
        [TestCase("SupportiveTouch")]
        public override void RedPenalizationBuilds(string id)
        {
            base.RedPenalizationBuilds(id);
        }

        [TestCase("EarlyStart", "Early start", "Early", "Time (seconds)", "s")]
        [TestCase("LateStart", "Late start", "Late", "Seconds after OT", "s")]
        [TestCase("ExitHelp", "Push/pull on exit", "Exit", null, null)]
        public override void YellowPenalizationListed(string id, string reason, string shortReason, string inputName, string inputUnit)
        {
            base.YellowPenalizationListed(id, reason, shortReason, inputName, inputUnit);
        }

        [TestCase("EarlyStart", 7.0, 120.0, 2.0)]
        [TestCase("LateStart", 19.0, 100.0, 2.0)]
        [TestCase("ExitHelp", 0.0, 100.0, 5.0)]
        public override void YellowPenalizationCalculates(string id, double input, double performanceValue, double penaltyValue)
        {
            base.YellowPenalizationCalculates(id, input, performanceValue, penaltyValue);
        }
    }

    [TestFixture]
    public class RulesAidaCwtTest : RulesBaseTest
    {
        protected override IRules Rule => Rules.AidaCwt;
        protected override string RuleName => "AIDA_CWT";
        protected override bool ConvertsToPoints => true;
        protected override PerformanceComponent PrimaryComponent => PerformanceComponent.Depth;
        protected override bool HasSupplementaryDuration => false;
        protected override bool IsPerformanceDifferenceRelevantForSorting => true;
        protected override bool IsDurationDifferenceRelevantForSorting => false;

        [TestCase(100.0, 100)]
        [TestCase(61, 61)]
        public override void GetPointsValue(double performance, double points)
        {
            base.GetPointsValue(performance, points);
        }

        [TestCase(120.0, 100.0, 21)]
        [TestCase(120.0, 112.0, 9)]
        public override void ShortPenalizationValue(double announced, double realized, double penalty)
        {
            base.ShortPenalizationValue(announced, realized, penalty);
        }

        [TestCase("Blackout", "Blackout", "BO", "Red")]
        [TestCase("SurfaceProtocol", "Surface protocol", "SP", "Red")]
        [TestCase("SupportiveTouch", "Supportive touch", "Touch", "Red")]
        public override void RedPenalizationListed(string id, string reason, string shortReason, string cardResult)
        {
            base.RedPenalizationListed(id, reason, shortReason, cardResult);
        }

        [TestCase("Blackout")]
        [TestCase("SurfaceProtocol")]
        [TestCase("SupportiveTouch")]
        public override void RedPenalizationBuilds(string id)
        {
            base.RedPenalizationBuilds(id);
        }

        [TestCase("EarlyStart", "Early start", "Early", "Time (seconds)", "s")]
        [TestCase("LateStart", "Late start", "Late", "Seconds after OT", "s")]
        [TestCase("GrabLine", "Grab line", "Grab", "Count", "x")]
        [TestCase("NoTag", "No tag delivered", "Tag", null, null)]
        public override void YellowPenalizationListed(string id, string reason, string shortReason, string inputName, string inputUnit)
        {
            base.YellowPenalizationListed(id, reason, shortReason, inputName, inputUnit);
        }

        [TestCase("EarlyStart", 7.0, 120.0, 2.0)]
        [TestCase("LateStart", 19.0, 100.0, 2.0)]
        [TestCase("GrabLine", 3.0, 80.0, 15.0)]
        [TestCase("NoTag", 0.0, 70.0, 1.0)]
        public override void YellowPenalizationCalculates(string id, double input, double performanceValue, double penaltyValue)
        {
            base.YellowPenalizationCalculates(id, input, performanceValue, penaltyValue);
        }
    }

    [TestFixture]
    public class RulesCmasStaTest : RulesBaseTest
    {
        protected override IRules Rule => Rules.CmasSta;
        protected override string RuleName => "CMAS_STA";
        protected override bool ConvertsToPoints => false;
        protected override PerformanceComponent PrimaryComponent => PerformanceComponent.Duration;
        protected override bool HasSupplementaryDuration => false;
        protected override bool IsPerformanceDifferenceRelevantForSorting => true;
        protected override bool IsDurationDifferenceRelevantForSorting => false;

        [TestCase("Blackout")]
        [TestCase("SurfaceProtocol")]
        public override void RedPenalizationBuilds(string id)
        {
            base.RedPenalizationBuilds(id);
        }

        [TestCase("Blackout", "Blackout", "BO", "Red")]
        [TestCase("SurfaceProtocol", "Surface protocol", "SP", "Red")]
        public override void RedPenalizationListed(string id, string reason, string shortReason, string cardResult)
        {
            base.RedPenalizationListed(id, reason, shortReason, cardResult);
        }

        [TestCase(120, 100, 20)]
        public override void ShortPenalizationValue(double announced, double realized, double penalty)
        {
            base.ShortPenalizationValue(announced, realized, penalty);
        }
    }

    [TestFixture]
    public class RulesCmasDynTest : RulesBaseTest
    {
        protected override IRules Rule => Rules.CmasDyn;
        protected override string RuleName => "CMAS_DYN";
        protected override bool ConvertsToPoints => false;
        protected override PerformanceComponent PrimaryComponent => PerformanceComponent.Distance;
        protected override bool HasSupplementaryDuration => false;
        protected override bool IsPerformanceDifferenceRelevantForSorting => true;
        protected override bool IsDurationDifferenceRelevantForSorting => false;

        [TestCase("Blackout")]
        [TestCase("SurfaceProtocol")]
        public override void RedPenalizationBuilds(string id)
        {
            base.RedPenalizationBuilds(id);
        }

        [TestCase("Blackout", "Blackout", "BO", "Red")]
        [TestCase("SurfaceProtocol", "Surface protocol", "SP", "Red")]
        public override void RedPenalizationListed(string id, string reason, string shortReason, string cardResult)
        {
            base.RedPenalizationListed(id, reason, shortReason, cardResult);
        }

        [TestCase(120, 100, 25)]
        public override void ShortPenalizationValue(double announced, double realized, double penalty)
        {
            base.ShortPenalizationValue(announced, realized, penalty);
        }

        [TestCase("Lane", "Out of lane", "Lane", "Count", "x")]
        public override void YellowPenalizationListed(string id, string reason, string shortReason, string inputName, string inputUnit)
        {
            base.YellowPenalizationListed(id, reason, shortReason, inputName, inputUnit);
        }

        [TestCase("Lane", 3, 100, 15)]
        public override void YellowPenalizationCalculates(string id, double input, double performanceValue, double penaltyValue)
        {
            base.YellowPenalizationCalculates(id, input, performanceValue, penaltyValue);
        }
    }

    [TestFixture]
    public class RulesCmasCwtTest : RulesBaseTest
    {
        protected override IRules Rule => Rules.CmasCwt;
        protected override string RuleName => "CMAS_CWT";
        protected override bool ConvertsToPoints => false;
        protected override PerformanceComponent PrimaryComponent => PerformanceComponent.Depth;
        protected override bool HasSupplementaryDuration => true;
        protected override bool IsPerformanceDifferenceRelevantForSorting => true;
        protected override bool IsDurationDifferenceRelevantForSorting => true;

        [TestCase("Blackout")]
        [TestCase("SurfaceProtocol")]
        public override void RedPenalizationBuilds(string id)
        {
            base.RedPenalizationBuilds(id);
        }

        [TestCase("Blackout", "Blackout", "BO", "Red")]
        [TestCase("SurfaceProtocol", "Surface protocol", "SP", "Red")]
        public override void RedPenalizationListed(string id, string reason, string shortReason, string cardResult)
        {
            base.RedPenalizationListed(id, reason, shortReason, cardResult);
        }

        [TestCase(120, 100, 25)]
        public override void ShortPenalizationValue(double announced, double realized, double penalty)
        {
            base.ShortPenalizationValue(announced, realized, penalty);
        }

        [TestCase("NoTag", "No tag delivered", "Tag", null, null)]
        [TestCase("Grab", "Grab line", "Grab", "Count", "x")]
        public override void YellowPenalizationListed(string id, string reason, string shortReason, string inputName, string inputUnit)
        {
            base.YellowPenalizationListed(id, reason, shortReason, inputName, inputUnit);
        }

        [TestCase("NoTag", 0, 100, 5)]
        [TestCase("Grab", 3, 100, 15)]
        public override void YellowPenalizationCalculates(string id, double input, double performanceValue, double penaltyValue)
        {
            base.YellowPenalizationCalculates(id, input, performanceValue, penaltyValue);
        }
    }

    [TestFixture]
    public class RulesCmasJumpBlueTest : RulesBaseTest
    {
        protected override IRules Rule => Rules.CmasJumpBlue;
        protected override string RuleName => "CMAS_JB";
        protected override bool ConvertsToPoints => false;
        protected override PerformanceComponent PrimaryComponent => PerformanceComponent.Distance;
        protected override bool HasSupplementaryDuration => false;
        protected override bool IsPerformanceDifferenceRelevantForSorting => true;
        protected override bool IsDurationDifferenceRelevantForSorting => false;

        [TestCase("Blackout")]
        [TestCase("SurfaceProtocol")]
        [TestCase("NoDisc")]
        [TestCase("Corner")]
        public override void RedPenalizationBuilds(string id)
        {
            base.RedPenalizationBuilds(id);
        }

        [TestCase("Blackout", "Blackout", "BO", "Red")]
        [TestCase("SurfaceProtocol", "Surface protocol", "SP", "Red")]
        [TestCase("NoDisc", "Not touched disc", "Disc", "Red")]
        [TestCase("Corner", "Cut corner", "Corner", "Red")]
        public override void RedPenalizationListed(string id, string reason, string shortReason, string cardResult)
        {
            base.RedPenalizationListed(id, reason, shortReason, cardResult);
        }

        [TestCase(120, 100, 25)]
        public override void ShortPenalizationValue(double announced, double realized, double penalty)
        {
            base.ShortPenalizationValue(announced, realized, penalty);
        }

        [TestCase("BadMarker", "No tag secured", "Tag", null, null)]
        public override void YellowPenalizationListed(string id, string reason, string shortReason, string inputName, string inputUnit)
        {
            base.YellowPenalizationListed(id, reason, shortReason, inputName, inputUnit);
        }

        [TestCase("BadMarker", 0, 100, 5)]
        public override void YellowPenalizationCalculates(string id, double input, double performanceValue, double penaltyValue)
        {
            base.YellowPenalizationCalculates(id, input, performanceValue, penaltyValue);
        }
    }

}
