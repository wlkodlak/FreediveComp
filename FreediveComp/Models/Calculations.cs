using System;

namespace MilanWilczak.FreediveComp.Models
{
    public interface ICalculation
    {
        double? Evaluate(ICalculationVariables variables);
    }

    public static class Calculation
    {
        public static ICalculation Ceiling(ICalculation argument) => new CalculationCeiling(argument);
        public static ICalculation Multiply(ICalculation a, ICalculation b) => new CalculationOperator('*', a, b);
        public static ICalculation Plus(ICalculation a, ICalculation b) => new CalculationOperator('+', a, b);
        public static ICalculation Minus(ICalculation a, ICalculation b) => new CalculationOperator('-', a, b);
        public static ICalculation Divide(ICalculation a, ICalculation b) => new CalculationOperator('/', a, b);
        public static ICalculation Constant(double value) => new CalculationConstant(value);
        public static ICalculation Input => new CalculationVariable("Input");
        public static ICalculation AnnouncedDuration => new CalculationVariable("AnnouncedDuration");
        public static ICalculation AnnouncedDepth => new CalculationVariable("AnnouncedDepth");
        public static ICalculation AnnouncedDistance => new CalculationVariable("AnnouncedDistance");
        public static ICalculation RealizedDuration => new CalculationVariable("RealizedDuration");
        public static ICalculation RealizedDepth => new CalculationVariable("RealizedDepth");
        public static ICalculation RealizedDistance => new CalculationVariable("RealizedDistance");
    }

    public interface ICalculationVariables
    {
        double? Input { get; }
        double? AnnouncedDuration { get; }
        double? AnnouncedDepth { get; }
        double? AnnouncedDistance { get; }
        double? RealizedDuration { get; }
        double? RealizedDepth { get; }
        double? RealizedDistance { get; }
    }

    public class CalculationVariables : ICalculationVariables
    {
        private readonly double? input;
        private readonly IPerformance announced, realized;

        public CalculationVariables(double? input, IPerformance announced, IPerformance realized)
        {
            this.input = input;
            this.announced = announced;
            this.realized = realized;
        }

        public double? Input => input;
        public double? AnnouncedDuration => announced?.DurationSeconds();
        public double? AnnouncedDepth => announced?.Depth;
        public double? AnnouncedDistance => announced?.Distance;
        public double? RealizedDuration => realized?.DurationSeconds();
        public double? RealizedDepth => realized?.Depth;
        public double? RealizedDistance => realized?.Distance;
    }

    public class CalculationCeiling : ICalculation
    {
        private readonly ICalculation argument;

        public CalculationCeiling(ICalculation argument)
        {
            this.argument = argument;
        }

        public ICalculation Argument => argument;

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

        public string Operation
        {
            get
            {
                switch (operation)
                {
                    case '+': return "Plus";
                    case '-': return "Minus";
                    case '*': return "Multiply";
                    case '/': return "Divide";
                    default: return null;
                }
            }
        }
        public ICalculation ArgumentA => a;
        public ICalculation ArgumentB => b;

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

        public double Value => value;

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

        public string Name => name;

        public double? Evaluate(ICalculationVariables variables)
        {
            switch (name)
            {
                case "Input": return variables.Input;
                case "AnnouncedDepth": return variables.AnnouncedDepth;
                case "AnnouncedDistance": return variables.AnnouncedDistance;
                case "AnnouncedDuration": return variables.AnnouncedDuration;
                case "RealizedDepth": return variables.RealizedDepth;
                case "RealizedDistance": return variables.RealizedDistance;
                case "RealizedDuration": return variables.RealizedDuration;
                default: return null;
            }
        }
    }
}