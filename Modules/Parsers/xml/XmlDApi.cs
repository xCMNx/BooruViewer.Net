using System;
using System.Collections.Generic;
using System.IO;
using Booru.Classes;
using Booru.Core;

namespace Booru.Base.Parsers
{
    public class XmlDanbooruAPI : IDataParser
    {
        public string DefaultPageMask()
        {
            return "$Server/post/index.xml?page=<$page>&limit=1000&tags=<$tags>";
        }

        public static int intParseOrDefault(string s, int Default)
        {
            int val;
            if (!int.TryParse(s, out val))
                return Default;
            return val;
        }

        public IList<DataRecord> GetData(byte[] Data, string Host, IParserSettings Settings)
        {
            try
            {
                var text = System.Text.Encoding.UTF8.GetString(Data);
                var r = new List<DataRecord>();
                var x = XmlToDynamic.Parse(text);
                if (x != null)
                    foreach (var o in x.post)
                        if (o.md5 != null)
                            r.Add(new DataRecord()
                            {
                                MD5 = o.md5,
                                Tags = o.tags.Split(' '),
                                Rating = o.rating != null ? (DataRating)o.rating[0] : DataRating.Questionable,
                                Servers = new[]{ new DataServer(){
                                    Post = intParseOrDefault(o.id, -1),
                                    Server = Host,
                                    Size = intParseOrDefault(o.file_size, -1),
                                    ParentPost = intParseOrDefault(o.parent_id, -1),
                                    Autor = o.author,
                                    Ext = Path.GetExtension(o.file_url),
                                }}
                            });
                return r;
            }
            catch (Exception e)
            {
                throw new ParserException(e);
            }
        }
    }
}
