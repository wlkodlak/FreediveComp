using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace MilanWilczak.FreediveComp.Models
{
    public class Athlete
    {
        public string AthleteId { get; set; }
        public string FirstName { get; set; }
        public string Surname { get; set; }
        public string Club { get; set; }
        public string CountryName { get; set; }
        public string ProfilePhotoName { get; set; }
        public Sex Sex { get; set; }
        public string Category { get; set; }
        public string ModeratorNotes { get; set; }
        public List<Announcement> Announcements { get; set; }
        public List<ActualResult> ActualResults { get; set; }
    }

    public struct Sex
    {
        public static readonly Sex Unspecified = new Sex();
        public static readonly Sex Male = new Sex(1);
        public static readonly Sex Female = new Sex(2);

        private readonly int data;

        private Sex(int data)
        {
            this.data = data;
        }

        public override string ToString()
        {
            switch (data)
            {
                case 1: return "Male";
                case 2: return "Female";
                default: return "";
            }
        }

        public override int GetHashCode()
        {
            return data;
        }

        public override bool Equals(object obj)
        {
            if (obj is Sex oth)
            {
                return data == oth.data;
            }
            else
            {
                return false;
            }
        }

        public static Sex Parse(string input)
        {
            if (string.IsNullOrEmpty(input)) return Unspecified;
            switch (input)
            {
                case "Male": return Male;
                case "Female": return Female;
                default: return Unspecified;
            }
        }

        public static bool operator ==(Sex a, Sex b)
        {
            return a.data == b.data;
        }

        public static bool operator !=(Sex a, Sex b)
        {
            return a.data != b.data;
        }
    }

    public class SexJsonConverter : JsonConverter<Sex>
    {
        public override Sex ReadJson(JsonReader reader, Type objectType, Sex existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            return Sex.Parse((string)reader.Value);
        }

        public override void WriteJson(JsonWriter writer, Sex value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }
    }

    public class Announcement
    {
        public string DisciplineId { get; set; }
        public Performance Performance { get; set; }
        public string ModeratorNotes { get; set; }
    }

    public class Performance : IPerformance
    {
        public TimeSpan? Duration { get; set; }
        public double? Depth { get; set; }
        public double? Distance { get; set; }
        public double? Points { get; set; }
        [JsonIgnore]
        public double? DurationSeconds
        {
            get { return Duration == null ? (double?)null : Duration.Value.TotalSeconds; }
            set { Duration = value == null ? (TimeSpan?)null : TimeSpan.FromSeconds(value.Value); }
        }
    }

    public class ActualResult
    {
        public string DisciplineId { get; set; }
        public Performance Performance { get; set; }
        public List<Penalization> Penalizations { get; set; }
        public Performance FinalPerformance { get; set; }
        public CardResult CardResult { get; set; }
        public string JudgeId { get; set; }
        public string JudgeComment { get; set; }
    }

    public class Penalization
    {
        public string Reason { get; set; }
        public string ShortReason { get; set; }
        public string PenalizationId { get; set; }
        public bool IsShortPerformance { get; set; }
        public Performance Performance { get; set; }
        public double? RuleInput { get; set; }
    }

    public struct CardResult
    {
        public static readonly CardResult None = new CardResult();
        public static readonly CardResult White = new CardResult(1);
        public static readonly CardResult DidNotStart = new CardResult(2);
        public static readonly CardResult Yellow = new CardResult(3);
        public static readonly CardResult Red = new CardResult(4);

        private readonly int data;

        private CardResult(int data)
        {
            this.data = data;
        }

        public override string ToString()
        {
            switch (data)
            {
                case 1: return "White";
                case 2: return "DidNotStart";
                case 3: return "Yellow";
                case 4: return "Red";
                default: return "";
            }
        }

        public override int GetHashCode()
        {
            return data;
        }

        public override bool Equals(object obj)
        {
            if (obj is CardResult oth)
            {
                return data == oth.data;
            }
            else
            {
                return false;
            }
        }

        public static CardResult Parse(string input)
        {
            if (string.IsNullOrEmpty(input)) return None;
            switch (input)
            {
                case "White": return White;
                case "DidNotStart": return DidNotStart;
                case "Yellow": return Yellow;
                case "Red": return Red;
                default: return None;
            }
        }

        public static bool operator ==(CardResult a, CardResult b)
        {
            return a.data == b.data;
        }

        public static bool operator !=(CardResult a, CardResult b)
        {
            return a.data != b.data;
        }
    }

    public class CardResultJsonConverter : JsonConverter<CardResult>
    {
        public override CardResult ReadJson(JsonReader reader, Type objectType, CardResult existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            return CardResult.Parse((string)reader.Value);
        }

        public override void WriteJson(JsonWriter writer, CardResult value, JsonSerializer serializer)
        {
            writer.WriteValue(value.ToString());
        }
    }

    public class Discipline
    {
        public string DisciplineId { get; set; }
        public string ShortName { get; set; }
        public string LongName { get; set; }
        public string Rules { get; set; }
        public Sex Sex { get; set; }
        public string Category { get; set; }
        public bool AnnouncementsClosed { get; set; }
    }

    public class RaceSettings
    {
        public string Name { get; set; }
        public DateTimeOffset? Start { get; set; }
        public DateTimeOffset? End { get; set; }
    }

    public class Judge
    {
        public string JudgeId { get; set; }
        public string Name { get; set; }
        public bool IsAdmin { get; set; }
    }

    public class JudgeDevice
    {
        public string AuthenticationToken { get; set; }
        public string ConnectCode { get; set; }
        public string DeviceId { get; set; }
        public string JudgeId { get; set; }
    }

    public class StartingLane
    {
        public string StartingLaneId { get; set; }
        public string ShortName { get; set; }
        public List<StartingLane> SubLanes { get; set; }
    }

    public class StartingListEntry
    {
        public string AthleteId { get; set; }
        public string DisciplineId { get; set; }
        public string StartingLaneId { get; set; }
        public DateTimeOffset? WarmUpTime { get; set; }
        public DateTimeOffset OfficialTop { get; set; }
    }

    public class ResultsList
    {
        public string ResultsListId { get; set; }
        public string Title { get; set; }
        public List<ResultsColumn> Columns { get; set; }
    }

    public class ResultsColumn
    {
        public string Title { get; set; }
        public bool IsFinal { get; set; }
        public List<ResultsComponent> Components { get; set; }
    }

    public class ResultsComponent
    {
        public string DisciplineId { get; set; }
        public double? FinalPointsCoeficient { get; set; }
    }

    public class RaceIndexEntry
    {
        public string RaceId { get; set; }
        public string Name { get; set; }
        public HashSet<string> SearchTokens { get; set; }
        public DateTimeOffset? Start { get; set; }
        public DateTimeOffset? End { get; set; }

        public int Match(HashSet<string> search, DateTimeOffset? date)
        {
            int dateResult = MatchDate(date);
            int searchResult = MatchQuery(search);
            if (dateResult == 0 || searchResult == 0) return 0;

            return dateResult * searchResult;
        }

        private int MatchDate(DateTimeOffset? date)
        {
            if (date == null) return 100;
            int score = 0;

            if (Start == null) score += 20;
            else if (Start.Value <= date.Value) score += 50;
            else return 0;

            if (End == null) score += 20;
            else if (End.Value >= date.Value) score += 50;
            else return 0;

            return score;
        }

        private int MatchQuery(HashSet<string> search)
        {
            if (search.Count == 0) return 100;

            var set = new HashSet<string>();
            int matches, extra, unmatched;

            set.Clear();
            set.UnionWith(search);
            set.IntersectWith(SearchTokens);
            matches = set.Count;
            if (matches == 0) return 0;

            set.Clear();
            set.UnionWith(SearchTokens);
            set.ExceptWith(search);
            extra = set.Count;

            set.Clear();
            set.UnionWith(search);
            set.ExceptWith(SearchTokens);
            unmatched = set.Count;

            return Math.Max(1, 1000 * matches / (10 * matches + 2 * extra + 100 * unmatched));
        }
    }

    public struct PerformanceComponent
    {
        public static readonly PerformanceComponent None = new PerformanceComponent();
        public static readonly PerformanceComponent Distance = new PerformanceComponent(1);
        public static readonly PerformanceComponent Depth = new PerformanceComponent(2);
        public static readonly PerformanceComponent Duration = new PerformanceComponent(3);
        public static readonly PerformanceComponent Points = new PerformanceComponent(4);

        private readonly int data;

        private PerformanceComponent(int value)
        {
            this.data = value;
        }

        public static PerformanceComponent Parse(string input)
        {
            switch (input)
            {
                case "Distance": return Distance;
                case "Depth": return Depth;
                case "Duration": return Duration;
                case "Points": return Points;
                default: return None;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is PerformanceComponent oth)
            {
                return data == oth.data;
            }
            else
            {
                return false;
            }
        }

        public override int GetHashCode()
        {
            return data;
        }

        public static bool operator ==(PerformanceComponent a, PerformanceComponent b)
        {
            return a.data == b.data;
        }

        public static bool operator !=(PerformanceComponent a, PerformanceComponent b)
        {
            return a.data != b.data;
        }

        public override string ToString()
        {
            switch (data)
            {
                case 1: return "Distance";
                case 2: return "Depth";
                case 3: return "Duration";
                case 4: return "Points";
                default: return "";
            }
        }

        public double? Get(IPerformance performance)
        {
            switch (data)
            {
                case 1: return performance.Distance;
                case 2: return performance.Depth;
                case 3: return performance.DurationSeconds();
                case 4: return performance.Points;
                default: return null;
            }
        }

        public void Modify(Performance performance, double? value)
        {
            switch (data)
            {
                case 1:
                    performance.Distance = value;
                    break;
                case 2:
                    performance.Depth = value;
                    break;
                case 3:
                    performance.DurationSeconds = value;
                    break;
                case 4:
                    performance.Points = value;
                    break;
            }
        }
    }
}