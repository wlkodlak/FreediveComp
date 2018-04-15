using MilanWilczak.FreediveComp.Models;
using NUnit.Framework.Constraints;

namespace MilanWilczak.FreediveComp.Tests
{
    public class EmptyPerformanceConstraint : Constraint
    {
        public EmptyPerformanceConstraint()
        {
            Description = "empty performance";
        }

        public override ConstraintResult ApplyTo<TActual>(TActual actual)
        {
            bool success;
            if (actual == null) success = true;
            else if (actual is IPerformance performance) success = IsSatisfiedBy(performance);
            else success = false;
            return new ConstraintResult(this, actual, success);
        }

        private bool IsSatisfiedBy(IPerformance performance)
        {
            return
                performance.Duration == null &&
                performance.Distance == null &&
                performance.Depth == null &&
                performance.Points == null;
        }
    }
}
