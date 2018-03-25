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
        public Gender Gender { get; set; }
        public string Category { get; set; }
        public string ModeratorNotes { get; set; }
        public List<Announcement> Announcements { get; set; }
        public List<ActualResult> ActualResults { get; set; }
    }

    public enum Gender
    {
        Unspecified,
        Male,
        Female
    }

    public class Announcement
    {
        public string AthleteId { get; set; }
        public string DisciplineId { get; set; }
        public TimeSpan? Duration { get; set; }
        public float? Depth { get; set; }
        public float? Distance { get; set; }
        public string ModeratorNotes { get; set; }
    }

    public class ActualResult
    {
        public string AthleteId { get; set; }
        public string DisciplineId { get; set; }
        public TimeSpan? Duration { get; set; }
        public float? Depth { get; set; }
        public float? Distance { get; set; }
        public float? Points { get; set; }
        public TimeSpan? DurationPenalty { get; set; }
        public float? DepthPenalty { get; set; }
        public float? DistancePenalty { get; set; }
        public float? PointsPenalty { get; set; }
        public CardResult CardResult { get; set; }
        public string JudgeId { get; set; }
        public string JudgeComment { get; set; }
    }

    public enum CardResult
    {
        None,
        DidNotStart,
        White,
        Yellow,
        Red
    }

    public class Discipline
    {
        public string DisciplineId { get; set; }
        public string Name { get; set; }
        public DisciplineRulesId Rules { get; set; }
        public float FinalPointsCoeficient { get; set; }
    }

    public enum DisciplineRulesId
    {
        Unspecified,
        AIDA_STA,
        AIDA_DYN,
        AIDA_DNF,
        AIDA_CWT,
        AIDA_CNF,
        AIDA_FIM,
        CMAS_STA,
        CMAS_DYN,
        CMAS_DYN_BI,
        CMAS_SPEED,
        CMAS_DNF,
        CMAS_CWT,
        CMAS_CWT_BI,
        CMAS_CNF,
        CMAS_FIM,
        CMAS_VWT,
        CMAS_JUMP_BLUE
    }

    public class RaceSettings
    {
        public string RaceId { get; set; }
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
        public string ParentLandId { get; set; }
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
        public float FinalPointsCoeficient { get; set; }
    }
}