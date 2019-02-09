using BlackOfWorld.Webkit.Toolkit;
using BlackOfWorld.Webkit.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
namespace BlackOfWorld.Webkit.Managers
{
    internal class StaticFileManager
    {
        private Dictionary<string, byte[]> staticItems;
        private string[] staticFiles;
        private ServerConfig cfg;
        private bool started = false;
        private byte[] FindNonCached(string fileName)
        {
            foreach (var realPath in staticFiles)
            {
                var path = realPath.Remove(0, cfg.StaticFileLocation.Length);
                if (path == fileName || path == fileName.Replace('/', '\\'))
                    return File.ReadAllBytes(realPath);
            }
            return new byte[0];
        }
        internal byte[] GetStaticItem(string fileName)
        {
            if (!started) return new byte[0];
            var nonCached = FindNonCached(fileName);
            if (nonCached.Length != 0) return nonCached;
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
            started = true;
            if (cfg.StaticFileLocation == "")
                return;
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
    }
}
