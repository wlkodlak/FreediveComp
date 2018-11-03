using System;
using System.Collections.Generic;
using MilanWilczak.FreediveComp.Models;
using System.Linq;

namespace MilanWilczak.FreediveComp.Api
{
    public interface IApiReports
    {
        StartingListReport GetReportStartingList(string raceId, string startingLaneId, JudgePrincipal principal);
        ResultsListReport GetReportDisciplineResults(string raceId, string disciplineId, JudgePrincipal principal);
        ResultsListReport GetReportResultsList(string raceId, string resultsListId, JudgePrincipal principal);
    }

    public class ApiReports : IApiReports
    {
        private readonly IRepositorySetProvider repositorySetProvider;
        private readonly IStartingLanesFlatBuilder flattener;
        private readonly IRulesRepository rulesProvider;

        public ApiReports(IRepositorySetProvider repositorySetProvider, IStartingLanesFlatBuilder flattener, IRulesRepository rulesProvider)
        {
            this.repositorySetProvider = repositorySetProvider;
            this.flattener = flattener;
            this.rulesProvider = rulesProvider;
        }

        public StartingListReport GetReportStartingList(string raceId, string startingLaneId, JudgePrincipal principal)
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
            var isAuthenticated = principal != null;

            var report = new StartingListReport();
            report.Title = BuildStartingListTitle(titleSource);
            report.Entries = new List<StartingListReportEntry>();

            foreach (var startingListEntry in startingList)
            {
                Athlete athlete;
                Discipline discipline;
                StartingLaneFlat startingLane;
                Judge judge = null;
                if (!athletes.TryGetValue(startingListEntry.AthleteId, out athlete)) continue;      // athlete not found, give up
                if (!disciplines.TryGetValue(startingListEntry.DisciplineId, out discipline)) continue;     // discipline does not exist, give up
                if (!allowedStartingLanes.TryGetValue(startingListEntry.StartingLaneId, out startingLane)) continue;        // not showing this lane, skip

                Announcement announcement = athlete.Announcements.FirstOrDefault(a => a.DisciplineId == startingListEntry.DisciplineId);
                ActualResult latestResult = athlete.ActualResults.LastOrDefault(r => r.DisciplineId == startingListEntry.DisciplineId);
                if (latestResult != null)
                {
                    judges.TryGetValue(latestResult.JudgeId, out judge);
                }

                var allowAnnouncement = isAuthenticated || discipline.AnnouncementsPublic;
                var allowResult = isAuthenticated || discipline.ResultsPublic;

                StartingListReportEntry entry = new StartingListReportEntry();
                entry.Announcement = allowAnnouncement ? BuildReportAnnouncement(announcement) : null;
                entry.Athlete = ApiAthlete.BuildProfile(athlete);
                entry.CurrentResult = allowResult ? BuildReportActualResult(latestResult, judge) : null;
                entry.Discipline = BuildReportDiscipline(discipline);
                entry.Start = BuildReportStart(startingListEntry, startingLane);
                report.Entries.Add(entry);
            }

            report.Entries.Sort(CompareStartingListEntries);

            return report;
        }

        private static int CompareStartingListEntries(StartingListReportEntry x, StartingListReportEntry y)
        {
            int startTimeComparison = DateTimeOffset.Compare(x.Start.OfficialTop, y.Start.OfficialTop);
            if (startTimeComparison != 0) return startTimeComparison;
            return string.Compare(x.Start.StartingLaneLongName, y.Start.StartingLaneLongName);
        }

        private static string BuildStartingListTitle(StartingLaneFlat titleSource)
        {
            return titleSource == null ? "" : titleSource.FullName;
        }

        public ResultsListReport GetReportDisciplineResults(string raceId, string disciplineId, JudgePrincipal principal)
        {
            if (string.IsNullOrEmpty(raceId)) throw new ArgumentNullException("Missing RaceId");
            if (string.IsNullOrEmpty(disciplineId)) throw new ArgumentNullException("Missing DisciplineId");
            IRepositorySet repositorySet = repositorySetProvider.GetRepositorySet(raceId);

            var discipline = repositorySet.Disciplines.FindDiscipline(disciplineId);
            if (discipline == null) throw new ArgumentOutOfRangeException("Unknown DisciplineId " + disciplineId);

            var rules = rulesProvider.Get(discipline.Rules);
            if (rules == null) throw new ArgumentOutOfRangeException("Unknown DisciplineRules " + discipline.Rules);

            var athletes = repositorySet.Athletes.GetAthletes();
            var judges = repositorySet.Judges.GetJudges();

            var allowAnnouncement = principal != null || discipline.AnnouncementsPublic;
            var allowResult = principal != null || discipline.ResultsPublic;

            var entries = new List<ResultsListReportEntry>();
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
                if (allowAnnouncement)
                {
                    subresult.Announcement = BuildReportAnnouncement(announcement);
                }
                if (allowResult)
                {
                    subresult.CurrentResult = BuildReportActualResult(actualResult, judge);
                    subresult.FinalPoints = subresult.CurrentResult?.FinalPerformance?.Points;
                }
                entry.Subresults.Add(subresult);

                entries.Add(entry);
            }

            entries.Sort(new ResultsListReportEntryComparer(rules.ResultsComparer, 0));

            return new ResultsListReport
            {
                Metadata = BuildDisciplineReportMetadata(discipline, rules),
                Results = entries
            };
        }

        private static ResultsListMetadata BuildDisciplineReportMetadata(Discipline discipline, IRules rules)
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
                        PrimaryComponent = rules.PrimaryComponent.ToString(),
                        HasSecondaryDuration = rules.HasDistance && rules.PrimaryComponent != PerformanceComponent.Duration,
                        HasFinalPoints = rules.HasPoints,
                        Title = discipline.ShortName,
                        Discipline = BuildReportDiscipline(discipline),
                    }
                }
            };
        }

        public ResultsListReport GetReportResultsList(string raceId, string resultsListId, JudgePrincipal principal)
        {
            if (string.IsNullOrEmpty(raceId)) throw new ArgumentNullException("Missing RaceId");
            if (string.IsNullOrEmpty(resultsListId)) throw new ArgumentNullException("Missing ResultsListId");
            IRepositorySet repositorySet = repositorySetProvider.GetRepositorySet(raceId);
            var isAuthenticated = principal != null;

            var resultsList = repositorySet.ResultsLists.FindResultsList(resultsListId);
            if (resultsList == null) throw new ArgumentOutOfRangeException("Wrong ResultsListId");

            var disciplines = repositorySet.Disciplines.GetDisciplines().ToDictionary(d => d.DisciplineId);
            var columns = new List<ResultsListColumn>();
            var sortingIndex = -1;
            foreach (var columnSource in resultsList.Columns)
            {
                ResultsListColumn column = new ResultsListColumn(columnSource.Title, columnSource.IsFinal, isAuthenticated);
                foreach (var component in columnSource.Components)
                {
                    Discipline discipline;
                    if (!disciplines.TryGetValue(component.DisciplineId, out discipline)) continue;
                    IRules rules = rulesProvider.Get(discipline.Rules);
                    column.AddComponent(discipline, rules, component.FinalPointsCoeficient ?? 1.0);
                }
                if (column.IsSortingColumn) sortingIndex = columns.Count;
                columns.Add(column);
            }

            var athletes = repositorySet.Athletes.GetAthletes();
            var entries = new List<ResultsListReportEntry>();
            foreach (var athlete in athletes)
            {
                if (columns.Any(c => c.IsParticipating(athlete)))
                {
                    entries.Add(new ResultsListReportEntry
                    {
                        Athlete = ApiAthlete.BuildProfile(athlete),
                        Subresults = columns.Select(c => c.BuildSubresult(athlete)).ToList()
                    });
                }
            }

            if (sortingIndex >= 0)
            {
                entries.Sort(new ResultsListReportEntryComparer(columns[sortingIndex].SortingComparer, sortingIndex));
            }

            return new ResultsListReport
            {
                Metadata = new ResultsListMetadata
                {
                    Title = resultsList.Title,
                    ResultsListId = resultsList.ResultsListId,
                    Columns = columns.Select(c => c.Metadata).ToList()
                },
                Results = entries
            };
        }

        private static ReportAnnouncement BuildReportAnnouncement(Announcement announcement)
        {
            if (announcement == null) return null;
            return new ReportAnnouncement
            {
                Performance = BuildPerformance(announcement.Performance),
                ModeratorNotes = announcement.ModeratorNotes
            };
        }

        private static ReportActualResult BuildReportActualResult(ActualResult latestResult, Judge judge)
        {
            if (latestResult == null) return null;
            return new ReportActualResult
            {
                CardResult = latestResult.CardResult.ToString(),
                JudgeId = judge != null ? judge.JudgeId : latestResult.JudgeId,
                JudgeName = judge != null ? judge.Name : "",
                JudgeComment = BuildReportResultNote(latestResult),
                Performance = BuildPerformance(latestResult.Performance),
                FinalPerformance = BuildPerformance(latestResult.FinalPerformance),
                Penalizations = latestResult.Penalizations.Select(BuildPenalization).ToList(),
            };
        }

        private static string BuildReportResultNote(ActualResult latestResult)
        {
            if (latestResult.JudgeComment != null) return latestResult.JudgeComment;
            return string.Join(" ", latestResult.Penalizations.Select(p => p.ShortReason));
        }

        private static ReportDiscipline BuildReportDiscipline(Discipline discipline)
        {
            if (discipline == null) return null;
            return new ReportDiscipline
            {
                DisciplineId = discipline.DisciplineId,
                Name = discipline.ShortName,
                Rules = discipline.Rules
            };
        }

        private static ReportStartTimes BuildReportStart(StartingListEntry startingListEntry, StartingLaneFlat startingLane)
        {
            return new ReportStartTimes
            {
                OfficialTop = startingListEntry.OfficialTop,
                WarmUpTime = startingListEntry.WarmUpTime,
                StartingLaneId = startingLane.StartingLaneId,
                StartingLaneLongName = startingLane.FullName
            };
        }

        private static PerformanceDto BuildPerformance(Performance performance)
        {
            if (performance == null) return null;
            return new PerformanceDto
            {
                Depth = performance.Depth,
                Distance = performance.Distance,
                Duration = performance.Duration,
                Points = performance.Points,
            };
        }

        private static PenalizationDto BuildPenalization(Penalization penalization)
        {
            if (penalization == null) return null;
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

        private class ResultsListReportEntryComparer : IComparer<ResultsListReportEntry>
        {
            private readonly IComparer<ICombinedResult> comparer;
            private readonly int sortingIndex;

            public ResultsListReportEntryComparer(IComparer<ICombinedResult> comparer, int sortingIndex)
            {
                this.comparer = comparer;
                this.sortingIndex = sortingIndex;
            }

            public int Compare(ResultsListReportEntry x, ResultsListReportEntry y)
            {
                return Compare(x.Subresults[sortingIndex], y.Subresults[sortingIndex]);
            }

            private int Compare(ResultsListReportEntrySubresult x, ResultsListReportEntrySubresult y)
            {
                if (comparer == null)
                {
                    return -Comparer<double?>.Default.Compare(x.FinalPoints, y.FinalPoints);    // descending by default
                }
                else
                {
                    return comparer.Compare(x, y);
                }
            }
        }

        private class ResultsListColumn
        {
            private string title;
            private bool isSortingColumn;
            private bool isSingleDiscipline;
            private bool hasSecondaryDuration;
            private bool hasFinalPoints;
            private bool allowPrivateData;
            private bool allowAnnouncements;
            private bool allowResults;
            private PerformanceComponent primaryComponent;
            private List<ResultsListColumnComponent> components;

            public ResultsListColumn(String title, bool isSortingColumn, bool allowPrivateData)
            {
                this.title = title;
                this.isSortingColumn = isSortingColumn;
                this.allowPrivateData = allowPrivateData;
                this.allowAnnouncements = true;
                this.allowResults = true;
                this.components = new List<ResultsListColumnComponent>();
            }

            public string Title => title;
            public bool IsSortingColumn => isSortingColumn;
            public Discipline Discipline => isSingleDiscipline ? components[0].Discipline : null;
            public IComparer<ICombinedResult> SortingComparer => isSingleDiscipline ? components[0].Rules.ResultsComparer : null;

            public ResultsListColumnMetadata Metadata
            {
                get
                {
                    return new ResultsListColumnMetadata
                    {
                        Title = Title,
                        IsSortingSource = IsSortingColumn,
                        Discipline = BuildReportDiscipline(Discipline),
                        HasFinalPoints = hasFinalPoints,
                        PrimaryComponent = primaryComponent == PerformanceComponent.None ? null : primaryComponent.ToString(),
                        HasSecondaryDuration = hasSecondaryDuration
                    };
                }
            }

            public int Index { get; internal set; }

            public void AddComponent(Discipline discipline, IRules rules, double coeficient)
            {
                var component = new ResultsListColumnComponent(discipline, rules, coeficient);
                this.components.Add(component);
                isSingleDiscipline = coeficient == 1.0f && this.components.Count == 1;
                hasFinalPoints = components.All(c => c.Rules.HasPoints);    // all disciplines support points
                primaryComponent = components.Select(c => c.Rules.PrimaryComponent).FirstOrDefault();
                hasSecondaryDuration = components.Select(c => c.Rules.HasDuration && c.Rules.PrimaryComponent == PerformanceComponent.Duration).FirstOrDefault();
                bool hasPerformance = components.Select(c => c.Rules.Name).Distinct().Count() == 1;  // all disciplines have same rules
                if (!hasPerformance)
                {
                    primaryComponent = PerformanceComponent.None;
                    hasSecondaryDuration = false;
                }
                if (!discipline.AnnouncementsPublic)
                {
                    allowAnnouncements = allowPrivateData;
                }
                if (!discipline.ResultsPublic)
                {
                    allowResults = allowPrivateData;
                }
            }

            public bool IsParticipating(Athlete athlete)
            {
                foreach (var component in components)
                {
                    var disciplineId = component.Discipline.DisciplineId;
                    var hasAnnouncement = athlete.Announcements.Any(a => a.DisciplineId == disciplineId);
                    if (hasAnnouncement) return true;
                }
                return false;
            }

            public ResultsListReportEntrySubresult BuildSubresult(Models.Athlete athlete)
            {
                return isSingleDiscipline ? BuildSingleSubresult(athlete) : BuildCompositeSubresult(athlete);
            }

            private ResultsListReportEntrySubresult BuildSingleSubresult(Models.Athlete athlete)
            {
                var disciplineId = components[0].Discipline.DisciplineId;
                var announcement = athlete.Announcements.FirstOrDefault(a => a.DisciplineId == disciplineId);
                var actualResult = athlete.ActualResults.LastOrDefault(a => a.DisciplineId == disciplineId);

                var subresult = new ResultsListReportEntrySubresult();
                if (allowAnnouncements)
                {
                    subresult.Announcement = BuildReportAnnouncement(announcement);
                }
                if (allowResults)
                {
                    subresult.CurrentResult = BuildReportActualResult(actualResult, null);
                    subresult.FinalPoints = components[0].CalculateFinalPoints(actualResult);
                }
                return subresult;
            }

            private ResultsListReportEntrySubresult BuildCompositeSubresult(Athlete athlete)
            {
                double finalPoints = 0f;
                Performance combinedAnnouncement = new Performance();
                ActualResult combinedResult = new ActualResult();
                combinedResult.Performance = new Performance();
                combinedResult.FinalPerformance = new Performance();
                combinedResult.Penalizations = new List<Penalization>();
                combinedResult.CardResult = CardResult.None;

                foreach (var component in components)
                {
                    var disciplineId = component.Discipline.DisciplineId;
                    var announcement = athlete.Announcements.FirstOrDefault(a => a.DisciplineId == disciplineId);
                    var actualResult = athlete.ActualResults.LastOrDefault(a => a.DisciplineId == disciplineId);

                    if (announcement != null)
                    {
                        MergePerformance(combinedAnnouncement, announcement.Performance);
                    }
                    if (actualResult != null)
                    {
                        MergePerformance(combinedResult.Performance, actualResult.Performance);
                        MergePerformance(combinedResult.FinalPerformance, actualResult.FinalPerformance);
                        combinedResult.CardResult = CombineCards(combinedResult.CardResult, actualResult.CardResult);
                        combinedResult.Penalizations.AddRange(actualResult.Penalizations);
                        finalPoints += component.CalculateFinalPoints(actualResult) ?? 0.0;
                    }
                }
                var subresult = new ResultsListReportEntrySubresult();
                if (hasFinalPoints && allowResults)
                {
                    subresult.FinalPoints = finalPoints;
                }
                if (primaryComponent != PerformanceComponent.None)
                {
                    if (allowAnnouncements)
                    {
                        subresult.Announcement = BuildReportAnnouncement(new Announcement { Performance = combinedAnnouncement });
                    }
                    if (allowResults)
                    {
                        subresult.CurrentResult = BuildReportActualResult(combinedResult, null);
                    }
                }
                return subresult;
            }

            private static void MergePerformance(Performance target, Performance addition)
            {
                target.DurationSeconds = Sum(target.DurationSeconds, addition.DurationSeconds);
                target.Distance = Sum(target.Distance, addition.Distance);
                target.Depth = Sum(target.Depth, addition.Depth);
                target.Points = Sum(target.Points, addition.Points);
            }

            private static CardResult CombineCards(CardResult a, CardResult b)
            {
                if (a == CardResult.Red || b == CardResult.Red) return CardResult.Red;
                if (a == CardResult.Yellow || b == CardResult.Yellow) return CardResult.Yellow;
                if (a == CardResult.DidNotStart || b == CardResult.DidNotStart) return CardResult.DidNotStart;
                if (a == CardResult.White || b == CardResult.White) return CardResult.White;
                return CardResult.None;
            }
            private static double? Sum(double? a, double? b)
            {
                if (a == null) return b;
                if (b == null) return a;
                return a.Value + b.Value;
            }

        }

        private class ResultsListColumnComponent
        {
            public readonly Discipline Discipline;
            public readonly IRules Rules;
            public readonly double Coeficient;

            public ResultsListColumnComponent(Discipline discipline, IRules rules, double coeficient)
            {
                Discipline = discipline;
                Coeficient = coeficient;
                Rules = rules;

            }

            public double? CalculateFinalPoints(ActualResult result)
            {
                if (result == null || result.FinalPerformance == null || result.FinalPerformance.Points == null) return null;
                return Math.Round(result.FinalPerformance.Points.Value * Coeficient, 4);
            }
        }

    }
}