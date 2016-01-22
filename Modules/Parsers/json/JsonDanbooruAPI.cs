using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Booru.Core;

namespace Booru.Base.Parsers
{
    public class JsonDanbooruAPI : IDataParser
    {
        public string DefaultPageMask()
        {
            return "$Server/post/index.json?page=<$page>&limit=1000&tags=<$tags>";
        }

        int IntOrDefault(object obj)
        {
            return obj == null ? -1 : obj is string ? int.Parse((string)obj) : Convert.ToInt32(obj);
        }

        public IList<DataRecord> GetData(byte[] Data, string Host, IParserSettings Settings)
        {
            try
            {
                var text = System.Text.Encoding.UTF8.GetString(Data);
                var tkn = Newtonsoft.Json.Linq.JToken.Parse(text);
                return tkn.Select(r => new DataRecord()
                {
                    MD5 = (string)r["md5"],
                    Rating = (DataRating)((string)r["rating"])[0],
                    Tags = ((string)r["tags"])?.Split(' ').ToArray(),
                    Servers = new[]{
                        new DataServer()
                        {
                            Post = IntOrDefault(r["id"]),
                            Server = Host,
                            subServers = new string[] { new Uri((string)r["file_url"]).Authority },
                            Size = IntOrDefault(r["file_size"]),
                            ParentPost = IntOrDefault(r["parent_id"]),
                            Autor = (string)r["author"],
                            Ext = Path.GetExtension((string)r["file_url"])
                        }
                    }
                }).ToArray();
            }
            catch (Exception e)
            {
                throw new ParserException(e);
            }
        }
    }
}
