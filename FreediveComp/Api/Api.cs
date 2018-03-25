using FreediveComp.Models;
using System;
using System.Collections.Generic;

namespace FreediveComp.Api
{
    public interface IApiService
    {
        List<StartingLaneReportEntry> GetReportStartingLane(string raceId, string startingLaneId);
        ResultsListReport GetReportDisciplineResults(string raceId, string disciplineId);
        ResultsListReport GetReportResultsList(string raceId, string resultsListId);
    }

    public class StartingLaneReportEntry
    {
        public AthleteProfile Athlete { get; set; }
        public Discipline Discipline { get; set; }
        public Announcement Announcement { get; set; }
        public StartTimes Start { get; set; }
        public ActualResult CurrentResult { get; set; }
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

    public class Discipline
    {
        public string DisciplineId { get; set; }
        public string Name { get; set; }
    }

    public class Announcement
    {
        public TimeSpan? Duration { get; set; }
        public float? Depth { get; set; }
        public float? Distance { get; set; }
        public string ModeratorNotes { get; set; }
    }

    public class StartTimes
    {
        public string StartingLaneId { get; set; }
        public string StartingLaneLongName { get; set; }
        public DateTimeOffset? WarmUpTime { get; set; }
        public DateTimeOffset OfficialTop { get; set; }
    }

    public class ActualResult
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
        public Discipline Discipline { get; set; }
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
        public Announcement Announcement { get; set; }
        public StartTimes Start { get; set; }
        public ActualResult CurrentResult { get; set; }
        public float? FinalPoints { get; set; }
    }
}