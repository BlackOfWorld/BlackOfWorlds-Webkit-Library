using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace BlackOfWorld.Webkit.Toolkit
{
    internal class MinifyHTML
    {
        #region Minify Procedures
        private static string DoXML(string xmlString)
        {
            XmlDocument xmlDocument = new XmlDocument
            {
                PreserveWhitespace = false
            };
            xmlDocument.Load(new MemoryStream(Encoding.UTF8.GetBytes(xmlString)));
            foreach (XmlNode comment in xmlDocument.SelectNodes("//comment()"))
            {
                comment.ParentNode.RemoveChild(comment);
            }
            foreach (XmlElement el in xmlDocument.SelectNodes("descendant::*[not(*) and not(normalize-space())]"))
            {
                el.IsEmpty = true;
            }
            return xmlDocument.InnerXml;
        }
        private static string doCss(string content)
        {
            content = Regex.Replace(content, @"/\*.+?\*/", "", RegexOptions.Compiled);
            content = Regex.Replace(content, @"[a-zA-Z]+#", "#", RegexOptions.Compiled);
            content = Regex.Replace(content, @"[\n\r]+\s*", " ", RegexOptions.Compiled);
            content = Regex.Replace(content, @"\s+", " ", RegexOptions.Compiled);
            content = Regex.Replace(content, @"\s?([:,;{}])\s?", "$1", RegexOptions.Compiled);
            content = content.Replace(";}", "}");
            content = Regex.Replace(content, @"([\s:]0)(px|pt|%|em)", "$1", RegexOptions.Compiled);
            return content;
        }
        private static string doHtml(string content)
        {
            content = Regex.Replace(content, @"\n|\t", " ");
            content = Regex.Replace(content, @">\s+<", "><").Trim();
            content = Regex.Replace(content, @"\s{2,}", " ");
            content = Regex.Replace(content, @"/\*[\d\D]*?\*/", string.Empty);
            return content;
        }
        #endregion
        internal static string Minify(string path)
        {
            string fileExt = Path.GetExtension(path);
            string content = File.ReadAllText(path);
            switch (fileExt)
            {
                case ".xml":
                    return DoXML(content);
                case ".css":
                    return doCss(content);
                case ".js":
                    return JSMin.MinifyJSCode(content);
                case ".json":
                    return Regex.Replace(content, "(\"(?:[^\"\\\\]|\\\\.)*\")|\\s+", "$1", RegexOptions.Compiled);
                case ".html":
                    return doHtml(content);
                default:
                    return content;
            }
        }
    }
}