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
            {
                var archive = new ZipArchive(stream, ZipArchiveMode.Read);
                if (subpath.StartsWith("/"))
                {
                    subpath = subpath.Substring(1);
                }
                if (subpath.StartsWith("api")) return false;
                contents = archive.Entries.Where(e => e.FullName.StartsWith(subpath)).Select(e => new ZipFileInfo(e)).ToList();
                return contents.Any();
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
                    if (subpath.StartsWith("static/")) return false;   // don't simulate static files
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
            this.contents = new byte[zipEntry.Length];
            using (var stream = zipEntry.Open())
            {
                stream.Read(contents, 0, (int) zipEntry.Length);
            }
        }

        public long Length { get; private set; }

        public string PhysicalPath => null;

        public string Name { get; private set; }

        public DateTime LastModified { get; private set; }

        public bool IsDirectory => false;

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
            if (subpath.StartsWith("api")) return false;
            var fullpath = Path.GetFullPath(Path.Combine(this.folder, subpath));
            if (fullpath.StartsWith(this.folder)) return false;
            var directory = new DirectoryInfo(fullpath);
            if (!directory.Exists) return false;
            contents = directory.GetFiles().Select(f => new LocalFileInfo(f)).ToList();
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
            var realFile = new FileInfo(fullpath);
            if (!realFile.Exists)
            {
                if (fullpath.StartsWith("static/")) return false;   // don't simulate static files
                realFile = new FileInfo(Path.Combine(this.folder, "index.html"));
            }
            if (!realFile.Exists) return false;

            fileInfo = new LocalFileInfo(realFile);
            return true;
        }
    }

    public class LocalFileInfo : IFileInfo
    {
        private FileInfo realFile;

        public LocalFileInfo(FileInfo realFile)
        {
            this.realFile = realFile;
        }

        public long Length => realFile.Length;

        public string PhysicalPath => realFile.FullName;

        public string Name => realFile.Name;

        public DateTime LastModified => realFile.LastWriteTime;

        public bool IsDirectory => false;

        public Stream CreateReadStream()
        {
            return new FileStream(PhysicalPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        }
    }
}