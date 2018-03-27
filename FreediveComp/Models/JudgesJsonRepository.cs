using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.IO;
using Newtonsoft.Json;
using System.Text;

namespace FreediveComp.Models
{
    public class JudgesJsonRepository : IJudgesRepository
    {
        private IDataFolder dataFolder;
        private bool isLoaded;
        private ReaderWriterLockSlim mutex;
        private Dictionary<string, Judge> judgesById;
        private Dictionary<string, string> authenticationMap;
        private Dictionary<string, JudgeDevice> devicesById;
        private Dictionary<string, JudgeDevice> devicesByConnectCode;
        private JsonSerializer serializer;

        public JudgesJsonRepository(IDataFolder dataFolder)
        {
            this.dataFolder = dataFolder;
            this.mutex = new ReaderWriterLockSlim();
            this.isLoaded = false;
            this.judgesById = new Dictionary<string, Judge>();
            this.devicesById = new Dictionary<string, JudgeDevice>();
            this.devicesByConnectCode = new Dictionary<string, JudgeDevice>();
            this.authenticationMap = new Dictionary<string, string>();
            this.serializer = JsonSerializer.Create();
        }

        public Judge AuthenticateJudge(string authenticationToken)
        {
            return GetData(() => AuthenticateJudgeInternal(authenticationToken));
        }

        private Judge AuthenticateJudgeInternal(string authenticationToken)
        {
            string judgeId;
            Judge judge;
            if (!authenticationMap.TryGetValue(authenticationToken, out judgeId)) return null;
            if (!judgesById.TryGetValue(judgeId, out judge)) return null;
            return judge;
        }

        public Judge FindJudge(string judgeId)
        {
            return GetData(() => FindJudgeInternal(judgeId));
        }

        private Judge FindJudgeInternal(string judgeId)
        {
            Judge judge;
            if (!judgesById.TryGetValue(judgeId, out judge)) return null;
            return judge;
        }

        public JudgeDevice FindJudgeDevice(string deviceId)
        {
            return GetData(() => FindJudgeDeviceInternal(deviceId));
        }

        private JudgeDevice FindJudgeDeviceInternal(string deviceId)
        {
            JudgeDevice device;
            devicesById.TryGetValue(deviceId, out device);
            return device;
        }

        public List<JudgeDevice> FindJudgesDevices(string judgeId)
        {
            return GetData(() => FindJudgesDevicesInternal(judgeId));
        }

        private List<JudgeDevice> FindJudgesDevicesInternal(string judgeId)
        {
            return devicesById.Values.Where(d => d.JudgeId == judgeId).ToList();
        }

        public List<Judge> GetJudges()
        {
            return GetData(() => GetJudgesInternal());
        }

        private List<Judge> GetJudgesInternal()
        {
            return judgesById.Values.ToList();
        }

        public JudgeDevice FindConnectCode(string connectCode)
        {
            return GetData(() => FindConnectCodeInternal(connectCode));
        }

        private JudgeDevice FindConnectCodeInternal(string connectCode)
        {
            JudgeDevice device;
            devicesByConnectCode.TryGetValue(connectCode, out device);
            return device;
        }

        private TResult GetData<TResult>(Func<TResult> reader)
        {
            try
            {
                mutex.EnterReadLock();
                if (isLoaded) return reader();
            }
            finally
            {
                mutex.ExitReadLock();
            }
            try
            {
                mutex.EnterWriteLock();
                EnsureDataLoaded();
                return reader();
            }
            finally
            {
                mutex.ExitWriteLock();
            }
        }

        private void EnsureDataLoaded()
        {
            if (isLoaded) return;

            JudgesData data;
            try
            {
                using (Stream stream = new MemoryStream(dataFolder.Open("judges.json"), false))
                using (TextReader textReader = new StreamReader(stream, true))
                using (JsonReader jsonReader = new JsonTextReader(textReader))
                {
                    data = serializer.Deserialize<JudgesData>(jsonReader);
                }
            }
            catch (IOException)
            {
                data = null;
            }

            isLoaded = true;
            judgesById.Clear();
            devicesById.Clear();
            authenticationMap.Clear();
            if (data != null && data.Judges != null)
            {
                foreach (var judge in data.Judges)
                {
                    judgesById[judge.JudgeId] = judge;
                }
            }
            if (data != null && data.Devices != null)
            {
                foreach (var device in data.Devices)
                {
                    devicesById[device.DeviceId] = device;
                    authenticationMap[device.AuthenticationToken] = device.JudgeId;
                }
            }
        }

        private void SaveData()
        {
            JudgesData data = new JudgesData();
            data.Judges = judgesById.Values.ToList();
            data.Devices = devicesById.Values.ToList();

            try
            {
                using (MemoryStream stream = new MemoryStream())
                {
                    using (TextWriter textWriter = new StreamWriter(stream, Encoding.UTF8))
                    using (JsonWriter jsonWriter = new JsonTextWriter(textWriter))
                    {
                        serializer.Serialize(jsonWriter, data);
                    }
                    dataFolder.Create("judges.json", stream.ToArray());
                }
            }
            catch (IOException)
            {
                data = null;
            }
        }

        public void SaveJudge(Judge judge)
        {
            try
            {
                mutex.EnterWriteLock();
                EnsureDataLoaded();
                judgesById[judge.JudgeId] = judge;
                SaveData();
            }
            finally
            {
                mutex.ExitWriteLock();
            }
        }

        public void SaveJudgeDevice(JudgeDevice device)
        {
            try
            {
                mutex.EnterWriteLock();
                EnsureDataLoaded();
                devicesById[device.DeviceId] = device;
                authenticationMap.Clear();
                devicesByConnectCode.Clear();
                foreach (JudgeDevice existing in devicesById.Values)
                {
                    authenticationMap[existing.AuthenticationToken] = existing.JudgeId;
                    if (existing.ConnectCode != null) devicesByConnectCode[existing.ConnectCode] = existing;
                }
                SaveData();
            }
            finally
            {
                mutex.ExitWriteLock();
            }
        }

        public class JudgesData
        {
            public List<Judge> Judges { get; set; }
            public List<JudgeDevice> Devices { get; set; }
        }
    }
}