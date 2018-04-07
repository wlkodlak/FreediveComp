using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace FreediveComp.Models
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
        public bool AnnouncementsClosed { get; set; }
    }

    public class RaceSettings
    {
        public string Name { get; set; }
    }

    public class Judge
    {
        public string JudgeId { get; set; }
        public string Name { get; set; }
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
}