using System.Linq;

namespace Booru.Core
{
    public enum DataRating { Safe = 's', Questionable = 'q', Explicit = 'e' }
    public class DataServer
    {
        public string Server;
        public string[] subServers;
        public int Post = -1;
        public int ParentPost = -1;
        public int ChildPost = -1;
        public string Ext;
        public int Size = -1;
        public string Autor;
    }
    public class DataRecord
    {
        public static readonly DataRecord Empty = new DataRecord();
        public DataServer[] Servers;
        public string[] Tags;
        public string MD5;
        public DataRating Rating = DataRating.Questionable;
        public override string ToString()
        {
            return string.Format(
@"MD5: {1}
Server: {0}
Rating: {3}
Tags: {2}
",
             string.Join("; ", Servers.Select((s) => s.Server).ToArray()), MD5, string.Join(" ", Tags), Rating
            );
        }
    }
}
