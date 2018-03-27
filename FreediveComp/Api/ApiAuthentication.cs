using FreediveComp.Models;
using System;
using System.Collections.Generic;

using ModelJudge = FreediveComp.Models.Judge;

namespace FreediveComp.Api
{
    public interface IApiAuthentication
    {
        Judge Authorize(string raceId, AuthorizeRequest authorization);
        AuthenticateResponse Authenticate(string raceId, AuthenticateRequest authentication);
        List<Judge> GetJudges(string raceId);
    }

    public class ApiAuthentication : IApiAuthentication
    {
        private readonly IRepositorySetProvider repositorySetProvider;
        private readonly Random random;

        public AuthenticateResponse Authenticate(string raceId, AuthenticateRequest authentication)
        {
            if (string.IsNullOrEmpty(raceId)) throw new ArgumentNullException("Missing RaceId");
            if (string.IsNullOrEmpty(authentication.DeviceId)) throw new ArgumentNullException("Missing DeviceId");

            var judgesRepository = repositorySetProvider.GetRepositorySet(raceId).Judges;
            var judgeDevice = judgesRepository.FindJudgeDevice(authentication.DeviceId);

            var response = new AuthenticateResponse();
            response.DeviceId = authentication.DeviceId;

            if (string.IsNullOrEmpty(authentication.ConnectCode) || judgeDevice == null || !string.Equals(judgeDevice.ConnectCode, authentication.ConnectCode))
            {
                if (judgeDevice == null)
                {
                    judgeDevice = new JudgeDevice();
                    judgeDevice.DeviceId = authentication.DeviceId;
                }
                bool needsNewConnectCode = true;
                while (needsNewConnectCode)
                {
                    judgeDevice.ConnectCode = GenerateConnectCode();
                    needsNewConnectCode = judgesRepository.FindConnectCode(judgeDevice.ConnectCode) != null;
                }
                judgesRepository.SaveJudgeDevice(judgeDevice);

                response.ConnectCode = judgeDevice.ConnectCode;
            }
            else if (judgeDevice.AuthenticationToken == null)
            {
                response.ConnectCode = judgeDevice.ConnectCode;
            }
            else
            {
                response.ConnectCode = judgeDevice.ConnectCode;
                response.AuthenticationToken = judgeDevice.AuthenticationToken;
                response.JudgeId = judgeDevice.JudgeId;
                var judge = judgesRepository.FindJudge(judgeDevice.JudgeId);
                if (judge != null)
                {
                    response.JudgeName = judge.Name;
                }
            }
            return response;
        }

        private string GenerateConnectCode()
        {
            return random.Next(100000, 999999).ToString();
        }

        public Judge Authorize(string raceId, AuthorizeRequest authorization)
        {
            if (string.IsNullOrEmpty(raceId)) throw new ArgumentNullException("Missing RaceId");
            if (string.IsNullOrEmpty(authorization.ConnectCode)) throw new ArgumentNullException("Missing ConnectCode");
            if (string.IsNullOrEmpty(authorization.JudgeId)) throw new ArgumentNullException("Missing JudgeId");
            if (string.IsNullOrEmpty(authorization.JudgeName)) throw new ArgumentNullException("Missing JudgeName");

            var judgesRepository = repositorySetProvider.GetRepositorySet(raceId).Judges;
            var judgesDevice = judgesRepository.FindConnectCode(authorization.ConnectCode);
            if (judgesDevice == null) throw new ArgumentOutOfRangeException("Unknown ConnectCode");

            ModelJudge judge = judgesRepository.FindJudge(authorization.JudgeId);
            if (judge == null)
            {
                judge = new ModelJudge();
                judge.JudgeId = authorization.JudgeId;
                judge.Name = authorization.JudgeName;
                judgesRepository.SaveJudge(judge);
            }

            judgesDevice.AuthenticationToken = GenerateAuthenticationToken();
            judgesRepository.SaveJudgeDevice(judgesDevice);

            Judge judgeDto = new Judge();
            judgeDto.JudgeId = judge.JudgeId;
            judgeDto.JudgeName = judge.Name;
            judgeDto.DeviceIds = new List<string>();
            foreach (var judgeDevice in judgesRepository.FindJudgesDevices(judge.JudgeId))
            {
                judgeDto.DeviceIds.Add(judgeDevice.DeviceId);
            }
            return judgeDto;
        }

        private string GenerateAuthenticationToken()
        {
            return Guid.NewGuid().ToString();
        }

        public List<Judge> GetJudges(string raceId)
        {
            if (string.IsNullOrEmpty(raceId)) throw new ArgumentNullException("Missing RaceId");

            var judgesRepository = repositorySetProvider.GetRepositorySet(raceId).Judges;
            var judges = new List<Judge>();
            foreach (var judge in judgesRepository.GetJudges())
            {
                var dto = new Judge();
                dto.JudgeId = judge.JudgeId;
                dto.JudgeName = judge.Name;
                dto.DeviceIds = new List<string>();
                foreach (var judgeDevice in judgesRepository.FindJudgesDevices(judge.JudgeId))
                {
                    dto.DeviceIds.Add(judgeDevice.DeviceId);
                }
            }
            return judges;
        }
    }
}