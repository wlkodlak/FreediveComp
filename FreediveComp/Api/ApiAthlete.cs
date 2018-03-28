using FreediveComp.Models;
using System;

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
        public Athlete GetAthlete(string raceId, string athleteId)
        {
            throw new NotImplementedException();
        }

        public void PostAthlete(string raceId, string athleteId, Athlete athlete)
        {
            throw new NotImplementedException();
        }

        public void PostAthleteResult(string raceId, string athleteId, string authenticationToken, ReportActualResult result)
        {
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
                Gender = model.Gender == Gender.Unspecified ? "" : model.Gender.ToString(),
                ModeratorNotes = model.ModeratorNotes,
                ProfilePhotoName = model.ProfilePhotoName,
                Surname = model.Surname,
            };
        }
    }
}