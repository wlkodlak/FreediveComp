using MilanWilczak.FreediveComp.Models;
using System;
using System.Collections.Generic;

using ModelJudge = MilanWilczak.FreediveComp.Models.Judge;

namespace MilanWilczak.FreediveComp.Api
{
    public interface IApiAuthentication
    {
        JudgeDto Authorize(string raceId, AuthorizeRequestDto authorization);
        JudgeDto Unauthorize(string raceId, UnauthorizeRequestDto authorization);
        AuthenticateResponseDto Authenticate(string raceId, AuthenticateRequestDto authentication);
        List<JudgeDto> GetJudges(string raceId, JudgePrincipal principal);
        JudgeDto Verify(string raceId, JudgePrincipal principal);
    }

    public class ApiAuthentication : IApiAuthentication
    {
        private readonly IRepositorySetProvider repositorySetProvider;
        private readonly Random random;

        public ApiAuthentication(IRepositorySetProvider repositorySetProvider)
        {
            this.repositorySetProvider = repositorySetProvider;
            this.random = new Random();
        }

        public AuthenticateResponseDto Authenticate(string raceId, AuthenticateRequestDto authentication)
        {
            if (string.IsNullOrEmpty(raceId)) throw new ArgumentNullException("Missing RaceId");
            if (string.IsNullOrEmpty(authentication.DeviceId)) throw new ArgumentNullException("Missing DeviceId");

            var judgesRepository = repositorySetProvider.GetRepositorySet(raceId).Judges;
            var judgeDevice = judgesRepository.FindJudgeDevice(authentication.DeviceId);

            var response = new AuthenticateResponseDto();
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
                    judgeDevice.AuthenticationToken = null;
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

        public JudgeDto Authorize(string raceId, AuthorizeRequestDto authorization)
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

            judgesDevice.AuthenticationToken = AuthenticationToken.Generate(raceId, judge.JudgeId).ToString();
            judgesDevice.JudgeId = judge.JudgeId;
            judgesRepository.SaveJudgeDevice(judgesDevice);

            JudgeDto judgeDto = new JudgeDto();
            judgeDto.JudgeId = judge.JudgeId;
            judgeDto.JudgeName = judge.Name;
            judgeDto.IsAdmin = judge.IsAdmin;
            judgeDto.DeviceIds = new List<string>();
            foreach (var judgeDevice in judgesRepository.FindJudgesDevices(judge.JudgeId))
            {
                judgeDto.DeviceIds.Add(judgeDevice.DeviceId);
            }
            return judgeDto;
        }

        public JudgeDto Unauthorize(string raceId, UnauthorizeRequestDto authorization)
        {
            if (string.IsNullOrEmpty(raceId)) throw new ArgumentNullException("Missing RaceId");
            if (string.IsNullOrEmpty(authorization.JudgeId)) throw new ArgumentNullException("Missing JudgeId");

            var judgesRepository = repositorySetProvider.GetRepositorySet(raceId).Judges;
            var judge = judgesRepository.FindJudge(authorization.JudgeId);
            if (judge == null) throw new ArgumentOutOfRangeException("Unknown JudgeId");

            foreach (var device in judgesRepository.FindJudgesDevices(judge.JudgeId))
            {
                var shouldRemove = string.IsNullOrEmpty(authorization.DeviceId) || authorization.DeviceId == device.DeviceId;
                if (shouldRemove)
                {
                    device.AuthenticationToken = null;
                    device.JudgeId = null;
                    judgesRepository.SaveJudgeDevice(device);
                }
            }

            JudgeDto judgeDto = new JudgeDto();
            judgeDto.JudgeId = judge.JudgeId;
            judgeDto.JudgeName = judge.Name;
            judgeDto.IsAdmin = judge.IsAdmin;
            judgeDto.DeviceIds = new List<string>();
            foreach (var judgeDevice in judgesRepository.FindJudgesDevices(judge.JudgeId))
            {
                judgeDto.DeviceIds.Add(judgeDevice.DeviceId);
            }
            return judgeDto;
        }

        public List<JudgeDto> GetJudges(string raceId, JudgePrincipal principal)
        {
            if (string.IsNullOrEmpty(raceId)) throw new ArgumentNullException("Missing RaceId");

            var isAdmin = principal != null && principal.Judge.IsAdmin;
            var judgesRepository = repositorySetProvider.GetRepositorySet(raceId).Judges;
            var judges = new List<JudgeDto>();
            foreach (var judge in judgesRepository.GetJudges())
            {
                var dto = new JudgeDto();
                dto.JudgeId = judge.JudgeId;
                dto.JudgeName = judge.Name;
                dto.IsAdmin = judge.IsAdmin;
                if (isAdmin)
                {
                    dto.DeviceIds = new List<string>();
                    foreach (var judgeDevice in judgesRepository.FindJudgesDevices(judge.JudgeId))
                    {
                        dto.DeviceIds.Add(judgeDevice.DeviceId);
                    }
                }
                judges.Add(dto);
            }
            return judges;
        }

        public JudgeDto Verify(string raceId, JudgePrincipal principal)
        {
            return new JudgeDto
            {
                JudgeId = principal.Judge.JudgeId,
                JudgeName = principal.Judge.Name,
                IsAdmin = principal.Judge.IsAdmin
            };
        }
    }
}