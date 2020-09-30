using BlackOfWorld.Webkit.Toolkit;
using BlackOfWorld.Webkit.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;

namespace BlackOfWorld.Webkit.Managers
{
    internal class StaticFileManager
    {
        private Dictionary<string, byte[]> staticItems;
        private string[] staticFiles;
        private ServerConfig cfg;
        private bool started = false;
        private readonly FileSystemWatcher fileSystemWatcher = new FileSystemWatcher();
        private byte[] FindNonCached(string fileName)
        {
            foreach (var realPath in staticFiles)
            {
                var path = realPath.Remove(0, cfg.StaticFileLocation.Length);
                if (path == fileName || path == fileName.Replace('/', '\\') || path == fileName.Replace("/", ""))
                    return File.ReadAllBytes(realPath);
            }
            return new byte[0];
        }
        internal byte[] GetStaticItem(string fileName)
        {
            if (!started) return new byte[0];
            var nonCached = FindNonCached(fileName);
            if (nonCached.Length != 0) return nonCached;
            if (fileName[0] == '/') fileName = fileName.Substring(1);
            if (!staticItems.TryGetValue(fileName, out byte[] result) && !staticItems.TryGetValue(fileName.Replace('/', '\\'), out result))
                return new byte[0];
            Tools.ConsolePrint("Static file found\n");
            return result;
        }
        internal void Start(ServerConfig cfg)
        {
            if (string.IsNullOrWhiteSpace(cfg.StaticFileLocation) || !Directory.Exists(cfg.StaticFileLocation)) return;
            staticItems = new Dictionary<string, byte[]>();
            staticFiles = new string[0];
            this.cfg = cfg;
            var filesFolders = Directory.GetFiles(cfg.StaticFileLocation, "*", SearchOption.AllDirectories);
            fileSystemWatcher.Path = cfg.StaticFileLocation;
            fileSystemWatcher.Created += OnStaticFileCreate;
            fileSystemWatcher.Deleted += OnStaticFileDelete;
            fileSystemWatcher.Changed += OnStaticFileChange;
            fileSystemWatcher.Renamed += OnStaticFileRename;
            fileSystemWatcher.EnableRaisingEvents = true;
            started = true;
            if (!cfg.CacheStaticFiles)
            {
                staticFiles = filesFolders;
                return;
            }
            foreach (string filePath in filesFolders)
            {
                var fileExt = Path.GetExtension(filePath);
                string realFilePath = filePath.Remove(0, cfg.StaticFileLocation.Length);
                if (cfg.MinifyCachedStaticFiles && (fileExt == ".js" || fileExt == ".css" || fileExt == ".xml" || fileExt == ".json"))
                    staticItems.Add(realFilePath, Encoding.UTF8.GetBytes(MinifyHTML.Minify(filePath)));
                else
                    staticItems.Add(realFilePath, File.ReadAllBytes(filePath));
            }
        }

        private void OnStaticFileRename(object sender, RenamedEventArgs e)
        {
            var filename = e.FullPath.Substring(cfg.StaticFileLocation.Length);
            Tools.ConsolePrint($"Static file called \"{filename}\" got renamed! Refreshing static file manager!");
            var filePath = e.OldFullPath;
            var newFilePath = e.FullPath;
            if (cfg.CacheStaticFiles)
            {
                var fileExt = Path.GetExtension(filePath);
                string realFilePath = filePath.Remove(0, cfg.StaticFileLocation.Length);
                string realNewFilePath = newFilePath.Remove(0, cfg.StaticFileLocation.Length);
                if (cfg.MinifyCachedStaticFiles && (fileExt == ".js" || fileExt == ".css" || fileExt == ".xml" || fileExt == ".json"))
                {
                    staticItems.Remove(realFilePath);
                    staticItems.Add(realNewFilePath, Encoding.UTF8.GetBytes(MinifyHTML.Minify(filePath)));
                }
                else
                {
                    staticItems.Remove(realFilePath);
                    staticItems.Add(realNewFilePath, File.ReadAllBytes(e.FullPath));
                }
            }
            else
            {
                for (int i = 0; i < staticFiles.Length; i++)
                {
                    if (staticFiles[i] != e.OldFullPath) continue;
                    staticFiles[i] = e.FullPath;
                    break;
                }
            }
        }

        private void OnStaticFileChange(object sender, FileSystemEventArgs e)
        {
            if (!cfg.CacheStaticFiles) return;
            var filename = e.FullPath.Substring(cfg.StaticFileLocation.Length);
            Tools.ConsolePrint($"Cached static file called \"{filename}\" got changed! Refreshing static file manager!");
            var filePath = e.FullPath;
            var fileExt = Path.GetExtension(filePath);
            string realFilePath = filePath.Remove(0, cfg.StaticFileLocation.Length);
            if (cfg.MinifyCachedStaticFiles && (fileExt == ".js" || fileExt == ".css" || fileExt == ".xml" || fileExt == ".json"))
                staticItems[realFilePath] = Encoding.UTF8.GetBytes(MinifyHTML.Minify(filePath));
            else
                staticItems[realFilePath] = File.ReadAllBytes(e.FullPath);
        }

        private void OnStaticFileCreate(object sender, FileSystemEventArgs e)
        {
            var filePath = e.FullPath;
            var filename = e.FullPath.Substring(cfg.StaticFileLocation.Length);
            Tools.ConsolePrint($"New static file called \"{filename}\" added! Refreshing static file manager!");
            if (cfg.CacheStaticFiles)
            {
                var fileExt = Path.GetExtension(filePath);
                string realFilePath = filePath.Remove(0, cfg.StaticFileLocation.Length);
                if (cfg.MinifyCachedStaticFiles && (fileExt == ".js" || fileExt == ".css" || fileExt == ".xml" || fileExt == ".json"))
                    staticItems.Add(realFilePath, Encoding.UTF8.GetBytes(MinifyHTML.Minify(filePath)));
                else
                    staticItems.Add(realFilePath, File.ReadAllBytes(e.FullPath));
            }
            else
            {
                staticFiles = staticFiles.Concat(new string[] { filePath }).ToArray();
            }
        }

        private void OnStaticFileDelete(object sender, FileSystemEventArgs e)
        {
            var filename = e.FullPath.Substring(cfg.StaticFileLocation.Length);
            var nonCached = Array.Exists(staticFiles, element => element == e.FullPath);
            if (!nonCached && !staticItems.ContainsKey(filename)) return;
            Tools.ConsolePrint($"Static file \"{filename}\" got deleted! Deleting it from static file manager!");
            if (nonCached)
            {
                staticFiles = Array.FindAll(staticFiles, t => t != e.FullPath);
                return;
            }
            staticItems.Remove(filename);
        }
    }
}
