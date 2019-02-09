using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection.Metadata;
using System.Text;

namespace BlackOfWorld.Webkit.Toolkit
{
    internal static class Tools
    {
        internal static bool enablePrint;
        internal static void ConsolePrint(string text)
        {
            if (enablePrint)
                Console.Write("[Webkit server] " + text);
        }

        internal static void SaveListToFile(string filename, object[] list)
        {
            string filePath = Path.Combine(HttpServer.mainFolderPath, filename);
            using (StreamWriter fs = File.CreateText(filePath))
            {
                fs.WriteLine("This file is part of BlackOfWorld's WebKit library, please don't touch this file");
                foreach (var line in list)
                {
                    fs.WriteLine(line.ToString());
                }
            }
        }

        internal static string[] ReadListFromFile(string filename)
        {
            string filePath = Path.Combine(HttpServer.mainFolderPath, filename);
            if (!File.Exists(filePath)) return new string[0];
            List<string> list = new List<string>();
            using (StreamReader fs = File.OpenText(filePath))
            {
                string line;
                fs.ReadLine(); // skip our signature
                while ((line = fs.ReadLine()) != null)
                {
                     list.Add(line);
                }
            }
            return list.ToArray();
        }
        internal static dynamic FireEvent(Delegate mevent, Object obj, EventArgs args)
        {
            return mevent != null ? mevent.DynamicInvoke(obj, args) : (dynamic)null;
        }
    }
}
