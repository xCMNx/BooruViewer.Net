using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using Booru.Core;

namespace Booru.Base.Parsers
{

    [SettingsType(typeof(RegExSettings)), SettingsRequiered]
    public class RegEx : IDataParser
    {
        public string PageNextRegexp = "next-page-url=\"/\\?.*next=(?<next>[\\d]+)(?:.*tags=(?<tags>.+?)(?:&|\"|>)|)";
        public string TagsFilterRegexp = "(Rating:.*?\\W)|(Score:.*?[^\\w\\.])|(Size:.*?\\W)|(User:.*?\\W)";
        public string ContentType = "text/";
        public bool UseHtmlDecode;

        public string DefaultPageMask()
        {
            return "$Server/post/?page=<$page>&next=<next>&tags=<$tags>";
        }

        public IList<DataRecord> GetData(byte[] Data, string Host, IParserSettings Settings)
        {
            try
            {
                var text = System.Text.Encoding.UTF8.GetString(Data);
                var r = new List<DataRecord>();
                //*
                MatchCollection mc = Regex.Matches(text, ((RegExSettings)Settings).Expression);
                foreach (Match m in mc)
                {
                    var pos = m.Groups["post"];
                    var srv = m.Groups["server"];
                    var md5 = m.Groups["md5"];
                    var ext = m.Groups["ext"];
                    var tagsM = m.Groups["tags"];
                    var tags = tagsM == null ? string.Empty : tagsM.Value + " ";
                    if (UseHtmlDecode)
                        tags = WebUtility.HtmlDecode(tags);
                    var m2 = Regex.Match(tags, "Rating:(\\w)");
                    var m3 = Regex.Match(tags, @"User:([\w]*)");
                    if (!string.IsNullOrWhiteSpace(TagsFilterRegexp))
                        tags = Regex.Replace(tags, TagsFilterRegexp, string.Empty);
                    if (md5 != null)
                        r.Add(new DataRecord()
                        {
                            MD5 = md5.Value,
                            Rating = m2.Success ? (DataRating)m2.Groups[1].Value.ToLower()[0] : (DataRating)'q',
                            Tags = tags.Trim().Split(' '),
                            Servers = new[]{ new DataServer(){
                                Post = pos == null ? 0 : Convert.ToInt32(pos.Value),
                                Server = Host,
                                subServers = (srv == null ? new string[0] : new[]{srv.Value.ToLower().Trim()}),
                                Autor = m3.Success ? m3.Groups[1].Value : string.Empty,
                                Ext = ext == null ? null : ext.Value.ToLower().Trim()
                            }}
                        });
                }
                //*/
                return r;
            }
            catch (Exception e)
            {
                throw new ParserException(e);
            }
        }
    }
}
