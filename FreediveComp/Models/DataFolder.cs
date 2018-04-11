using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace MilanWilczak.FreediveComp.Models
{
    public interface IDataFolder : IDisposable
    {
        IDataFolder GetSubfolder(string name);
        List<string> GetFiles();
        bool Exists(string filename);
        void Create(string filename, byte[] data);
        byte[] Open(string filename);
        void Delete(string filename);
    }

    public delegate void DataFolderChanged(string filename);

    public class DataFolderMemory : IDataFolder
    {
        private readonly Dictionary<string, DataFolderMemory> subfolders = new Dictionary<string, DataFolderMemory>();
        private readonly Dictionary<string, byte[]> files = new Dictionary<string, byte[]>();

        public void Create(string filename, byte[] data)
        {
            lock (files)
            {
                files[filename] = data;
            }
        }

        public void Delete(string filename)
        {
            lock (files)
            {
                files.Remove(filename);
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        public bool Exists(string filename)
        {
            lock (files)
            {
                return files.ContainsKey(filename);
            }
        }

        public List<string> GetFiles()
        {
            lock (files)
            {
                return files.Keys.ToList();
            }
        }

        public IDataFolder GetSubfolder(string name)
        {
            lock (subfolders)
            {
                DataFolderMemory subfolder;
                if (subfolders.TryGetValue(name, out subfolder)) return subfolder;
                subfolders[name] = subfolder = new DataFolderMemory();
                return subfolder;
            }
        }

        public byte[] Open(string filename)
        {
            lock (files)
            {
                byte[] data;
                if (files.TryGetValue(filename, out data)) return data;
                return new byte[0];
            }
        }
    }

    public class DataFolderReal : IDataFolder
    {
        public static string GetUserDataFolder => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "/MilanWilczak.FreediveComp";
        public static string GetWebDataFolder => AppDomain.CurrentDomain.BaseDirectory + "/MilanWilczak.FreediveComp";
        private DirectoryInfo folder;

        public DataFolderReal(string folder)
        {
            this.folder = new DirectoryInfo(folder);
            this.folder.Create();
        }

        private string GetFullFilePath(string filename)
        {
            return Path.Combine(folder.FullName, filename);
        }

        public void Create(string filename, byte[] data)
        {
            using (var stream = File.Open(GetFullFilePath(filename), FileMode.Create, FileAccess.Write))
            {
                stream.Write(data, 0, data.Length);
            }
        }

        public void Delete(string filename)
        {
            File.Delete(GetFullFilePath(filename));
        }

        public bool Exists(string filename)
        {
            return File.Exists(GetFullFilePath(filename));
        }

        public List<string> GetFiles()
        {
            return folder.EnumerateFiles().Select(fi => fi.Name).ToList();
        }

        public byte[] Open(string filename)
        {
            try
            {
                return File.ReadAllBytes(GetFullFilePath(filename));
            }
            catch (IOException)
            {
                return new byte[0];
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        public IDataFolder GetSubfolder(string name)
        {
            return new DataFolderReal(Path.Combine(folder.FullName, name));
        }
    }
}