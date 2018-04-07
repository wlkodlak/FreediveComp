using System;
using System.Collections.Generic;

namespace FreediveComp.Api
{
    public class StartingListReport
    {
        public string Title { get; set; }
        public List<StartingListReportEntry> Entries { get; set; }
    }

    public class StartingListReportEntry
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
        public string Sex { get; set; }
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
        public PerformanceDto Performance { get; set; }
        public string ModeratorNotes { get; set; }
    }

    public class PerformanceDto : Models.IPerformance
    {
        public TimeSpan? Duration { get; set; }
        public double? Depth { get; set; }
        public double? Distance { get; set; }
        public double? Points { get; set; }
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
        public PerformanceDto Performance { get; set; }
        public List<PenalizationDto> Penalizations { get; set; }
        public PerformanceDto FinalPerformance { get; set; }
        public string CardResult { get; set; }
        public string JudgeId { get; set; }
        public string JudgeName { get; set; }
        public string JudgeComment { get; set; }
    }

    public class PenalizationDto
    {
        public string Reason { get; set; }
        public string ShortReason { get; set; }
        public string PenalizationId { get; set; }
        public bool IsShortPerformance { get; set; }
        public PerformanceDto Performance { get; set; }
        public double? RuleInput { get; set; }
    }

    public class ResultsListReport
    {
        public ResultsListMetadata Metadata { get; set; }
        public List<ResultsListReportEntry> Results { get; set; }
    }

    public class ResultsListMetadata
    {
        public string DisciplineId { get; set; }
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

    public class ResultsListReportEntrySubresult : Models.ICombinedResult
    {
        public ReportAnnouncement Announcement { get; set; }
        public ReportActualResult CurrentResult { get; set; }
        public double? FinalPoints { get; set; }

        Models.IPerformance Models.ICombinedResult.Announcement => Announcement?.Performance;
        Models.IPerformance Models.ICombinedResult.Realized => CurrentResult?.Performance;
        Models.IPerformance Models.ICombinedResult.Final => CurrentResult?.FinalPerformance;
    }

    public class RaceSetupDto
    {
        public RaceSettingsDto Race { get; set; }
        public List<StartingLaneDto> StartingLanes { get; set; }
        public List<ResultsListDto> ResultsLists { get; set; }
        public List<DisciplineDto> Disciplines { get; set; }
    }

    public class RaceSettingsDto
    {
        public string RaceId { get; set; }
        public string Name { get; set; }
        public DateTimeOffset? Start { get; set; }
        public DateTimeOffset? End { get; set; }
    }

    public class StartingLaneDto
    {
        public string StartingLaneId { get; set; }
        public string ShortName { get; set; }
        public List<StartingLaneDto> SubLanes { get; set; }
    }

    public class ResultsListDto
    {
        public string ResultsListId { get; set; }
        public string Title { get; set; }
        public List<ResultsColumnDto> Columns { get; set; }
    }

    public class ResultsColumnDto
    {
        public string Title { get; set; }
        public bool IsFinal { get; set; }
        public List<ResultsComponentDto> Components { get; set; }
    }

    public class ResultsComponentDto
    {
        public string DisciplineId { get; set; }
        public double? FinalPointsCoeficient { get; set; }
    }


    public class DisciplineDto
    {
        public string DisciplineId { get; set; }
        public string ShortName { get; set; }
        public string LongName { get; set; }
        public string Rules { get; set; }
        public bool AnnouncementsClosed { get; set; }
    }

    public class AthleteDto
    {
        public AthleteProfile Profile { get; set; }
        public List<AnnouncementDto> Announcements { get; set; }
        public List<ActualResultDto> Results { get; set; }
    }

    public class AnnouncementDto
    {
        public string DisciplineId { get; set; }
        public PerformanceDto Performance { get; set; }
        public string ModeratorNotes { get; set; }
    }

    public class ActualResultDto
    {
        public string DisciplineId { get; set; }
        public PerformanceDto Performance { get; set; }
        public List<PenalizationDto> Penalizations { get; set; }
        public PerformanceDto FinalPerformance { get; set; }
        public string CardResult { get; set; }
        public string JudgeId { get; set; }
        public string JudgeComment { get; set; }
        public bool JudgeOverride { get; set; }
    }

    public class AuthenticateRequestDto
    {
        public string DeviceId { get; set; }
        public string ConnectCode { get; set; }
    }

    public class AuthenticateResponseDto
    {
        public string DeviceId { get; set; }
        public string ConnectCode { get; set; }
        public string AuthenticationToken { get; set; }
        public string JudgeId { get; set; }
        public string JudgeName { get; set; }
    }

    public class AuthorizeRequestDto
    {
        public string ConnectCode { get; set; }
        public string JudgeId { get; set; }
        public string JudgeName { get; set; }
    }

    public class JudgeDto
    {
        public string JudgeId { get; set; }
        public string JudgeName { get; set; }
        public List<string> DeviceIds { get; set; }
    }

    public class RaceSearchResultDto
    {
        public string RaceId { get; set; }
        public string Name { get; set; }
        public DateTimeOffset? Start { get; set; }
        public DateTimeOffset? End { get; set; }
    }
}