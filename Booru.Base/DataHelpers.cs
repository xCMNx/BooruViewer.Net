using Booru.Core;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Booru.Base
{
    public static class DataHelpers
    {
        public static readonly string[] EXT_LIST = (Core.Utils.Helpers.ReadFromConfig("ExtList") ?? ".jpg;.png;.jpeg;.gif;.zip;.tga").Split(';');
        public static readonly string[] PREV_EXT_LIST = (Core.Utils.Helpers.ReadFromConfig("PrevExtList") ?? ".jpg;.png;.jpeg").Split(';');
        //public static string[] EXT_LIST = new[] { ".jpg", ".png", ".jpeg", ".gif", ".zip", ".tga" };

        const string CMD_SERVER = "%SERVER%";
        const string CMD_SUBSERVER = "%SUBSERVER%";
        const string CMD_MD5 = "%MD5%";
        const string CMD_EXT = "%EXT%";

        public static List<string> GenerateUrls(DataRecord Data, bool preview = false)
        {
            var urls = new List<string>();
            foreach (var srv in Data.Servers)
            {
                var t = Templates.getTemplate(srv.Server);
                if (t == null) continue;
                urls.AddRange(GenerateUrls(preview ? t.PreviewMask : t.FileMask, srv, Data.MD5, preview ? PREV_EXT_LIST : EXT_LIST));
            }
            return urls;
        }

        public static List<string> GenerateUrls(string Mask, DataServer srv, string MD5, string[] extList )
        {
            if (string.IsNullOrWhiteSpace(Mask)) return null;
            var urls = new List<string>();
            var um = Mask.ToUpper();
            if (um.Contains(CMD_SUBSERVER))
                foreach (var s in srv.subServers)
                    urls.AddRange(GenerateUrls(s, MD5, srv.Ext, Mask, extList));
            else
                urls.AddRange(GenerateUrls(srv.Server, MD5, srv.Ext, Mask, extList));
            return urls;
        }

        public static List<string> GenerateUrls(string Server, string MD5, string Ext, string Mask, string[] extList)
        {
            var urls = new List<string>();
            if (!string.IsNullOrWhiteSpace(Ext))
                urls.Add(GetFileURL(Server, MD5, Ext, Mask));
            if (Mask.ToUpper().Contains(CMD_EXT))
            {
                foreach (var ext in extList)
                    if (!ext.Equals(Ext, StringComparison.CurrentCultureIgnoreCase))
                        urls.Add(GetFileURL(Server, MD5, ext, Mask));
            }
            else if (string.IsNullOrWhiteSpace(Ext))
                urls.Add(GetFileURL(Server, MD5, null, Mask));
            return urls;
        }

        public static string MatchEvaluator(Match match, string Server, string MD5, string Ext)
        {
            var v = match.Value.ToUpper();
            if (v == CMD_SERVER || v == CMD_SUBSERVER) return Server;
            if (v == CMD_MD5) return MD5;
            if (v.StartsWith("%MD5|"))
            {
                v = v.Substring(5);
                var dlm = v.IndexOf('|');
                var s = Convert.ToInt32(v.Substring(0, dlm));
                var cnt = Convert.ToInt32(v.Substring(dlm + 1, v.Length - 3));
                return MD5.Substring(s, cnt);
            }
            if (v == CMD_EXT) return Ext;
            return match.Value;
        }

        public static string GetFileURL(string Server, string MD5, string Ext, string Mask)
        {
            return Regex.Replace(Mask, "%.+?%", (m) => MatchEvaluator(m, Server, MD5, Ext));
        }

        public static Parameters GetParamsFromStr(string prms)
        {
            var r = new Regex("(?<param><.+?>).*?:.*?\"(?<value>.*?)\"");
            var mc = r.Matches(prms);
            var d = new Parameters();
            foreach (Match m in mc)
            {
                var p = m.Groups["param"];
                var v = m.Groups["value"];
                if (p != null && v != null)
                    d.Add(new DictItem(p.Value, v.Value));
            }
            return d;
        }
    }
}
