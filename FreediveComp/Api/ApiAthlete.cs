using FreediveComp.Models;
using System;
using System.Linq;
using System.Collections.Generic;

namespace FreediveComp.Api
{
    public interface IApiAthlete
    {
        Athlete GetAthlete(string raceId, string athleteId);
        void PostAthlete(string raceId, string athleteId, Athlete athlete);
        void PostAthleteResult(string raceId, string athleteId, string authenticationToken, ActualResult result);
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

        public Athlete GetAthlete(string raceId, string athleteId)
        {
            if (string.IsNullOrEmpty(raceId)) throw new ArgumentNullException("Missing RaceId");
            if (string.IsNullOrEmpty(athleteId)) throw new ArgumentNullException("Missing AthleteId");
            var repositorySet = repositorySetProvider.GetRepositorySet(raceId);
            var athlete = repositorySet.Athletes.FindAthlete(athleteId);
            if (athlete == null) throw new ArgumentOutOfRangeException("Unknown AthleteId " + athleteId);

            return new Athlete
            {
                Profile = BuildProfile(athlete),
                Announcements = athlete.Announcements,
                Results = athlete.ActualResults
            };
        }

        public void PostAthlete(string raceId, string athleteId, Athlete incomingAthlete)
        {
            if (string.IsNullOrEmpty(raceId)) throw new ArgumentNullException("Missing RaceId");
            if (string.IsNullOrEmpty(athleteId)) throw new ArgumentNullException("Missing AthleteId");
            var repositorySet = repositorySetProvider.GetRepositorySet(raceId);

            var athleteModel = repositorySet.Athletes.FindAthlete(athleteId);
            if (athleteModel == null)
            {
                athleteModel = new Models.Athlete();
                athleteModel.AthleteId = athleteId;
                athleteModel.Announcements = new List<Announcement>();
                athleteModel.ActualResults = new List<ActualResult>();
            }

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
                    var announcementModel = athleteModel.Announcements.FirstOrDefault(a => a.DisciplineId == disciplineId);
                    if (announcementModel == null)
                    {
                        announcementModel = new Announcement();
                        announcementModel.DisciplineId = incomingAnnouncement.DisciplineId;
                    }

                    if (disciplineRules.HasDuration && incomingAnnouncement.Performance.Duration == null)
                        throw new ArgumentNullException("Missing Announcement.Duration for " + disciplineId);
                    if (disciplineRules.HasDepth && incomingAnnouncement.Performance.Depth == null)
                        throw new ArgumentNullException("Missing Announcement.Depth for " + disciplineId);
                    if (disciplineRules.HasDistance && incomingAnnouncement.Performance.Distance == null)
                        throw new ArgumentNullException("Missing Announcement.Distance for " + disciplineId);

                    announcementModel.ModeratorNotes = incomingAnnouncement.ModeratorNotes;
                    announcementModel.Performance = incomingAnnouncement.Performance;
                }
            }

            repositorySet.Athletes.SaveAthlete(athleteModel);
        }

        public void PostAthleteResult(string raceId, string athleteId, string authenticationToken, ActualResult incomingResult)
        {
            if (string.IsNullOrEmpty(raceId)) throw new ArgumentNullException("Missing RaceId");
            if (string.IsNullOrEmpty(athleteId)) throw new ArgumentNullException("Missing AthleteId");
            if (string.IsNullOrEmpty(authenticationToken)) throw new ArgumentNullException("Missing AuthenticationToken");
            if (string.IsNullOrEmpty(incomingResult.DisciplineId)) throw new ArgumentNullException("Missing DisciplineId");

            var repositorySet = repositorySetProvider.GetRepositorySet(raceId);
            var athlete = repositorySet.Athletes.FindAthlete(athleteId);
            if (athlete == null) throw new ArgumentOutOfRangeException("Unknown AthleteId " + athleteId);
            var judge = repositorySet.Judges.AuthenticateJudge(authenticationToken);
            if (judge == null) throw new ArgumentOutOfRangeException("Wrong AuthenticationToken");
            var discipline = repositorySet.Disciplines.FindDiscipline(incomingResult.DisciplineId);
            if (discipline == null) throw new ArgumentOutOfRangeException("Wrong DisciplineId " + incomingResult.DisciplineId);
            var rules = rulesRepository.Get(discipline.Rules);

            ActualResult finalResult;
            if (incomingResult.JudgeOverride)
            {
                finalResult = incomingResult;
                finalResult.JudgeId = judge.JudgeId;
            }
            else
            {
                var announcement = athlete.Announcements.FirstOrDefault(a => a.DisciplineId == incomingResult.DisciplineId);
                if (announcement == null) throw new ArgumentOutOfRangeException("No announcement for " + incomingResult.DisciplineId);

                finalResult = new ActualResult();
                finalResult.DisciplineId = discipline.DisciplineId;
                finalResult.JudgeId = judge.JudgeId;
                finalResult.Penalizations = new List<Penalization>();
                finalResult.CardResult = incomingResult.CardResult;

                foreach (var incomingPenalization in incomingResult.Penalizations)
                {
                    if (incomingPenalization.IsShortPerformance) continue;  // we will calculate this ourselves

                    if (incomingPenalization.PenalizationId == null)        // custom penalization
                    {
                        VerifyResult(rules.HasDepth, false, incomingPenalization.Performance.Depth, "Penalization.Depth");
                        VerifyResult(rules.HasDuration, false, incomingPenalization.Performance.DurationSeconds, "Penalization.Duration");
                        VerifyResult(rules.HasDistance, false, incomingPenalization.Performance.Distance, "Penalization.Distance");
                        VerifyResult(rules.CanConvertToPoints, false, incomingPenalization.Performance.Points, "Penalization.Points");
                        finalResult.Penalizations.Add(incomingPenalization);
                    }
                    else
                    {
                        var rulesPenalization = rules.Penalizations.FirstOrDefault(p => p.Id == incomingPenalization.PenalizationId);
                        if (rulesPenalization == null) throw new ArgumentOutOfRangeException("Unknown Penalization.Id " + incomingPenalization.PenalizationId);
                        var finalPenalization = rulesPenalization.BuildPenalization(incomingPenalization.RuleInput ?? 0, finalResult);
                        if (finalPenalization != null)
                        {
                            finalResult.Penalizations.Add(incomingPenalization);
                            finalResult.CardResult = CombineCards(finalResult.CardResult, rulesPenalization.CardResult);
                        }
                    }
                }

                bool didFinish = finalResult.CardResult == CardResult.White || finalResult.CardResult == CardResult.Yellow;
                VerifyResult(rules.HasDepth, rules.HasDepth && didFinish, incomingResult.Performance.Depth, "Performance.Depth");
                VerifyResult(rules.HasDuration, rules.HasDuration && didFinish, incomingResult.Performance.DurationSeconds, "Performance.Duration");
                VerifyResult(rules.HasDistance, rules.HasDistance && didFinish, incomingResult.Performance.Distance, "Performance.Distance");
                finalResult.Performance = incomingResult.Performance;
                if (!rules.CanConvertToPoints) finalResult.Performance.Points = null;
                else finalResult.Performance.Points = rules.GetPoints(incomingResult.Performance);

                var shortPenalization = rules.BuildShortPenalization(announcement.Performance, finalResult.Performance);
                if (shortPenalization != null)
                {
                    finalResult.Penalizations.Insert(0, shortPenalization);
                    finalResult.CardResult = CombineCards(finalResult.CardResult, CardResult.Yellow);
                }

                finalResult.FinalPerformance = new Performance();
                CalculateFinalPerformance(finalResult, f => f.DurationSeconds, (p, s) => p.DurationSeconds = s);
                CalculateFinalPerformance(finalResult, f => f.Depth, (p, s) => p.Depth = s);
                CalculateFinalPerformance(finalResult, f => f.Distance, (p, s) => p.Distance = s);
                CalculateFinalPerformance(finalResult, f => f.Points, (p, s) => p.Points = s);
            }

            athlete.ActualResults.Add(finalResult);
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

        private static void CalculateFinalPerformance(ActualResult result, Func<Performance, double?> extractor, Action<Performance, double?> setter)
        {
            double? realized = extractor(result.Performance);
            if (realized == null)
            {
                setter(result.FinalPerformance, null);
            }
            else
            {
                double final = realized.Value;
                foreach (var penalization in result.Penalizations)
                {
                    double? minus = extractor(penalization.Performance);
                    if (minus != null) final -= minus.Value;
                }
                if (final < 0) final = 0;
                setter(result.FinalPerformance, final);
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