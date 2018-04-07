using FreediveComp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FreediveComp.Api
{
    public class ApiRules
    {
        private readonly IRulesRepository rulesRepository;

        public ApiRules(IRulesRepository rulesRepository)
        {
            this.rulesRepository = rulesRepository;
        }


    }
}