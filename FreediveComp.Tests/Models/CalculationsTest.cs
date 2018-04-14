using MilanWilczak.FreediveComp.Models;
using NUnit.Framework;

namespace MilanWilczak.FreediveComp.Tests.Models
{
    [TestFixture]
    public class CalculationsTest
    {
        [TestCase(5.1)]
        [TestCase(3.3)]
        [TestCase(null)]
        public void CalculateVariableInput(double? value)
        {
            Assert.That(Calculation.Input.Evaluate(new CalculationVariables(value, null, null)), Is.EqualTo(value));
        }

        [TestCase(5.1)]
        [TestCase(3.3)]
        [TestCase(null)]
        public void CalculateVariableAnnouncedDuration(double? value)
        {
            Assert.That(Calculation.AnnouncedDuration.Evaluate(new CalculationVariables(null, BuildPerformance(duration: value), null)), Is.EqualTo(value));
        }

        [TestCase(5.1)]
        [TestCase(3.3)]
        [TestCase(null)]
        public void CalculateVariableAnnouncedDistance(double? value)
        {
            Assert.That(Calculation.AnnouncedDistance.Evaluate(new CalculationVariables(null, BuildPerformance(distance: value), null)), Is.EqualTo(value));
        }

        [TestCase(5.1)]
        [TestCase(3.3)]
        [TestCase(null)]
        public void CalculateVariableAnnouncedDepth(double? value)
        {
            Assert.That(Calculation.AnnouncedDepth.Evaluate(new CalculationVariables(null, BuildPerformance(depth: value), null)), Is.EqualTo(value));
        }

        [TestCase(5.1)]
        [TestCase(3.3)]
        [TestCase(null)]
        public void CalculateVariableRealizedDuration(double? value)
        {
            Assert.That(Calculation.RealizedDuration.Evaluate(new CalculationVariables(null, null, BuildPerformance(duration: value))), Is.EqualTo(value));
        }

        [TestCase(5.1)]
        [TestCase(3.3)]
        [TestCase(null)]
        public void CalculateVariableRealizedDistance(double? value)
        {
            Assert.That(Calculation.RealizedDistance.Evaluate(new CalculationVariables(null, null, BuildPerformance(distance: value))), Is.EqualTo(value));
        }

        [TestCase(5.1)]
        [TestCase(3.3)]
        [TestCase(null)]
        public void CalculateVariableRealizedDepth(double? value)
        {
            Assert.That(Calculation.RealizedDepth.Evaluate(new CalculationVariables(null, null, BuildPerformance(depth: value))), Is.EqualTo(value));
        }

        [TestCase(5.1)]
        [TestCase(3.3)]
        public void CalculateConstant(double value)
        {
            Assert.That(Calculation.Constant(value).Evaluate(new CalculationVariables(null, null, null)), Is.EqualTo(value));
        }

        [TestCase(0, 0)]
        [TestCase(null, null)]
        [TestCase(4, 4)]
        [TestCase(4.1, 5)]
        [TestCase(4.9, 5)]
        public void CalculateCeiling(double? input, double? result)
        {
            Assert.That(Calculation.Ceiling(Calculation.Input).Evaluate(new CalculationVariables(input, null, null)), Is.EqualTo(result).Within(0.01));
        }

        [TestCase(null, null, null)]
        [TestCase(2.4, null, null)]
        [TestCase(null, 4.2, null)]
        [TestCase(4.7, 1.1, 5.8)]
        public void CalculateOperationPlus(double? a, double? b, double? result)
        {
            var calculation = Calculation.Plus(Calculation.AnnouncedDistance, Calculation.RealizedDistance);
            var variables = new CalculationVariables(null, BuildPerformance(distance: a), BuildPerformance(distance: b));
            Assert.That(calculation.Evaluate(variables), Is.EqualTo(result).Within(0.01));
        }


        [TestCase(null, null, null)]
        [TestCase(2.4, null, null)]
        [TestCase(null, 4.2, null)]
        [TestCase(4.7, 1.1, 3.6)]
        public void CalculateOperationMinus(double? a, double? b, double? result)
        {
            var calculation = Calculation.Minus(Calculation.AnnouncedDistance, Calculation.RealizedDistance);
            var variables = new CalculationVariables(null, BuildPerformance(distance: a), BuildPerformance(distance: b));
            Assert.That(calculation.Evaluate(variables), Is.EqualTo(result).Within(0.01));
        }


        [TestCase(null, null, null)]
        [TestCase(2.4, null, null)]
        [TestCase(null, 4.2, null)]
        [TestCase(4.2, 2.0, 8.4)]
        public void CalculateOperationMultiply(double? a, double? b, double? result)
        {
            var calculation = Calculation.Multiply(Calculation.AnnouncedDistance, Calculation.RealizedDistance);
            var variables = new CalculationVariables(null, BuildPerformance(distance: a), BuildPerformance(distance: b));
            Assert.That(calculation.Evaluate(variables), Is.EqualTo(result).Within(0.01));
        }


        [TestCase(null, null, null)]
        [TestCase(2.4, null, null)]
        [TestCase(null, 4.2, null)]
        [TestCase(9.6, 3.0, 3.2)]
        public void CalculateOperationDivide(double? a, double? b, double? result)
        {
            var calculation = Calculation.Divide(Calculation.AnnouncedDistance, Calculation.RealizedDistance);
            var variables = new CalculationVariables(null, BuildPerformance(distance: a), BuildPerformance(distance: b));
            Assert.That(calculation.Evaluate(variables), Is.EqualTo(result).Within(0.01));
        }

        [Test]
        public void CalculationsExposeTheirParameters()
        {
            var a = Calculation.AnnouncedDistance;
            var b = Calculation.RealizedDistance;
            Assert.That(new CalculationConstant(5.3).Value, Is.EqualTo(5.3));
            Assert.That(new CalculationVariable("Input").Name, Is.EqualTo("Input"));
            Assert.That(new CalculationCeiling(a).Argument, Is.SameAs(a));
            Assert.That(new CalculationOperator('+', a, b).Operation, Is.EqualTo("Plus"));
            Assert.That(new CalculationOperator('-', a, b).Operation, Is.EqualTo("Minus"));
            Assert.That(new CalculationOperator('+', a, b).ArgumentA, Is.SameAs(a));
            Assert.That(new CalculationOperator('+', a, b).ArgumentB, Is.SameAs(b));
        }

        private IPerformance BuildPerformance(double? duration = null, double? distance = null, double? depth = null, double? points = null)
        {
            return new Performance
            {
                DurationSeconds = duration,
                Distance = distance,
                Depth = depth,
                Points = points
            };
        }
    }
}
