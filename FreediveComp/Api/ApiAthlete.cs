using MilanWilczak.FreediveComp.Models;
using System;
using System.Linq;
using System.Collections.Generic;

namespace MilanWilczak.FreediveComp.Api
{
    public interface IApiAthlete
    {
        List<AthleteDto> GetAthletes(string raceId);
        AthleteDto GetAthlete(string raceId, string athleteId);
        void PostAthlete(string raceId, string athleteId, AthleteDto athlete);
        void PostAthleteResult(string raceId, string athleteId, Judge authenticatedJudge, ActualResultDto result);
    }

    public class ApiAthlete : IApiAthlete
    {
        private readonly IRepositorySetProvider repositorySetProvider;
        private readonly IRulesRepository rulesRepository;

        public ApiAthlete(IRepositorySetProvider repositorySetProvider, IRulesRepository rulesRepository)
        {
            this.repositorySetProvider = repositorySetProvider;
            this.rulesRepository = rulesRepository;
        }

        public List<AthleteDto> GetAthletes(string raceId)
        {
            if (string.IsNullOrEmpty(raceId)) throw new ArgumentNullException("Missing RaceId");
            var repositorySet = repositorySetProvider.GetRepositorySet(raceId);
            var athletes = repositorySet.Athletes.GetAthletes();

            return athletes.Select(athlete => new AthleteDto
            {
                Profile = BuildProfile(athlete),
                Announcements = athlete.Announcements.Select(BuildAnnouncement).ToList(),
                Results = athlete.ActualResults.Select(BuildActualResult).ToList()
            }).ToList();
        }

        public AthleteDto GetAthlete(string raceId, string athleteId)
        {
            if (string.IsNullOrEmpty(raceId)) throw new ArgumentNullException("Missing RaceId");
            if (string.IsNullOrEmpty(athleteId)) throw new ArgumentNullException("Missing AthleteId");
            var repositorySet = repositorySetProvider.GetRepositorySet(raceId);
            var athlete = repositorySet.Athletes.FindAthlete(athleteId);
            if (athlete == null) throw new ArgumentOutOfRangeException("Unknown AthleteId " + athleteId);

            return new AthleteDto
            {
                Profile = BuildProfile(athlete),
                Announcements = athlete.Announcements.Select(BuildAnnouncement).ToList(),
                Results = athlete.ActualResults.Select(BuildActualResult).ToList()
            };
        }

        private static AnnouncementDto BuildAnnouncement(Announcement model)
        {
            return new AnnouncementDto
            {
                DisciplineId = model.DisciplineId,
                ModeratorNotes = model.ModeratorNotes,
                Performance = BuildPerformance(model.Performance),
            };
        }

        private static PerformanceDto BuildPerformance(Performance performance)
        {
            return new PerformanceDto
            {
                Depth = performance.Depth,
                Distance = performance.Distance,
                Duration = performance.Duration,
                Points = performance.Points,
            };
        }

        private static ActualResultDto BuildActualResult(ActualResult result)
        {
            return new ActualResultDto
            {
                DisciplineId = result.DisciplineId,
                CardResult = result.CardResult.ToString(),
                FinalPerformance = BuildPerformance(result.FinalPerformance),
                Performance = BuildPerformance(result.Performance),
                JudgeComment = result.JudgeComment,
                JudgeId = result.JudgeId,
                Penalizations = result.Penalizations.Select(BuildPenalization).ToList(),
            };
        }

        private static PenalizationDto BuildPenalization(Penalization penalization)
        {
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

        public void PostAthlete(string raceId, string athleteId, AthleteDto incomingAthlete)
        {
            if (string.IsNullOrEmpty(raceId)) throw new ArgumentNullException("Missing RaceId");
            if (string.IsNullOrEmpty(athleteId)) throw new ArgumentNullException("Missing AthleteId");
            var repositorySet = repositorySetProvider.GetRepositorySet(raceId);
            bool requireProfile = false;

            var athleteModel = repositorySet.Athletes.FindAthlete(athleteId);
            if (athleteModel == null)
            {
                athleteModel = new Athlete();
                athleteModel.AthleteId = athleteId;
                athleteModel.Announcements = new List<Announcement>();
                athleteModel.ActualResults = new List<ActualResult>();
                requireProfile = true;
            }

            if (incomingAthlete.Profile == null)
            {
                if (requireProfile) throw new ArgumentNullException("Missing Profile");
            }
            else
            {
                if (string.IsNullOrEmpty(incomingAthlete.Profile.FirstName)) throw new ArgumentNullException("Missing FirstName");
                if (string.IsNullOrEmpty(incomingAthlete.Profile.Surname)) throw new ArgumentNullException("Missing Surname");
                athleteModel.FirstName = incomingAthlete.Profile.FirstName;
                athleteModel.Surname = incomingAthlete.Profile.Surname;
                athleteModel.Club = incomingAthlete.Profile.Club;
                athleteModel.CountryName = incomingAthlete.Profile.CountryName;
                athleteModel.ProfilePhotoName = incomingAthlete.Profile.ProfilePhotoName;
                athleteModel.Sex = Sex.Parse(incomingAthlete.Profile.Sex);
                athleteModel.Category = incomingAthlete.Profile.Category;
                athleteModel.ModeratorNotes = incomingAthlete.Profile.ModeratorNotes;
            }

            if (incomingAthlete.Announcements != null)
            {
                foreach (var incomingAnnouncement in incomingAthlete.Announcements)
                {
                    string disciplineId = incomingAnnouncement.DisciplineId;
                    if (string.IsNullOrEmpty(disciplineId)) throw new ArgumentNullException("Missing Announcement.DisciplineId");
                    Discipline discipline = repositorySet.Disciplines.FindDiscipline(disciplineId);
                    if (discipline == null) throw new ArgumentOutOfRangeException("Unknown Announcement.DisciplineId " + disciplineId);
                    IRules disciplineRules = rulesRepository.Get(discipline.Rules);

                    if (discipline.AnnouncementsClosed) throw new ArgumentOutOfRangeException("Discipline " + disciplineId + " already closed announcements");

                    if (incomingAnnouncement.Performance == null)
                    {
                        athleteModel.Announcements.RemoveAll(a => a.DisciplineId == disciplineId);
                    }
                    else
                    {
                        var newAnnouncement = false;
                        var announcementModel = athleteModel.Announcements.FirstOrDefault(a => a.DisciplineId == disciplineId);
                        if (announcementModel == null)
                        {
                            announcementModel = new Announcement();
                            announcementModel.DisciplineId = incomingAnnouncement.DisciplineId;
                            newAnnouncement = true;
                        }

                        if (disciplineRules.HasDuration && incomingAnnouncement.Performance.Duration == null)
                            throw new ArgumentNullException("Missing Announcement.Duration for " + disciplineId);
                        if (disciplineRules.HasDepth && incomingAnnouncement.Performance.Depth == null)
                            throw new ArgumentNullException("Missing Announcement.Depth for " + disciplineId);
                        if (disciplineRules.HasDistance && incomingAnnouncement.Performance.Distance == null)
                            throw new ArgumentNullException("Missing Announcement.Distance for " + disciplineId);

                        announcementModel.ModeratorNotes = incomingAnnouncement.ModeratorNotes;
                        announcementModel.Performance = ExtractPerformance(incomingAnnouncement.Performance);
                        if (newAnnouncement)
                        {
                            athleteModel.Announcements.Add(announcementModel);
                        }
                    }
                }
            }

            repositorySet.Athletes.SaveAthlete(athleteModel);
        }

        private static Performance ExtractPerformance(PerformanceDto performance)
        {
            return new Performance
            {
                Depth = performance.Depth,
                Distance = performance.Distance,
                Duration = performance.Duration,
                Points = performance.Points,
            };
        }

        public void PostAthleteResult(string raceId, string athleteId, Judge authenticatedJudge, ActualResultDto incomingResult)
        {
            if (string.IsNullOrEmpty(raceId)) throw new ArgumentNullException("Missing RaceId");
            if (string.IsNullOrEmpty(athleteId)) throw new ArgumentNullException("Missing AthleteId");
            if (authenticatedJudge == null) throw new ArgumentNullException("Missing AuthenticationToken");
            if (string.IsNullOrEmpty(incomingResult.DisciplineId)) throw new ArgumentNullException("Missing DisciplineId");

            var repositorySet = repositorySetProvider.GetRepositorySet(raceId);
            var athlete = repositorySet.Athletes.FindAthlete(athleteId);
            if (athlete == null) throw new ArgumentOutOfRangeException("Unknown AthleteId " + athleteId);
            var discipline = repositorySet.Disciplines.FindDiscipline(incomingResult.DisciplineId);
            if (discipline == null) throw new ArgumentOutOfRangeException("Wrong DisciplineId " + incomingResult.DisciplineId);
            var rules = rulesRepository.Get(discipline.Rules);
            if (discipline.ResultsClosed) throw new ArgumentOutOfRangeException("Discipline " + incomingResult.DisciplineId + " already closed results");

            ActualResult finalResult = new ActualResult();
            finalResult.DisciplineId = discipline.DisciplineId;
            finalResult.JudgeId = authenticatedJudge.JudgeId;
            finalResult.Penalizations = new List<Penalization>();
            finalResult.CardResult = CardResult.Parse(incomingResult.CardResult);

            if (incomingResult.JudgeOverride)
            {
                finalResult.Performance = ExtractPerformance(incomingResult.Performance);
                finalResult.FinalPerformance = ExtractPerformance(incomingResult.FinalPerformance);
                finalResult.JudgeComment = incomingResult.JudgeComment;
            }
            else
            {
                var announcement = athlete.Announcements.FirstOrDefault(a => a.DisciplineId == incomingResult.DisciplineId);
                if (announcement == null) throw new ArgumentOutOfRangeException("No announcement for " + incomingResult.DisciplineId);


                foreach (var incomingPenalization in incomingResult.Penalizations)
                {
                    if (incomingPenalization.IsShortPerformance) continue;  // we will calculate this ourselves

                    if (incomingPenalization.PenalizationId == null)        // custom penalization
                    {
                        VerifyResult(rules.HasDepth, false, incomingPenalization.Performance.Depth, "Penalization.Depth");
                        VerifyResult(rules.HasDuration, false, incomingPenalization.Performance.DurationSeconds(), "Penalization.Duration");
                        VerifyResult(rules.HasDistance, false, incomingPenalization.Performance.Distance, "Penalization.Distance");
                        VerifyResult(rules.HasPoints, false, incomingPenalization.Performance.Points, "Penalization.Points");
                        finalResult.Penalizations.Add(ExtractPenalization(incomingPenalization));
                    }
                    else
                    {
                        var rulesPenalization = rules.Penalizations.FirstOrDefault(p => p.Id == incomingPenalization.PenalizationId);
                        if (rulesPenalization == null) throw new ArgumentOutOfRangeException("Unknown Penalization.Id " + incomingPenalization.PenalizationId);
                        var finalPenalization = rulesPenalization.BuildPenalization(incomingPenalization.RuleInput ?? 0, finalResult.Performance);
                        if (finalPenalization != null)
                        {
                            finalResult.Penalizations.Add(finalPenalization);
                            finalResult.CardResult = CombineCards(finalResult.CardResult, rulesPenalization.CardResult);
                        }
                    }
                }

                bool didFinish = finalResult.CardResult == CardResult.White || finalResult.CardResult == CardResult.Yellow;
                VerifyResult(rules.HasDepth, rules.HasDepth && didFinish, incomingResult.Performance.Depth, "Performance.Depth");
                VerifyResult(rules.HasDuration, rules.HasDuration && didFinish, incomingResult.Performance.DurationSeconds(), "Performance.Duration");
                VerifyResult(rules.HasDistance, rules.HasDistance && didFinish, incomingResult.Performance.Distance, "Performance.Distance");
                finalResult.Performance = ExtractPerformance(incomingResult.Performance);
                if (!rules.HasPoints) finalResult.Performance.Points = null;
                else finalResult.Performance.Points = rules.GetPoints(incomingResult.Performance);

                var shortPenalization = rules.BuildShortPenalization(announcement.Performance, finalResult.Performance);
                if (shortPenalization != null)
                {
                    finalResult.Penalizations.Insert(0, shortPenalization);
                    finalResult.CardResult = CombineCards(finalResult.CardResult, CardResult.Yellow);
                }

                finalResult.FinalPerformance = new Performance();
                CalculateFinalPerformance(finalResult, PerformanceComponent.Duration);
                CalculateFinalPerformance(finalResult, PerformanceComponent.Depth);
                CalculateFinalPerformance(finalResult, PerformanceComponent.Distance);
                CalculateFinalPerformance(finalResult, PerformanceComponent.Points);
            }

            athlete.ActualResults.Add(finalResult);
            repositorySet.Athletes.SaveAthlete(athlete);
        }

        private static Penalization ExtractPenalization(PenalizationDto dto)
        {
            return new Penalization
            {
                IsShortPerformance = dto.IsShortPerformance,
                PenalizationId = dto.PenalizationId,
                Performance = ExtractPerformance(dto.Performance),
                Reason = dto.Reason,
                RuleInput = dto.RuleInput,
                ShortReason = dto.ShortReason,
            };
        }

        private static void VerifyResult(bool allowsComponent, bool requiresComponent, double? value, string name)
        {
            if (!allowsComponent && value != null) throw new ArgumentOutOfRangeException("Unexpected " + name);
            if (requiresComponent && value == null) throw new ArgumentNullException("Missing " + name);
            if (allowsComponent && value < 0) throw new ArgumentNullException("Negative " + name);
        }

        private static CardResult CombineCards(CardResult a, CardResult b)
        {
            if (a == CardResult.Red || b == CardResult.Red) return CardResult.Red;
            if (a == CardResult.Yellow || b == CardResult.Yellow) return CardResult.Yellow;
            if (a == CardResult.DidNotStart || b == CardResult.DidNotStart) return CardResult.DidNotStart;
            if (a == CardResult.White || b == CardResult.White) return CardResult.White;
            return CardResult.None;
        }

        private static void CalculateFinalPerformance(ActualResult result, PerformanceComponent component)
        {
            double? realized = component.Get(result.Performance);
            if (realized == null)
            {
                component.Modify(result.FinalPerformance, null);
            }
            else
            {
                double final = realized.Value;
                foreach (var penalization in result.Penalizations)
                {
                    double? minus = component.Get(penalization.Performance);
                    if (minus != null) final -= minus.Value;
                }
                if (final < 0) final = 0;
                component.Modify(result.FinalPerformance, final);
            }
        }

        public static AthleteProfile BuildProfile(Models.Athlete model)
        {
            return new AthleteProfile
            {
                AthleteId = model.AthleteId,
                Category = model.Category,
                Club = model.Club,
                CountryName = model.CountryName,
                FirstName = model.FirstName,
                Sex = model.Sex.ToString(),
                ModeratorNotes = model.ModeratorNotes,
                ProfilePhotoName = model.ProfilePhotoName,
                Surname = model.Surname,
            };
        }
    }
}