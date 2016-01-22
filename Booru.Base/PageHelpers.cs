using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Booru.Base
{

    [Serializable]
    public class DictItem
    {
        public string Key;
        public string Value;

        public DictItem(string k, string v)
        {
            Key = k;
            Value = v;
        }
        public DictItem()
        {
        }
    }

    public class Parameters : List<DictItem> { };

    public static class PageHelpers
    {
        public static string Value(this List<DictItem> lst, string key)
        {
            foreach (var i in lst)
                if (i.Key == key)
                    return i.Value;
            return null;
        }

        public static string GetPageURL(Parameters param, string PageMask, ServerTemplate tmpl)
        {
            var s = Regex.Replace(PageMask, "<.+?>", (m) => param.Value(m.Value));
            return Regex.Replace(s, "\\$([\\w]+)", (m) =>
            {
                switch (m.Value)
                {
                    case "$Server": return tmpl.Server.AbsoluteUri;
                }
                return m.Value;
            });
        }

    }
}
