using FreediveComp.Models;
using System;
using System.Collections.Generic;

namespace FreediveComp.Api
{
    public interface IApiStartingList
    {
        List<StartingListEntry> GetStartingList(string raceId, string startingLaneId);
        void SetupStartingList(string raceId, string startingLaneId, List<StartingListEntry> startingList);
    }

    public class ApiStartingList : IApiStartingList
    {
        public List<StartingListEntry> GetStartingList(string raceId, string startingLaneId)
        {
            throw new NotImplementedException();
        }

        public void SetupStartingList(string raceId, string startingLaneId, List<StartingListEntry> startingList)
        {
            throw new NotImplementedException();
        }
    }
}