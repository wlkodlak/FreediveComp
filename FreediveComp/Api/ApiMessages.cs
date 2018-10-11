using System;
using System.Collections.Generic;

namespace MilanWilczak.FreediveComp.Api
{
    public sealed class StartingListReport
    {
        public string Title { get; set; }
        public List<StartingListReportEntry> Entries { get; set; }
    }

    public sealed class StartingListReportEntry
    {
        public AthleteProfile Athlete { get; set; }
        public ReportDiscipline Discipline { get; set; }
        public ReportAnnouncement Announcement { get; set; }
        public ReportStartTimes Start { get; set; }
        public ReportActualResult CurrentResult { get; set; }
    }

    public sealed class StartingListEntryDto
    {
        public string AthleteId { get; set; }
        public string DisciplineId { get; set; }
        public string StartingLaneId { get; set; }
        public DateTimeOffset? WarmUpTime { get; set; }
        public DateTimeOffset OfficialTop { get; set; }
    }

    public sealed class AthleteProfile
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

    public sealed class ReportDiscipline
    {
        public string DisciplineId { get; set; }
        public string Name { get; set; }
        public string Rules { get; set; }
    }

    public sealed class ReportAnnouncement
    {
        public PerformanceDto Performance { get; set; }
        public string ModeratorNotes { get; set; }
    }

    public sealed class PerformanceDto : Models.IPerformance
    {
        public TimeSpan? Duration { get; set; }
        public double? Depth { get; set; }
        public double? Distance { get; set; }
        public double? Points { get; set; }
    }

    public sealed class ReportStartTimes
    {
        public string StartingLaneId { get; set; }
        public string StartingLaneLongName { get; set; }
        public DateTimeOffset? WarmUpTime { get; set; }
        public DateTimeOffset OfficialTop { get; set; }
    }

    public sealed class ReportActualResult
    {
        public PerformanceDto Performance { get; set; }
        public List<PenalizationDto> Penalizations { get; set; }
        public PerformanceDto FinalPerformance { get; set; }
        public string CardResult { get; set; }
        public string JudgeId { get; set; }
        public string JudgeName { get; set; }
        public string JudgeComment { get; set; }
    }

    public sealed class PenalizationDto
    {
        public string Reason { get; set; }
        public string ShortReason { get; set; }
        public string PenalizationId { get; set; }
        public bool IsShortPerformance { get; set; }
        public PerformanceDto Performance { get; set; }
        public double? RuleInput { get; set; }
    }

    public sealed class ResultsListReport
    {
        public ResultsListMetadata Metadata { get; set; }
        public List<ResultsListReportEntry> Results { get; set; }
    }

    public sealed class ResultsListMetadata
    {
        public string DisciplineId { get; set; }
        public string ResultsListId { get; set; }
        public string Title { get; set; }
        public List<ResultsListColumnMetadata> Columns { get; set; }
    }

    public sealed class ResultsListColumnMetadata
    {
        public ReportDiscipline Discipline { get; set; }
        public string Title { get; set; }
        public bool IsSortingSource { get; set; }
        public string PrimaryComponent { get; set; }
        public bool HasFinalPoints { get; set; }
    }

    public sealed class ResultsListReportEntry
    {
        public AthleteProfile Athlete { get; set; }
        public List<ResultsListReportEntrySubresult> Subresults { get; set; }
    }

    public sealed class ResultsListReportEntrySubresult : Models.ICombinedResult
    {
        public ReportAnnouncement Announcement { get; set; }
        public ReportActualResult CurrentResult { get; set; }
        public double? FinalPoints { get; set; }

        Models.IPerformance Models.ICombinedResult.Announcement => Announcement?.Performance;
        Models.IPerformance Models.ICombinedResult.Realized => CurrentResult?.Performance;
        Models.IPerformance Models.ICombinedResult.Final => CurrentResult?.FinalPerformance;
    }

    public sealed class RaceSetupDto
    {
        public RaceSettingsDto Race { get; set; }
        public List<StartingLaneDto> StartingLanes { get; set; }
        public List<ResultsListDto> ResultsLists { get; set; }
        public List<DisciplineDto> Disciplines { get; set; }
    }

    public sealed class RaceSettingsDto
    {
        public string RaceId { get; set; }
        public string Name { get; set; }
        public DateTimeOffset? Start { get; set; }
        public DateTimeOffset? End { get; set; }
    }

    public sealed class StartingLaneDto
    {
        public string StartingLaneId { get; set; }
        public string ShortName { get; set; }
        public List<StartingLaneDto> SubLanes { get; set; }
    }

    public sealed class ResultsListDto
    {
        public string ResultsListId { get; set; }
        public string Title { get; set; }
        public List<ResultsColumnDto> Columns { get; set; }
    }

    public sealed class ResultsColumnDto
    {
        public string Title { get; set; }
        public bool IsFinal { get; set; }
        public List<ResultsComponentDto> Components { get; set; }
    }

    public sealed class ResultsComponentDto
    {
        public string DisciplineId { get; set; }
        public double? FinalPointsCoeficient { get; set; }
    }


    public sealed class DisciplineDto
    {
        public string DisciplineId { get; set; }
        public string ShortName { get; set; }
        public string LongName { get; set; }
        public string Rules { get; set; }
        public string Sex { get; set; }
        public string Category { get; set; }
        public bool AnnouncementsClosed { get; set; }
        public bool ResultsClosed { get; set; }
    }

    public sealed class AthleteDto
    {
        public AthleteProfile Profile { get; set; }
        public List<AnnouncementDto> Announcements { get; set; }
        public List<ActualResultDto> Results { get; set; }
    }

    public sealed class AnnouncementDto
    {
        public string DisciplineId { get; set; }
        public PerformanceDto Performance { get; set; }
        public string ModeratorNotes { get; set; }
    }

    public sealed class ActualResultDto
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

    public sealed class AuthenticateRequestDto
    {
        public string DeviceId { get; set; }
        public string ConnectCode { get; set; }
    }

    public sealed class AuthenticateResponseDto
    {
        public string DeviceId { get; set; }
        public string ConnectCode { get; set; }
        public string AuthenticationToken { get; set; }
        public string JudgeId { get; set; }
        public string JudgeName { get; set; }
    }

    public sealed class AuthorizeRequestDto
    {
        public string ConnectCode { get; set; }
        public string JudgeId { get; set; }
        public string JudgeName { get; set; }
    }

    public sealed class JudgeDto
    {
        public string JudgeId { get; set; }
        public string JudgeName { get; set; }
        public bool? IsAdmin { get; set; }
        public List<string> DeviceIds { get; set; }
    }

    public sealed class RaceSearchResultDto
    {
        public string RaceId { get; set; }
        public string Name { get; set; }
        public DateTimeOffset? Start { get; set; }
        public DateTimeOffset? End { get; set; }
    }

    public sealed class RulesDto
    {
        public string Name { get; set; }
        public bool HasDuration { get; set; }
        public bool HasDistance { get; set; }
        public bool HasDepth { get; set; }
        public bool HasPoints { get; set; }
        public string PrimaryComponent { get; set; }
        public string PenalizationsTarget { get; set; }
        public List<RulesPenalizationDto> Penalizations { get; set; }
        public CalculationDto PointsCalculation { get; set; }
        public CalculationDto ShortCalculation { get; set; }
    }

    public sealed class RulesPenalizationDto
    {
        public string Id { get; set; }
        public string Reason { get; set; }
        public string ShortReason { get; set; }
        public bool HasInput { get; set; }
        public string InputName { get; set; }
        public string InputUnit { get; set; }
        public string CardResult { get; set; }
        public CalculationDto Calculation { get; set; }
    }

    public sealed class CalculationDto
    {
        public string Operation { get; set; }
        public double? Constant { get; set; }
        public string Variable { get; set; }
        public CalculationDto ArgumentA { get; set; }
        public CalculationDto ArgumentB { get; set; }
    }

    public sealed class GetShortPenalizationRequest
    {
        public PerformanceDto Announced { get; set; }
        public PerformanceDto Realized { get; set; }
    }

    public sealed class GetCalculatedPenalizationRequest
    {
        public string PenalizationId { get; set; }
        public double Input { get; set; }
        public PerformanceDto Realized { get; set; }
    }
}