using System;

namespace FreediveComp.Api
{
    public interface IApiSetup
    {
        RaceSetup GetSetup(string raceId);
        void SetupRace(string raceId, RaceSetup raceSetup);
    }

    public class ApiSetup : IApiSetup
    {
        public RaceSetup GetSetup(string raceId)
        {
            throw new NotImplementedException();
        }

        public void SetupRace(string raceId, RaceSetup raceSetup)
        {
            throw new NotImplementedException();
        }
    }
}