using Microsoft.Owin.FileSystems;
using Microsoft.Owin.StaticFiles;
using Owin;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace MilanWilczak.FreediveComp.Controllers
{
    public static class FilesControllerExtension
    {
        public static void UseFiles(this IAppBuilder app, string folder)
        {
            if (string.IsNullOrEmpty(folder)) return;   // give up

            var applicationBase = AppDomain.CurrentDomain.SetupInformation.ApplicationBase;
            var fullpath = Path.GetFullPath(Path.Combine(applicationBase, folder));

            FileServerOptions options = new FileServerOptions();
            options.EnableDefaultFiles = true;
            options.EnableDirectoryBrowsing = false;
            options.DefaultFilesOptions.DefaultFileNames = new[] { "index.html" };
            if (fullpath.EndsWith(".zip") && File.Exists(fullpath))
                options.FileSystem = new ZipFileSystem(fullpath);
            else if (Directory.Exists(fullpath))
                options.FileSystem = new LocalFileSystem(fullpath);
            app.UseFileServer(options);
        }
    }

    public class ZipFileSystem : IFileSystem
    {
        private readonly string zipFilePath;

        public ZipFileSystem(string zipFilePath)
        {
            this.zipFilePath = zipFilePath;
        }

        public bool TryGetDirectoryContents(string subpath, out IEnumerable<IFileInfo> contents)
        {
            contents = null;
            using (var stream = new FileStream(zipFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                if (subpath.StartsWith("/"))
                {
                    subpath = subpath.Substring(1);
                }
                if (subpath.EndsWith("/"))
                {
                    subpath = subpath.Substring(0, subpath.Length - 1);
                }
                if (subpath.StartsWith("api")) return false;

                var files = new List<ZipFileInfo>();
                foreach (var entry in archive.Entries)
                {
                    if (!entry.FullName.StartsWith(subpath)) continue;  // not matched, skip it
                    if (entry.FullName == subpath) return false;    // exact match means file, not a folder
                    if (entry.FullName[subpath.Length] != '/') continue;    // name not matched completely yet (a/ vs abc/)
                    var remaining = entry.FullName.Substring(subpath.Length + 1);
                    if (string.IsNullOrEmpty(remaining)) continue;  // this is the folder being listed, skip it
                    var slashPosition = remaining.IndexOf('/');
                    if (slashPosition < 0)
                    {
                        files.Add(new ZipFileInfo(entry));
                    }
                    else if (slashPosition == remaining.Length - 1)
                    {
                        files.Add(new ZipFileInfo(entry));
                    }
                }
                if (files.Count > 0)
                {
                    contents = files;
                    return true;
                }

                var zipEntry = archive.GetEntry("index.html");
                if (zipEntry == null) return false;
                contents = new IFileInfo[] { new ZipFileInfo(zipEntry) };
                return true;
            }
        }

        public bool TryGetFileInfo(string subpath, out IFileInfo fileInfo)
        {
            fileInfo = null;
            using (var stream = new FileStream(zipFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var archive = new ZipArchive(stream, ZipArchiveMode.Read);
                if (subpath.StartsWith("/"))
                {
                    subpath = subpath.Substring(1);
                }
                if (subpath.StartsWith("api")) return false;
                var zipEntry = archive.GetEntry(subpath);
                if (zipEntry == null)
                {
                    zipEntry = archive.GetEntry("index.html");
                }
                if (zipEntry == null) return false;
                fileInfo = new ZipFileInfo(zipEntry);
                return true;
            }
        }
    }

    public class ZipFileInfo : IFileInfo
    {
        private byte[] contents;

        public ZipFileInfo(ZipArchiveEntry zipEntry)
        {
            this.Name = zipEntry.Name;
            this.Length = zipEntry.Length;
            this.LastModified = zipEntry.LastWriteTime.DateTime;
            this.IsDirectory = zipEntry.FullName.EndsWith("/");
            if (this.IsDirectory)
            {
                var fullNameSplit = zipEntry.FullName.Split('/');
                this.Name = fullNameSplit[fullNameSplit.Length - 2];
            }
            else
            {
                this.contents = new byte[zipEntry.Length];
                using (var stream = zipEntry.Open())
                {
                    stream.Read(contents, 0, (int)zipEntry.Length);
                }
            }
        }

        public long Length { get; private set; }

        public string PhysicalPath => null;

        public string Name { get; private set; }

        public DateTime LastModified { get; private set; }

        public bool IsDirectory { get; private set; }

        public Stream CreateReadStream()
        {
            return new MemoryStream(contents);
        }
    }

    public class LocalFileSystem : IFileSystem
    {
        private readonly string folder;

        public LocalFileSystem(string folder)
        {
            if (!folder.EndsWith("/"))
            {
                folder = folder + "/";
            }
            this.folder = folder;
        }

        public bool TryGetDirectoryContents(string subpath, out IEnumerable<IFileInfo> contents)
        {
            contents = null;
            if (subpath.StartsWith("/"))
            {
                subpath = subpath.Substring(1);
            }
            if (subpath.EndsWith("/"))
            {
                subpath = subpath.Substring(0, subpath.Length - 1);
            }
            if (subpath.StartsWith("api")) return false;
            var fullpath = Path.GetFullPath(Path.Combine(this.folder, subpath));
            if (fullpath.StartsWith(this.folder)) return false;
            var directory = new DirectoryInfo(fullpath);
            if (directory.Exists)
            {
                contents = directory.GetFileSystemInfos().Select(f => new LocalFileInfo(f)).ToList();
                return true;
            }

            var file = new FileInfo(fullpath);
            if (file.Exists) return false;  // it is a file, not a folder, disable listing its contents

            var indexFile = new FileInfo(Path.Combine(this.folder, "index.html"));
            if (!indexFile.Exists) return false;
            contents = new IFileInfo[] { new LocalFileInfo(indexFile) };
            return true;
        }

        public bool TryGetFileInfo(string subpath, out IFileInfo fileInfo)
        {
            fileInfo = null;
            if (subpath.StartsWith("/"))
            {
                subpath = subpath.Substring(1);
            }
            if (subpath.StartsWith("api")) return false;
            var fullpath = Path.GetFullPath(Path.Combine(this.folder, subpath));
            if (fullpath.StartsWith(this.folder)) return false;
            var directory = new DirectoryInfo(fullpath);
            var realFile = new FileInfo(fullpath);
            if (directory.Exists)
            {
                fileInfo = new LocalFileInfo(directory);
            }
            else if (realFile.Exists)
            {
                fileInfo = new LocalFileInfo(realFile);
            }
            else
            { 
                realFile = new FileInfo(Path.Combine(this.folder, "index.html"));
                if (realFile.Exists)
                {
                    fileInfo = new LocalFileInfo(realFile);
                }
            }
            return fileInfo != null;
        }
    }

    public class LocalFileInfo : IFileInfo
    {
        private FileSystemInfo realFile;

        public LocalFileInfo(FileSystemInfo realFile)
        {
            this.realFile = realFile;
        }

        public long Length
        {
            get
            {
                var fileInfo = realFile as FileInfo;
                return fileInfo == null ? 0 : fileInfo.Length;
            }
        }

        public string PhysicalPath => realFile.FullName;

        public string Name => realFile.Name;

        public DateTime LastModified => realFile.LastWriteTime;

        public bool IsDirectory => realFile is FileInfo;

        public Stream CreateReadStream()
        {
            return new FileStream(PhysicalPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }
    }
}