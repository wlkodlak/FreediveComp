using FreediveComp.Models;
using System;
using System.Collections.Generic;

namespace FreediveComp.Api
{
    public class StartingLaneReportEntry
    {
        public AthleteProfile Athlete { get; set; }
        public ReportDiscipline Discipline { get; set; }
        public ReportAnnouncement Announcement { get; set; }
        public ReportStartTimes Start { get; set; }
        public ReportActualResult CurrentResult { get; set; }
    }

    public class AthleteProfile
    {
        public string AthleteId { get; set; }
        public string FirstName { get; set; }
        public string Surname { get; set; }
        public string Club { get; set; }
        public string CountryName { get; set; }
        public string ProfilePhotoName { get; set; }
        public string Gender { get; set; }
        public string Category { get; set; }
        public string ModeratorNotes { get; set; }
    }

    public class ReportDiscipline
    {
        public string DisciplineId { get; set; }
        public string Name { get; set; }
    }

    public class ReportAnnouncement
    {
        public TimeSpan? Duration { get; set; }
        public float? Depth { get; set; }
        public float? Distance { get; set; }
        public string ModeratorNotes { get; set; }
    }

    public class ReportStartTimes
    {
        public string StartingLaneId { get; set; }
        public string StartingLaneLongName { get; set; }
        public DateTimeOffset? WarmUpTime { get; set; }
        public DateTimeOffset OfficialTop { get; set; }
    }

    public class ReportActualResult
    {
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
        public string JudgeName { get; set; }
        public string JudgeComment { get; set; }
    }

    public class ResultsListReport
    {
        public ResultsListMetadata Metadata { get; set; }
        public List<ResultsListReportEntry> Results { get; set; }
    }

    public class ResultsListMetadata
    {
        public string ResultsListId { get; set; }
        public string Title { get; set; }
        public List<ResultsListColumnMetadata> Columns { get; set; }
    }

    public class ResultsListColumnMetadata
    {
        public ReportDiscipline Discipline { get; set; }
        public string Title { get; set; }
        public bool IsSortingSource { get; set; }
    }

    public class ResultsListReportEntry
    {
        public AthleteProfile Athlete { get; set; }
        public List<ResultsListReportEntrySubresult> Subresults { get; set; }
    }

    public class ResultsListReportEntrySubresult
    {
        public ReportAnnouncement Announcement { get; set; }
        public ReportStartTimes Start { get; set; }
        public ReportActualResult CurrentResult { get; set; }
        public float? FinalPoints { get; set; }
    }

    public class RaceSetup
    {
        public RaceSettings Race { get; set; }
        public List<StartingLane> StartingLanes { get; set; }
        public List<ResultsList> ResultsLists { get; set; }
        public List<Discipline> Disciplines { get; set; }
    }

    public class StartingLane
    {
        public string StartingLaneId { get; set; }
        public string ShortName { get; set; }
        public List<StartingLane> SubLanes { get; set; }
    }

    public class Athlete
    {
        public AthleteProfile Profile { get; set; }
        public List<Announcement> Announcements { get; set; }
        public List<ActualResult> Results { get; set; }
    }

    public class AuthenticateRequest
    {
        public string DeviceId { get; set; }
        public string ConnectCode { get; set; }
    }

    public class AuthenticateResponse
    {
        public string DeviceId { get; set; }
        public string ConnectCode { get; set; }
        public string AuthenticationToken { get; set; }
        public string JudgeId { get; set; }
        public string JudgeName { get; set; }
    }

    public class AuthorizeRequest
    {
        public string ConnectCode { get; set; }
        public string JudgeId { get; set; }
        public string JudgeName { get; set; }
    }

    public class Judge
    {
        public string JudgeId { get; set; }
        public string JudgeName { get; set; }
        public List<string> DeviceIds { get; set; }
    }
}