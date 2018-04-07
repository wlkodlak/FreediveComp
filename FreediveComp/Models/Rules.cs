using System;
using System.Collections.Generic;
using System.Linq;

namespace FreediveComp.Models
{
    public interface IRulesRepository
    {
        IRules Get(string rulesName);
        List<IRules> GetAll();
        HashSet<string> GetNames();
    }

    public class RulesRepository : IRulesRepository
    {
        private readonly Dictionary<string, IRules> rulesIndex;

        public RulesRepository()
        {
            rulesIndex = new Dictionary<string, IRules>();
        }

        public RulesRepository Add(IRules rules)
        {
            rulesIndex[rules.Name] = rules;
            return this;
        }

        public RulesRepository AutoFill()
        {
            foreach (var type in typeof(RulesRepository).Assembly.GetTypes())
            {
                if (!typeof(IRules).IsAssignableFrom(type)) continue;
                Add((IRules)Activator.CreateInstance(type));
            }
            return this;
        }

        public List<IRules> GetAll()
        {
            return rulesIndex.Values.ToList();
        }

        public IRules Get(string rulesName)
        {
            IRules found;
            if (rulesIndex.TryGetValue(rulesName, out found)) return found;
            return RulesUnknown.Default;
        }

        public HashSet<string> GetNames()
        {
            return new HashSet<string>(rulesIndex.Keys);
        }
    }

    public interface IRules
    {
        string Name { get; }
        bool HasDuration { get; }
        bool HasDistance { get; }
        bool HasDepth { get; }
        bool HasPoints { get; }
        PerformanceComponent PrimaryComponent { get; }
        PerformanceComponent PenalizationsTarget { get; }
        ICalculation PointsCalculation { get; }
        double GetPoints(IPerformance result);
        IEnumerable<IRulesPenalization> Penalizations { get; }
        Penalization BuildShortPenalization(IPerformance announcement, IPerformance realized);
        ICalculation ShortCalculation { get; }
        IComparer<ICombinedResult> ResultsComparer { get; }
    }

    public interface IRulesPenalization
    {
        string Id { get; }
        string Reason { get; }
        string ShortReason { get; }
        bool HasInput { get; }
        string InputName { get; }
        string InputUnit { get; }
        CardResult CardResult { get; }
        Penalization BuildPenalization(double input, Performance result);
        ICalculation PenaltyCalculation { get; }
    }

    public interface IPerformance
    {
        TimeSpan? Duration { get; }
        double? Depth { get; }
        double? Distance { get; }
        double? Points { get; }
    }

    public static class PerformanceExtension
    {
        public static double? DurationSeconds(this IPerformance performance)
        {
            return performance.Duration == null ? (double?)null : performance.Duration.Value.TotalSeconds;
        }
    }

    public interface ICombinedResult
    {
        IPerformance Announcement { get; }
        IPerformance Realized { get; }
        IPerformance Final { get; }
    }

    public class CombinedResultFinalResultComparer<T> : IComparer<ICombinedResult> where T : struct
    {
        private readonly Func<IPerformance, T?> extractor;
        private readonly IComparer<T> comparer;

        public CombinedResultFinalResultComparer(Func<IPerformance, T?> extractor, IComparer<T> comparer)
        {
            this.extractor = extractor;
            this.comparer = comparer;
        }

        public int Compare(ICombinedResult x, ICombinedResult y)
        {
            T extractedX = extractor(x.Final) ?? default(T);
            T extractedY = extractor(y.Final) ?? default(T);
            return comparer.Compare(extractedX, extractedY);
        }
    }

    public class CombinedResultAnnouncementDifferenceComparer<T> : IComparer<ICombinedResult> where T : struct
    {
        private readonly Func<IPerformance, T?> extractor;
        private readonly Func<T, T, double> differenceCalculator;

        public CombinedResultAnnouncementDifferenceComparer(Func<IPerformance, T?> extractor, Func<T, T, double> differenceCalculator)
        {
            this.extractor = extractor;
            this.differenceCalculator = differenceCalculator;
        }

        public int Compare(ICombinedResult x, ICombinedResult y)
        {
            T resultX = extractor(x.Realized) ?? default(T);
            T resultY = extractor(y.Realized) ?? default(T);
            T announcementX = extractor(x.Announcement) ?? default(T);
            T announcementY = extractor(y.Announcement) ?? default(T);
            double diffX = Math.Abs(differenceCalculator(resultX, announcementX));
            double diffY = Math.Abs(differenceCalculator(resultY, announcementY));

            if (diffX < diffY) return -1;
            if (diffX > diffY) return 1;
            return 0;
        }
    }

    public class CombinedResultCompositeComparer : IComparer<ICombinedResult>
    {
        private const int Capacity = 4;
        private int count;
        private readonly IComparer<ICombinedResult>[] comparers;
        private readonly bool[] sortOrders;

        public CombinedResultCompositeComparer()
        {
            this.count = 0;
            this.comparers = new IComparer<ICombinedResult>[Capacity];
            this.sortOrders = new bool[Capacity];
        }

        public CombinedResultCompositeComparer Ascending(IComparer<ICombinedResult> comparer)
        {
            return Add(comparer, true);
        }

        public CombinedResultCompositeComparer Descending(IComparer<ICombinedResult> comparer)
        {
            return Add(comparer, false);
        }

        private CombinedResultCompositeComparer Add(IComparer<ICombinedResult> comparer, bool ascending)
        {
            if (count >= Capacity) throw new NotSupportedException("Too many comparers");
            comparers[count] = comparer;
            sortOrders[count] = ascending;
            count++;
            return this;
        }

        public int Compare(ICombinedResult x, ICombinedResult y)
        {
            for (int i = 0; i < count; i++)
            {
                int comparison = comparers[i].Compare(x, y);
                if (!sortOrders[i]) comparison = -comparison;
                if (comparison != 0) return comparison;
            }
            return 0;
        }
    }

    public static class CombinedResultsComparers
    {
        public static readonly IComparer<ICombinedResult> FinalPoints = new CombinedResultFinalResultComparer<double>(r => r.Points, Comparer<double>.Default);
        public static readonly IComparer<ICombinedResult> FinalDuration = new CombinedResultFinalResultComparer<TimeSpan>(r => r.Duration, Comparer<TimeSpan>.Default);
        public static readonly IComparer<ICombinedResult> FinalDistance = new CombinedResultFinalResultComparer<double>(r => r.Distance, Comparer<double>.Default);
        public static readonly IComparer<ICombinedResult> FinalDepth = new CombinedResultFinalResultComparer<double>(r => r.Depth, Comparer<double>.Default);

        public static readonly IComparer<ICombinedResult> DiffDuration = new CombinedResultAnnouncementDifferenceComparer<TimeSpan>(r => r.Duration, (r, a) => (r - a).TotalSeconds);
        public static readonly IComparer<ICombinedResult> DiffDistance = new CombinedResultAnnouncementDifferenceComparer<double>(r => r.Distance, (r, a) => r - a);
        public static readonly IComparer<ICombinedResult> DiffDepth = new CombinedResultAnnouncementDifferenceComparer<double>(r => r.Depth, (r, a) => r - a);
    }

    public class PointsCalculator
    {
        public static double GetPoints(IPerformance performance, ICalculation calculation)
        {
            return calculation.Evaluate(new CalculationVariables(null, null, performance)) ?? 0;
        }
    }

    public class ShortPenalizationCalculator
    {
        public static Penalization Build(IPerformance announced, IPerformance realized, IRules rules)
        {
            double announcedValue = rules.PrimaryComponent.Get(announced) ?? 0;
            double realizedValue = rules.PrimaryComponent.Get(realized) ?? 0;
            if (announcedValue <= realizedValue) return null;
            var penalization = new Penalization
            {
                IsShortPerformance = true,
                ShortReason = "",
                PenalizationId = "Short",
                Reason = "Short performance",
                Performance = new Performance()
            };
            var penalty = rules.ShortCalculation.Evaluate(new CalculationVariables(null, announced, realized));
            rules.PenalizationsTarget.Modify(penalization.Performance, penalty);
            return penalization;
        }
    }

    public class RulesAidaSta : IRules
    {
        public string Name => "AIDA_STA";

        public bool HasDuration => true;

        public bool HasDistance => false;

        public bool HasDepth => false;

        public bool HasPoints => true;

        public IEnumerable<IRulesPenalization> Penalizations => new IRulesPenalization[] {
            AidaPenalization.EarlyStart,
            AidaPenalization.LateStart,
            AidaPenalization.Blackout,
            AidaPenalization.SurfaceProtocol,
            AidaPenalization.SupportiveTouch,
            AidaPenalization.Equipment,
            AidaPenalization.MissedStart,
        };

        public IComparer<ICombinedResult> ResultsComparer => new CombinedResultCompositeComparer()
            .Descending(CombinedResultsComparers.FinalPoints)
            .Ascending(CombinedResultsComparers.DiffDuration);

        public ICalculation PointsCalculation => Calculation.Multiply(Calculation.Constant(0.2), Calculation.RealizedDuration);

        public ICalculation ShortCalculation => Calculation.Ceiling(Calculation.Multiply(Calculation.Constant(0.2), Calculation.Minus(Calculation.AnnouncedDuration, Calculation.RealizedDuration)));

        public PerformanceComponent PrimaryComponent => PerformanceComponent.Duration;

        public PerformanceComponent PenalizationsTarget => PerformanceComponent.Points;

        public Penalization BuildShortPenalization(IPerformance announcement, IPerformance result)
        {
            return ShortPenalizationCalculator.Build(announcement, result, this);
        }

        public double GetPoints(IPerformance result)
        {
            return PointsCalculator.GetPoints(result, PointsCalculation);
        }
    }

    public class RulesAidaDyn : IRules
    {
        public string Name => "AIDA_DYN";

        public bool HasDuration => false;

        public bool HasDistance => true;

        public bool HasDepth => false;

        public bool HasPoints => true;

        public IEnumerable<IRulesPenalization> Penalizations => new IRulesPenalization[] {
            AidaPenalization.EarlyStart,
            AidaPenalization.LateStart,
            AidaPenalization.ExitHelp,
            AidaPenalization.NoWall,
            AidaPenalization.Blackout,
            AidaPenalization.SurfaceProtocol,
            AidaPenalization.SupportiveTouch,
            AidaPenalization.Equipment,
            AidaPenalization.MissedStart,
            AidaPenalization.WrongTurn,
        };

        public IComparer<ICombinedResult> ResultsComparer => new CombinedResultCompositeComparer()
            .Descending(CombinedResultsComparers.FinalPoints)
            .Ascending(CombinedResultsComparers.DiffDistance);

        public PerformanceComponent PrimaryComponent => PerformanceComponent.Distance;

        public PerformanceComponent PenalizationsTarget => PerformanceComponent.Points;

        public ICalculation PointsCalculation => Calculation.Multiply(Calculation.Constant(0.5), Calculation.RealizedDistance);

        public ICalculation ShortCalculation => Calculation.Multiply(Calculation.Constant(0.5), Calculation.Minus(Calculation.AnnouncedDistance, Calculation.RealizedDistance));

        public Penalization BuildShortPenalization(IPerformance announcement, IPerformance result)
        {
            return ShortPenalizationCalculator.Build(announcement, result, this);
        }

        public double GetPoints(IPerformance result)
        {
            return PointsCalculator.GetPoints(result, PointsCalculation);
        }
    }

    public class RulesAidaCwt : IRules
    {
        public string Name => "AIDA_CWT";

        public bool HasDuration => false;

        public bool HasDistance => false;

        public bool HasDepth => true;

        public bool HasPoints => true;

        public IEnumerable<IRulesPenalization> Penalizations => new IRulesPenalization[] {
            AidaPenalization.EarlyStart,
            AidaPenalization.LateStart,
            AidaPenalization.Lanyard,
            AidaPenalization.GrabLine,
            AidaPenalization.Blackout,
            AidaPenalization.SurfaceProtocol,
            AidaPenalization.SupportiveTouch,
            AidaPenalization.Equipment,
            AidaPenalization.MissedStart,
            AidaPenalization.WrongTurn,
        };

        public IComparer<ICombinedResult> ResultsComparer => new CombinedResultCompositeComparer()
            .Descending(CombinedResultsComparers.FinalPoints)
            .Ascending(CombinedResultsComparers.DiffDepth);

        public PerformanceComponent PrimaryComponent => PerformanceComponent.Depth;

        public PerformanceComponent PenalizationsTarget => PerformanceComponent.Points;

        public ICalculation PointsCalculation => Calculation.RealizedDepth;

        public ICalculation ShortCalculation => Calculation.Minus(Calculation.AnnouncedDepth, Calculation.RealizedDepth);

        public Penalization BuildShortPenalization(IPerformance announcement, IPerformance result)
        {
            return ShortPenalizationCalculator.Build(announcement, result, this);
        }

        public double GetPoints(IPerformance result)
        {
            return PointsCalculator.GetPoints(result, PointsCalculation);
        }
    }

    public class AidaPenalization : IRulesPenalization
    {
        public static AidaPenalization EarlyStart = new AidaPenalization("EarlyStart", "Early start", "Early", "Time (seconds)", "s",
            Calculation.Ceiling(Calculation.Multiply(Calculation.Constant(0.2), Calculation.Input)));
        public static AidaPenalization LateStart = new AidaPenalization("LateStart", "Late start", "Late", "Time (seconds)", "s",
            Calculation.Ceiling(Calculation.Multiply(Calculation.Constant(0.2), Calculation.Input)));
        public static AidaPenalization NoWall = new AidaPenalization("NoWall", "Wrong turn <1m", "Turn", "Count", "x", 5);
        public static AidaPenalization ExitHelp = new AidaPenalization("ExitHelp", "Push/pull on exit", "Exit", 5);
        public static AidaPenalization NoTag = new AidaPenalization("NoTag", "No tag delivered", "Tag", 1);
        public static AidaPenalization GrabLine = new AidaPenalization("GrabLine", "Grab line", "Grab", "Count", "x", 5);
        public static AidaPenalization Lanyard = new AidaPenalization("Lanyard", "Removed lanyard", "Lanyard", 10);
        public static AidaPenalization Blackout = new AidaPenalization("Blackout", "Blackout", "BO", CardResult.Red);
        public static AidaPenalization SurfaceProtocol = new AidaPenalization("SurfaceProtocol", "Surface protocol", "SP", CardResult.Red);
        public static AidaPenalization SupportiveTouch = new AidaPenalization("SupportiveTouch", "Supportive touch", "Touch", CardResult.Red);
        public static AidaPenalization Equipment = new AidaPenalization("Equipment", "Forbidden equipment", "Equipment", CardResult.Red);
        public static AidaPenalization MissedStart = new AidaPenalization("MissedStart", "Missed start", "DNS", CardResult.DidNotStart);
        public static AidaPenalization WrongTurn = new AidaPenalization("WrongTurn", "Wrong turn >1m", "Turn", CardResult.Red);

        private string id, reason, shortReason, inputName, inputUnit;
        private ICalculation penaltyCalculation;
        private CardResult cardResult;

        public AidaPenalization(string id, string reason, string shortReason, double fixedPoints)
            : this(id, reason, shortReason, null, null, Calculation.Constant(fixedPoints))
        {
        }

        public AidaPenalization(string id, string reason, string shortReason, CardResult cardResult)
        {
            this.id = id;
            this.reason = reason;
            this.shortReason = shortReason;
            this.inputName = null;
            this.inputUnit = null;
            this.penaltyCalculation = null;
            this.cardResult = cardResult;
        }

        public AidaPenalization(string id, string reason, string shortReason, string inputName, string inputUnit, double fixedPoints)
            : this(id, reason, shortReason, inputName, inputUnit, Calculation.Multiply(Calculation.Constant(fixedPoints), Calculation.Input))
        {
        }

        public AidaPenalization(string id, string reason, string shortReason, string inputName, string inputUnit, ICalculation penaltyCalculation)
        {
            this.id = id;
            this.reason = reason;
            this.shortReason = shortReason;
            this.inputName = inputName;
            this.inputUnit = inputUnit;
            this.penaltyCalculation = penaltyCalculation;
            this.cardResult = CardResult.Yellow;
        }

        public string Id => id;

        public string Reason => reason;

        public string ShortReason => shortReason;

        public bool HasInput => inputName != null;

        public string InputName => inputName ?? "";

        public string InputUnit => inputUnit ?? "";

        public CardResult CardResult => cardResult;

        public ICalculation PenaltyCalculation => penaltyCalculation;

        public Penalization BuildPenalization(double input, Performance result)
        {
            return new Penalization
            {
                PenalizationId = id,
                Reason = reason,
                ShortReason = shortReason,
                RuleInput = inputName == null ? null : (double?)input,
                Performance = new Performance
                {
                    Points = penaltyCalculation.Evaluate(new CalculationVariables(input, null, result))
                }
            };
        }
    }

    public class RulesCmasSta : IRules
    {
        public string Name => "CMAS_STA";

        public bool HasDuration => true;

        public bool HasDistance => false;

        public bool HasDepth => false;

        public bool HasPoints => false;

        public IEnumerable<IRulesPenalization> Penalizations => new IRulesPenalization[] {
            CmasPenalization.Blackout,
            CmasPenalization.SurfaceProtocol,
            CmasPenalization.MissedStart,
        };

        public IComparer<ICombinedResult> ResultsComparer => new CombinedResultCompositeComparer()
            .Descending(CombinedResultsComparers.FinalDuration)
            .Ascending(CombinedResultsComparers.DiffDuration);

        public PerformanceComponent PrimaryComponent => PerformanceComponent.Distance;

        public PerformanceComponent PenalizationsTarget => PerformanceComponent.Distance;

        public ICalculation PointsCalculation => null;

        public ICalculation ShortCalculation => Calculation.Minus(Calculation.AnnouncedDuration, Calculation.RealizedDuration);

        public Penalization BuildShortPenalization(IPerformance announcement, IPerformance result)
        {
            return ShortPenalizationCalculator.Build(announcement, result, this);
        }

        public double GetPoints(IPerformance result)
        {
            throw new NotSupportedException();
        }
    }

    public class RulesCmasDyn : IRules
    {
        public string Name => "CMAS_DYN";

        public bool HasDuration => false;

        public bool HasDistance => true;

        public bool HasDepth => false;

        public bool HasPoints => false;

        public IEnumerable<IRulesPenalization> Penalizations => new IRulesPenalization[] {
            CmasPenalization.Blackout,
            CmasPenalization.SurfaceProtocol,
            CmasPenalization.MissedStart,
            CmasPenalization.Lane
        };

        public IComparer<ICombinedResult> ResultsComparer => new CombinedResultCompositeComparer()
            .Descending(CombinedResultsComparers.FinalDistance)
            .Ascending(CombinedResultsComparers.DiffDistance);

        public PerformanceComponent PrimaryComponent => PerformanceComponent.Distance;

        public PerformanceComponent PenalizationsTarget => PerformanceComponent.Distance;

        public ICalculation PointsCalculation => null;

        public ICalculation ShortCalculation => Calculation.Plus(Calculation.Constant(5), Calculation.Minus(Calculation.AnnouncedDistance, Calculation.RealizedDistance));

        public Penalization BuildShortPenalization(IPerformance announcement, IPerformance result)
        {
            return ShortPenalizationCalculator.Build(announcement, result, this);
        }

        public double GetPoints(IPerformance result)
        {
            throw new NotSupportedException();
        }
    }

    public class RulesCmasCwt : IRules
    {
        public string Name => "CMAS_CWT";

        public bool HasDuration => true;

        public bool HasDistance => false;

        public bool HasDepth => true;

        public bool HasPoints => false;

        public IEnumerable<IRulesPenalization> Penalizations => new IRulesPenalization[] {
            CmasPenalization.Blackout,
            CmasPenalization.SurfaceProtocol,
            CmasPenalization.MissedStart,
            CmasPenalization.NoTag,
            CmasPenalization.Grab,
        };

        public IComparer<ICombinedResult> ResultsComparer => new CombinedResultCompositeComparer()
            .Descending(CombinedResultsComparers.FinalDepth)
            .Ascending(CombinedResultsComparers.DiffDepth)
            .Ascending(CombinedResultsComparers.DiffDuration);

        public PerformanceComponent PrimaryComponent => PerformanceComponent.Depth;

        public PerformanceComponent PenalizationsTarget => PerformanceComponent.Depth;

        public ICalculation PointsCalculation => null;

        public ICalculation ShortCalculation => Calculation.Plus(Calculation.Constant(5), Calculation.Minus(Calculation.AnnouncedDistance, Calculation.RealizedDistance));

        public Penalization BuildShortPenalization(IPerformance announcement, IPerformance result)
        {
            return ShortPenalizationCalculator.Build(announcement, result, this);
        }

        public double GetPoints(IPerformance result)
        {
            throw new NotSupportedException();
        }
    }

    public class RulesCmasJumpBlue : IRules
    {
        public string Name => "CMAS_JB";

        public bool HasDuration => false;

        public bool HasDistance => true;

        public bool HasDepth => false;

        public bool HasPoints => false;

        public IEnumerable<IRulesPenalization> Penalizations => new IRulesPenalization[] {
            CmasPenalization.Blackout,
            CmasPenalization.SurfaceProtocol,
            CmasPenalization.MissedStart,
            CmasPenalization.Corner,
            CmasPenalization.NoDisc,
            CmasPenalization.BadMarker,
        };

        public IComparer<ICombinedResult> ResultsComparer => new CombinedResultCompositeComparer()
            .Descending(CombinedResultsComparers.FinalDistance)
            .Ascending(CombinedResultsComparers.DiffDistance);

        public PerformanceComponent PrimaryComponent => PerformanceComponent.Distance;

        public PerformanceComponent PenalizationsTarget => PerformanceComponent.Distance;

        public ICalculation PointsCalculation => null;

        public ICalculation ShortCalculation => Calculation.Plus(Calculation.Constant(5), Calculation.Minus(Calculation.AnnouncedDistance, Calculation.RealizedDistance));

        public Penalization BuildShortPenalization(IPerformance announcement, IPerformance result)
        {
            return ShortPenalizationCalculator.Build(announcement, result, this);
        }

        public double GetPoints(IPerformance result)
        {
            throw new NotSupportedException();
        }
    }

    public class CmasPenalization : IRulesPenalization
    {
        public static CmasPenalization Blackout = new CmasPenalization("Blackout", "Blackout", "BO", CardResult.Red);
        public static CmasPenalization SurfaceProtocol = new CmasPenalization("SurfaceProtocol", "Surface protocol", "SP", CardResult.Red);
        public static CmasPenalization MissedStart = new CmasPenalization("MissedStart", "Missed start", "DNS", CardResult.DidNotStart);
        public static CmasPenalization NoDisc = new CmasPenalization("NoDisc", "Not touched disc", "Disc", CardResult.Red);
        public static CmasPenalization Corner = new CmasPenalization("Corner", "Cut corner", "Corner", CardResult.Red);
        public static CmasPenalization Lane = new CmasPenalization("Lane", "Out of lane", "Lane", "Count", "x", PerformanceComponent.Distance, 5);
        public static CmasPenalization NoTag = new CmasPenalization("NoTag", "No tag delivered", "Tag", null, null, PerformanceComponent.Depth, 5);
        public static CmasPenalization Grab = new CmasPenalization("Grab", "Grab line", "Grab", "Count", "x", PerformanceComponent.Depth, 5);
        public static CmasPenalization BadMarker = new CmasPenalization("BadMarker", "No tag secured", "Tag", null, null, PerformanceComponent.Distance, 5);

        private readonly string id, reason, shortReason, inputName, inputUnit;
        private readonly CardResult cardResult;
        private readonly PerformanceComponent component;
        private readonly ICalculation calculation;

        public CmasPenalization(string id, string reason, string shortReason, CardResult cardResult)
        {
            this.id = id;
            this.reason = reason;
            this.shortReason = shortReason;
            this.cardResult = cardResult;
        }

        public CmasPenalization(string id, string reason, string shortReason, string inputName, string inputUnit, PerformanceComponent component, double value)
        {
            this.id = id;
            this.reason = reason;
            this.shortReason = shortReason;
            this.cardResult = CardResult.Yellow;
            this.inputName = inputName;
            this.inputUnit = inputUnit;
            this.component = component;
            if (inputName == null)
            {
                this.calculation = Calculation.Constant(value);
            }
            else
            {
                this.calculation = Calculation.Multiply(Calculation.Constant(value), Calculation.Input);
            }
        }

        public string Id => id;

        public string Reason => reason;

        public string ShortReason => shortReason;

        public bool HasInput => inputName != null;

        public string InputName => inputName ?? "";

        public string InputUnit => inputUnit ?? "";

        public CardResult CardResult => cardResult;

        public ICalculation PenaltyCalculation => calculation;

        public Penalization BuildPenalization(double input, Performance result)
        {
            Penalization penalization = new Penalization
            {
                PenalizationId = id,
                Reason = reason,
                ShortReason = shortReason,
                IsShortPerformance = false,
                RuleInput = HasInput ? input : (double?)null,
                Performance = new Performance()
            };
            if (calculation != null)
            {
                component.Modify(penalization.Performance, calculation.Evaluate(new CalculationVariables(input, null, result)));
            }
            return penalization;
        }
    }

    public class RulesUnknown : IRules
    {
        public static readonly IRules Default = new RulesUnknown();

        public string Name => "?";

        public bool HasDuration => true;

        public bool HasDistance => true;

        public bool HasDepth => true;

        public bool HasPoints => false;

        public IEnumerable<IRulesPenalization> Penalizations => new IRulesPenalization[0];

        public IComparer<ICombinedResult> ResultsComparer => new CombinedResultCompositeComparer();

        public PerformanceComponent PrimaryComponent => PerformanceComponent.None;

        public PerformanceComponent PenalizationsTarget => PerformanceComponent.None;

        public ICalculation PointsCalculation => null;

        public ICalculation ShortCalculation => null;

        public Penalization BuildShortPenalization(IPerformance announcement, IPerformance realized)
        {
            return null;
        }

        public double GetPoints(IPerformance result)
        {
            throw new NotSupportedException();
        }
    }
}