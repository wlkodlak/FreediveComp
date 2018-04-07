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
        void PostAthleteResult(string raceId, string athleteId, string authenticationToken, ReportActualResult result);
    }

    public class ApiAthlete : IApiAthlete
    {
        private readonly IRepositorySetProvider repositorySetProvider;
        private readonly IRulesRepository rulesRepository;

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

        public void PostAthleteResult(string raceId, string athleteId, string authenticationToken, ReportActualResult result)
        {
            if (string.IsNullOrEmpty(raceId)) throw new ArgumentNullException("Missing RaceId");
            if (string.IsNullOrEmpty(athleteId)) throw new ArgumentNullException("Missing AthleteId");
            var repositorySet = repositorySetProvider.GetRepositorySet(raceId);

            throw new NotImplementedException();
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