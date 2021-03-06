﻿using MilanWilczak.FreediveComp.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MilanWilczak.FreediveComp.Api
{
    public interface IApiSearch
    {
        List<RaceSearchResultDto> GetSearch(string query, DateTimeOffset? date);
    }

    public class ApiSearch : IApiSearch
    {
        private readonly SearchTokenizer tokenizer;
        private readonly IRacesIndexRepository racesIndexRepository;

        public ApiSearch(SearchTokenizer tokenizer, IRacesIndexRepository racesIndexRepository)
        {
            this.tokenizer = tokenizer;
            this.racesIndexRepository = racesIndexRepository;
        }

        public List<RaceSearchResultDto> GetSearch(string query, DateTimeOffset? date)
        {
            var tokens = tokenizer.GetTokens(query);
            var entries = racesIndexRepository.Search(tokens, date);
            return entries.Select(BuildSearchResult).ToList();
        }

        private static RaceSearchResultDto BuildSearchResult(RaceIndexEntry entry)
        {
            return new RaceSearchResultDto
            {
                RaceId = entry.RaceId,
                Name = entry.Name,
                Start = entry.Start,
                End = entry.End
            };
        }
    }
}