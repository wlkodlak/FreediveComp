using System;
using System.Collections.Generic;
using FreediveComp.Models;
using System.Linq;

namespace FreediveComp.Api
{
    public interface IApiReports
    {
        StartingListReport GetReportStartingList(string raceId, string startingLaneId);
        ResultsListReport GetReportDisciplineResults(string raceId, string disciplineId);
        ResultsListReport GetReportResultsList(string raceId, string resultsListId);
    }

    public class ApiReports : IApiReports
    {
        private readonly IRepositorySetProvider repositorySetProvider;
        private readonly StartingLanesFlatBuilder flattener;

        public StartingListReport GetReportStartingList(string raceId, string startingLaneId)
        {
            if (string.IsNullOrEmpty(raceId)) throw new ArgumentNullException("Missing RaceId");
            IRepositorySet repositorySet = repositorySetProvider.GetRepositorySet(raceId);

            var rootStartingLanes = repositorySet.StartingLanes.GetStartingLanes();
            var titleSource = flattener.GetParent(rootStartingLanes, startingLaneId);
            var allowedStartingLanes = flattener.GetLeaves(rootStartingLanes, startingLaneId).ToDictionary(s => s.StartingLaneId);
            var athletes = repositorySet.Athletes.GetAthletes().ToDictionary(a => a.AthleteId);
            var disciplines = repositorySet.Disciplines.GetDisciplines().ToDictionary(d => d.DisciplineId);
            var startingList = repositorySet.StartingList.GetStartingList();
            var judges = repositorySet.Judges.GetJudges().ToDictionary(j => j.JudgeId);

            var report = new StartingListReport();
            report.Title = BuildStartingListTitle(titleSource);
            report.Entries = new List<StartingListReportEntry>();

            foreach (var startingListEntry in startingList)
            {
                Models.Athlete athlete;
                Discipline discipline;
                StartingLaneFlat startingLane;
                Models.Judge judge = null;
                if (!athletes.TryGetValue(startingListEntry.AthleteId, out athlete)) continue;      // athlete not found, give up
                if (!disciplines.TryGetValue(startingListEntry.DisciplineId, out discipline)) continue;     // discipline does not exist, give up
                if (!allowedStartingLanes.TryGetValue(startingListEntry.StartingLaneId, out startingLane)) continue;        // not showing this lane, skip

                Announcement announcement = athlete.Announcements.FirstOrDefault(a => a.DisciplineId == startingListEntry.DisciplineId);
                ActualResult latestResult = athlete.ActualResults.LastOrDefault(r => r.DisciplineId == startingListEntry.DisciplineId);
                if (latestResult != null)
                {
                    judges.TryGetValue(latestResult.JudgeId, out judge);
                }

                StartingListReportEntry entry = new StartingListReportEntry();
                entry.Announcement = BuildReportAnnouncement(announcement);
                entry.Athlete = ApiAthlete.BuildProfile(athlete);
                entry.CurrentResult = BuildReportActualResult(latestResult, judge);
                entry.Discipline = BuildReportDiscipline(discipline);
                entry.Start = BuildReportStart(startingListEntry, startingLane);
                report.Entries.Add(entry);
            }

            report.Entries.Sort(CompareStartingListEntries);

            return report;
        }

        private int CompareStartingListEntries(StartingListReportEntry x, StartingListReportEntry y)
        {
            int startTimeComparison = DateTimeOffset.Compare(x.Start.OfficialTop, y.Start.OfficialTop);
            if (startTimeComparison != 0) return startTimeComparison;
            return string.Compare(x.Start.StartingLaneLongName, y.Start.StartingLaneLongName);
        }

        private string BuildStartingListTitle(StartingLaneFlat titleSource)
        {
            return titleSource == null ? "" : titleSource.FullName;
        }

        public ResultsListReport GetReportDisciplineResults(string raceId, string disciplineId)
        {
            if (string.IsNullOrEmpty(raceId)) throw new ArgumentNullException("Missing RaceId");
            if (string.IsNullOrEmpty(disciplineId)) throw new ArgumentNullException("Missing DisciplineId");
            IRepositorySet repositorySet = repositorySetProvider.GetRepositorySet(raceId);

            var discipline = repositorySet.Disciplines.FindDiscipline(disciplineId);
            if (discipline == null) throw new ArgumentOutOfRangeException("Unknown DisciplineId " + disciplineId);

            var athletes = repositorySet.Athletes.GetAthletes();
            var judges = repositorySet.Judges.GetJudges();

            ResultsListReport report = new ResultsListReport();
            report.Metadata = BuildDisciplineReportMetadata(discipline);
            report.Results = new List<ResultsListReportEntry>();

            foreach (var athlete in athletes)
            {
                var announcement = athlete.Announcements.FirstOrDefault(a => a.DisciplineId == disciplineId);
                var actualResult = athlete.ActualResults.LastOrDefault(r => r.DisciplineId == disciplineId);
                if (announcement == null) continue;     // does not compete in this discipline, skip
                var judge = actualResult == null ? null : judges.FirstOrDefault(j => j.JudgeId == actualResult.JudgeId);

                var entry = new ResultsListReportEntry();
                entry.Athlete = ApiAthlete.BuildProfile(athlete);
                entry.Subresults = new List<ResultsListReportEntrySubresult>();
                var subresult = new ResultsListReportEntrySubresult();
                subresult.Announcement = BuildReportAnnouncement(announcement);
                subresult.CurrentResult = BuildReportActualResult(actualResult, judge);
                entry.Subresults.Add(subresult);
                report.Results.Add(entry);
            }

            // report.Results.Sort( ... );      // TODO

            return report;
        }

        private ResultsListMetadata BuildDisciplineReportMetadata(Discipline discipline)
        {
            return new ResultsListMetadata
            {
                DisciplineId = discipline.DisciplineId,
                Title = discipline.LongName,
                Columns = new List<ResultsListColumnMetadata>()
                {
                    new ResultsListColumnMetadata
                    {
                        IsSortingSource = true,
                        Title = discipline.ShortName,
                        Discipline = BuildReportDiscipline(discipline),
                    }
                }
            };
        }

        public ResultsListReport GetReportResultsList(string raceId, string resultsListId)
        {
            if (string.IsNullOrEmpty(raceId)) throw new ArgumentNullException("Missing RaceId");
            if (string.IsNullOrEmpty(resultsListId)) throw new ArgumentNullException("Missing ResultsListId");
            IRepositorySet repositorySet = repositorySetProvider.GetRepositorySet(raceId);

            throw new NotImplementedException();
        }

        private ReportAnnouncement BuildReportAnnouncement(Announcement announcement)
        {
            if (announcement == null) return null;
            return new ReportAnnouncement
            {
                Depth = announcement.Depth,
                Distance = announcement.Distance,
                Duration = announcement.Duration,
                ModeratorNotes = announcement.ModeratorNotes
            };
        }

        private ReportActualResult BuildReportActualResult(ActualResult latestResult, Models.Judge judge)
        {
            if (latestResult == null) return null;
            return new ReportActualResult
            {
                CardResult = latestResult.CardResult,
                Depth = latestResult.Depth,
                DepthPenalty = latestResult.DepthPenalty,
                Distance = latestResult.Distance,
                DistancePenalty = latestResult.DistancePenalty,
                Duration = latestResult.Duration,
                DurationPenalty = latestResult.DurationPenalty,
                Points = latestResult.Points,
                PointsPenalty = latestResult.PointsPenalty,
                JudgeId = judge != null ? judge.JudgeId : latestResult.JudgeId,
                JudgeName = judge != null ? judge.Name : "",
                JudgeComment = latestResult.JudgeComment,
            };
        }

        private ReportDiscipline BuildReportDiscipline(Discipline discipline)
        {
            return new ReportDiscipline
            {
                DisciplineId = discipline.DisciplineId,
                Name = discipline.ShortName
            };
        }

        private ReportStartTimes BuildReportStart(StartingListEntry startingListEntry, StartingLaneFlat startingLane)
        {
            return new ReportStartTimes
            {
                OfficialTop = startingListEntry.OfficialTop,
                WarmUpTime = startingListEntry.WarmUpTime,
                StartingLaneId = startingLane.StartingLaneId,
                StartingLaneLongName = startingLane.FullName
            };
        }
    }
}