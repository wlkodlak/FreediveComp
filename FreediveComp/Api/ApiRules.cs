using FreediveComp.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FreediveComp.Api
{
    public interface IApiRules
    {
        List<RulesDto> GetRules();
        double GetPoints(string rulesName, PerformanceDto performance);
        PenalizationDto GetShort(string rulesName, GetShortPenalizationRequest request);
        PenalizationDto GetPenalization(string rulesName, GetCalculatedPenalizationRequest request);
    }

    public class ApiRules : IApiRules
    {
        private readonly IRulesRepository rulesRepository;

        public ApiRules(IRulesRepository rulesRepository)
        {
            this.rulesRepository = rulesRepository;
        }

        public List<RulesDto> GetRules()
        {
            return rulesRepository.GetAll().Select(BuildRules).ToList();
        }

        private static RulesDto BuildRules(IRules rules)
        {
            return new RulesDto
            {
                Name = rules.Name,
                HasDepth = rules.HasDepth,
                HasDistance = rules.HasDistance,
                HasDuration = rules.HasDuration,
                HasPoints = rules.HasPoints,
                PenalizationsTarget = rules.PenalizationsTarget.ToString(),
                PrimaryComponent = rules.PrimaryComponent.ToString(),
                Penalizations = rules.Penalizations.Select(BuildRulesPenalization).ToList(),
                PointsCalculation = BuildCalculation(rules.PointsCalculation),
                ShortCalculation = BuildCalculation(rules.ShortCalculation)
            };
        }

        private static RulesPenalizationDto BuildRulesPenalization(IRulesPenalization penalization)
        {
            return new RulesPenalizationDto
            {
                Id = penalization.Id,
                CardResult = penalization.CardResult.ToString(),
                HasInput = penalization.HasInput,
                InputName = penalization.InputName,
                InputUnit = penalization.InputUnit,
                Reason = penalization.Reason,
                ShortReason = penalization.ShortReason,
                Calculation = BuildCalculation(penalization.PenaltyCalculation)
            };
        }

        private static CalculationDto BuildCalculation(ICalculation calculation)
        {
            if (calculation == null)
            {
                return null;
            }
            else if (calculation is CalculationConstant constant)
            {
                return new CalculationDto
                {
                    Operation = "Constant",
                    Constant = constant.Value
                };
            }
            else if (calculation is CalculationVariable variable)
            {
                return new CalculationDto
                {
                    Operation = "Variable",
                    Variable = variable.Name
                };
            }
            else if (calculation is CalculationCeiling ceiling)
            {
                return new CalculationDto
                {
                    Operation = "Ceiling",
                    ArgumentA = BuildCalculation(ceiling.Argument)
                };
            }
            else if (calculation is CalculationOperator operation)
            {
                return new CalculationDto
                {
                    Operation = operation.Operation,
                    ArgumentA = BuildCalculation(operation.ArgumentA),
                    ArgumentB = BuildCalculation(operation.ArgumentB),
                };
            }
            else
            {
                return null;
            }
        }

        public double GetPoints(string rulesName, PerformanceDto performance)
        {
            if (string.IsNullOrEmpty(rulesName)) throw new ArgumentNullException("Missing RulesName");
            if (performance == null) throw new ArgumentNullException("Missing Performance");

            var rules = rulesRepository.Get(rulesName);
            if (rules == RulesUnknown.Default) throw new ArgumentOutOfRangeException("Unknown RulesName " + rulesName);

            return rules.GetPoints(ExtractPerformance(performance));
        }

        public PenalizationDto GetShort(string rulesName, GetShortPenalizationRequest request)
        {
            if (string.IsNullOrEmpty(rulesName)) throw new ArgumentNullException("Missing RulesName");
            if (request.Announced == null) throw new ArgumentNullException("Missing Announced");
            if (request.Realized == null) throw new ArgumentNullException("Missing Realized");

            var rules = rulesRepository.Get(rulesName);
            if (rules == RulesUnknown.Default) throw new ArgumentOutOfRangeException("Unknown RulesName " + rulesName);

            var calculatedPenalization = rules.BuildShortPenalization(ExtractPerformance(request.Announced), ExtractPerformance(request.Realized));
            return BuildPenalization(calculatedPenalization);
        }

        public PenalizationDto GetPenalization(string rulesName, GetCalculatedPenalizationRequest request)
        {
            if (string.IsNullOrEmpty(rulesName)) throw new ArgumentNullException("Missing RulesName");
            if (string.IsNullOrEmpty(rulesName)) throw new ArgumentNullException("Missing RulesName");
            if (string.IsNullOrEmpty(request.PenalizationId)) throw new ArgumentNullException("Missing PenalizationId");
            if (request.Input <= 0) throw new ArgumentNullException("Input must be positive");
            if (request.Realized == null) throw new ArgumentNullException("Missing Realized");

            var rules = rulesRepository.Get(rulesName);
            if (rules == RulesUnknown.Default) throw new ArgumentOutOfRangeException("Unknown RulesName " + rulesName);

            var rulesPenalization = rules.Penalizations.FirstOrDefault(r => r.Id == request.PenalizationId);
            if (rulesPenalization == null) throw new ArgumentOutOfRangeException("Unknown PenalizationId " + request.PenalizationId);

            var calculatedPenalization = rulesPenalization.BuildPenalization(request.Input, ExtractPerformance(request.Realized));
            return BuildPenalization(calculatedPenalization);
        }

        private static PenalizationDto BuildPenalization(Penalization penalization)
        {
            return new PenalizationDto
            {
                PenalizationId = penalization.PenalizationId,
                Reason = penalization.Reason,
                ShortReason = penalization.ShortReason,
                IsShortPerformance = penalization.IsShortPerformance,
                RuleInput = penalization.RuleInput,
                Performance = BuildPerformance(penalization.Performance),
            };
        }

        private static Performance ExtractPerformance(PerformanceDto performance)
        {
            return new Performance
            {
                Depth = performance.Depth,
                Distance = performance.Distance,
                Duration = performance.Duration,
                Points = performance.Points,
            };
        }

        private static PerformanceDto BuildPerformance(Performance performance)
        {
            return new PerformanceDto
            {
                Depth = performance.Depth,
                Distance = performance.Distance,
                Duration = performance.Duration,
                Points = performance.Points,
            };
        }
    }
}