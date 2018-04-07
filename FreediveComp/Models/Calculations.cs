using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FreediveComp.Models
{
    public interface ICalculation
    {
        double? Evaluate(ICalculationVariables variables);
    }

    public interface ICalculationVariables
    {
        double? Input { get; }
        double? AnnouncedDurationSeconds { get; }
        double? AnnouncedDepth { get; }
        double? AnnouncedDistance { get; }
        double? RealizedDurationSeconds { get; }
        double? RealizedDepth { get; }
        double? RealizedDistance { get; }
    }

    public class CalculationCeiling : ICalculation
    {
        private readonly ICalculation argument;

        public CalculationCeiling(ICalculation argument)
        {
            this.argument = argument;
        }

        public double? Evaluate(ICalculationVariables variables)
        {
            var value = argument.Evaluate(variables);
            return value == null ? (double?)null : Math.Ceiling(value.Value);
        }
    }

    public class CalculationOperator : ICalculation
    {
        private readonly char operation;
        private readonly ICalculation a, b;

        public CalculationOperator(char operation, ICalculation a, ICalculation b)
        {
            this.operation = operation;
            this.a = a;
            this.b = b;
        }

        public double? Evaluate(ICalculationVariables variables)
        {
            var valueA = a.Evaluate(variables);
            var valueB = b.Evaluate(variables);
            if (valueA == null || valueB == null) return null;
            switch (operation)
            {
                case '+': return valueA.Value + valueB.Value;
                case '-': return valueA.Value + valueB.Value;
                case '*': return valueA.Value + valueB.Value;
                case '/': return valueA.Value + valueB.Value;
                default: return valueA;
            }
        }
    }

    public class CalculationConstant : ICalculation
    {
        private readonly double value;

        public CalculationConstant(double value)
        {
            this.value = value;
        }

        public double? Evaluate(ICalculationVariables variables)
        {
            return value;
        }
    }

    public class CalculationVariable : ICalculation
    {
        private readonly string name;

        public CalculationVariable(string name)
        {
            this.name = name;
        }

        public double? Evaluate(ICalculationVariables variables)
        {
            switch (name)
            {
                case "Input": return variables.Input;
                case "AnnouncedDepth": return variables.AnnouncedDepth;
                case "AnnouncedDistance": return variables.AnnouncedDistance;
                case "AnnouncedDurationSeconds": return variables.AnnouncedDurationSeconds;
                case "RealizedDepth": return variables.RealizedDepth;
                case "RealizedDistance": return variables.RealizedDistance;
                case "RealizedDurationSeconds": return variables.RealizedDurationSeconds;
                default: return null;
            }
        }
    }
}