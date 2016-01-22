using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Booru.Core;
using Booru.Core.Utils;
using FirebirdSql.Data.FirebirdClient;
using FirebirdSql.Data.Isql;

namespace Booru.Base.DataContainers
{
    public static class FBhelp
    {
        public static long GetGeneratorValue(this FbCommand com, string GenName, int inc = 0)
        {
            using (var c = new FbCommand(string.Format("select GEN_ID({0}, {1}) from RDB$DATABASE", GenName, inc), com.Connection, com.Transaction))
                return (long)c.ExecuteScalar();
        }
    }

    [SettingsType(typeof(FbDatabaseSettings))]
    public class FbDatabase : IDataContainer, IDisposable
    {
        FbConnection _conw;
        FbConnection _conr;
        protected FbDatabaseSettings _CurrentSettings = null;
        public IModuleSettings CurrentSettings { get { return _CurrentSettings; } }

        public const string DEFAULT_CONSTR = "character set=UTF8;user id=SYSDBA;password=masterkey;dialect=3;initial catalog=db.fdb;server type=Default;data source=localhost";

        static CancellationTokenSource cts = new CancellationTokenSource();
        static ManualResetEvent StoppedEvent = new ManualResetEvent(true);
        static int TAGS_LENGTH = 128;

        static FbDatabase()
        {
            _readTransactionOptions.TransactionBehavior = FbTransactionBehavior.ReadCommitted | FbTransactionBehavior.Read | FbTransactionBehavior.RecVersion | FbTransactionBehavior.NoWait;
            _writeTransactionOptions.TransactionBehavior = FbTransactionBehavior.Write | FbTransactionBehavior.Wait | FbTransactionBehavior.Concurrency;
        }

        public FbDatabase(IContainerSettings settings)
        {
            var set = (FbDatabaseSettings)settings;
            _conw = new FbConnection(set.ConnectionString);
            try
            {
                _conw.Open();
                SwitchTrigger("MD5_LIST_FROM_EMPTY", UpdateEmptyList() > 0);
            }
            catch (FbException e)
            {
                if (e.ErrorCode == 335544344)
                {
                    if (System.Windows.Forms.MessageBox.Show("File not exists" + (set.isEmbedded ? " or database is opened" : string.Empty) + ".\r\nTry to create file?", "Error", MessageBoxButtons.YesNo) == DialogResult.No)
                        throw;
                    CreateDb(_conw.ConnectionString, Path.Combine(Helpers.AssemblyDirectory(Assembly.GetExecutingAssembly()), "struct.sql"));
                    _conw.Open();
                }
                else
                    throw;
            }
            if (_conw.State == System.Data.ConnectionState.Closed)
                _conw.Open();
            _CurrentSettings = (FbDatabaseSettings)set.Clone();
            _conr = new FbConnection(set.ConnectionString);
            _conr.Open();
            InitTagsLength();
            StoppedEvent.Set();
        }

        public void Stop()
        {
            cts.Cancel();
        }
        public void WaitCompletition()
        {
            cts.Cancel();
            StoppedEvent.WaitOne();
            if (_conw != null && _conw.State != System.Data.ConnectionState.Closed)
            {
                lock (_conw)
                {
                    _conw.Close();
                }
                _conw = null;
            }
            if (_conr != null && _conr.State != System.Data.ConnectionState.Closed)
            {
                lock (_conr)
                {
                    _conr.Close();
                }
                _conr = null;
            }
        }

        void InitTagsLength()
        {
            lock (_conr)
            {
                using (var c = _conr.CreateCommand())
                using (c.Transaction = _conr.BeginTransaction(_readTransactionOptions))
                    try
                    {
                        c.CommandText = "SELECT RDB$CHARACTER_LENGTH FROM RDB$RELATION_FIELDS CAMPOS left join RDB$FIELDS DADOSCAMPO on CAMPOS.RDB$FIELD_SOURCE = DADOSCAMPO.RDB$FIELD_NAME WHERE CAMPOS.RDB$RELATION_NAME = 'TAGS_LIST' and CAMPOS.RDB$FIELD_NAME = 'TAG'";
                        TAGS_LENGTH = (Int16)c.ExecuteScalar();
                    }
                    finally
                    {
                        c.Transaction.Rollback();
                    }
            }
        }

        public void Dispose()
        {
            WaitCompletition();
        }

        void CreateDb(string ConnStr, string ScriptPath)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var stream = assembly.GetManifestResourceStream(typeof(FbDatabase).Namespace + ".struct.sql");
            string script = new StreamReader(stream, System.Text.Encoding.ASCII).ReadToEnd();
            FbScript fbs = new FbScript(script);
            fbs.Parse();
            FbConnection.CreateDatabase(_conw.ConnectionString);
            using (var c = new FbConnection(ConnStr))
            {
                c.Open();
                try
                {
                    using (FbTransaction myTransaction = c.BeginTransaction())
                    {
                        FbBatchExecution fbe = new FbBatchExecution(c, fbs);
                        fbe.CommandExecuting += (sender, args) => args.SqlCommand.Transaction = myTransaction;
                        fbe.Execute(true);
                    }
                }
                finally
                {
                    c.Close();
                }
            }
        }

        private void ExecuteScript(FbConnection myConnection, string scriptPath)
        {
            try
            {
                using (FbTransaction myTransaction = myConnection.BeginTransaction())
                {
                    if (!File.Exists(scriptPath))
                        throw new FileNotFoundException("Script not found", scriptPath);

                    string script = File.ReadAllText(scriptPath);

                    // use FbScript to parse all statements
                    FbScript fbs = new FbScript(script);
                    fbs.Parse();

                    // execute all statements
                    FbBatchExecution fbe = new FbBatchExecution(myConnection, fbs);
                    fbe.CommandExecuting += (sender, args) => args.SqlCommand.Transaction = myTransaction;
                    fbe.Execute(true);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                throw;
            }
        }

        public int UpdateEmptyList()
        {
            lock (_conw)
            {
                if (_conw.State != System.Data.ConnectionState.Open)
                    _conw.Open();
                using (var c = _conw.CreateCommand())
                using (c.Transaction = _conw.BeginTransaction(_writeTransactionOptions))
                {
                    c.CommandText = "merge into EMPTY_MD5_ID em using GET_EMPTY_MD5_ID pm on em.md5_id = pm.EMPTY_MD5_ID when not matched then insert (MD5_ID) VALUES (pm.EMPTY_MD5_ID)";
                    c.ExecuteNonQuery();
                    c.CommandText = "select count(0) from EMPTY_MD5_ID";
                    var res = Convert.ToInt32(c.ExecuteScalar());
                    c.Transaction.Commit();
                    return res;
                }
            }
        }

        public void SwitchTrigger(string TriggerName, bool switchOn)
        {
            lock (_conw)
            {
                if (_conw.State != System.Data.ConnectionState.Open)
                    _conw.Open();
                using (var c = _conw.CreateCommand())
                using (c.Transaction = _conw.BeginTransaction(_writeTransactionOptions))
                {
                    c.CommandText = string.Format("ALTER TRIGGER {0} {1}", TriggerName, (switchOn ? "ACTIVE" : "INACTIVE"));
                    c.ExecuteNonQuery();
                    c.Transaction.Commit();
                }
            }
        }

        public void ExecProcedure(string ProcName)
        {
            lock (_conw)
            {
                if (_conw.State != System.Data.ConnectionState.Open)
                    _conw.Open();
                using (var c = _conw.CreateCommand())
                using (c.Transaction = _conw.BeginTransaction(_writeTransactionOptions))
                {
                    c.CommandText = ProcName;
                    c.CommandType = CommandType.StoredProcedure;
                    c.ExecuteNonQuery();
                    c.Transaction.Commit();
                }
            }
        }

        public void ResetMD5Generator()
        {
            ExecProcedure("RESET_MD5_GENERATOR");
        }

        SortedList<string, long> TagsCachedId = new SortedList<string, long>();

        public Dictionary<string, long> AddTags(FbTransaction tr, IEnumerable<string> tags, out int newTags)
        {
            newTags = 0;
            var r = new Dictionary<string, long>();
            using (FbCommand cmd = new FbCommand("update or insert into TAGS_LIST (TAG) values (?) MATCHING (TAG) returning TAG_ID", tr.Connection, tr))
            {
                long tc = cmd.GetGeneratorValue("GEN_TAG_INDEX");
                var p = new FbParameter();
                p.Direction = System.Data.ParameterDirection.Output;
                var p2 = new FbParameter();
                cmd.Parameters.Add(p2);
                cmd.Parameters.Add(p);
                long id;
                foreach (var t in tags)
                    if (TagsCachedId.TryGetValue(t, out id))
                        r[t] = id;
                    else
                    {
                        p2.Value = t;
                        if (cmd.ExecuteNonQuery() > 0)
                            if (tc < (TagsCachedId[t] = r[t] = (long)p.Value))
                                newTags++;
                    }
            }
            return r;
        }

        static FbTransactionOptions _writeTransactionOptions = new FbTransactionOptions();

        public string Statistics = string.Empty;

        public string getStatistics()
        {
            return Statistics;
        }

        public void AddDataRecords(IEnumerable<DataRecord> data, out int newData, out int NewTags)
        {
            newData = 0;
            NewTags = 0;
            lock (_conw)
            {
                if (cts.IsCancellationRequested) return;
                StoppedEvent.Reset();
                if (_conw.State != System.Data.ConnectionState.Open)
                    _conw.Open();
                try
                {
                    var ttTc = Helpers.TickCount;
                    long diTc = 0, mTc = 0, tTc = 0, tiTc = 0, tc = 0;
                    bool failed = false;
                    using (var tr = _conw.BeginTransaction(_writeTransactionOptions))
                    {
                        //var removed = new List<long>();
                        try
                        {
                            var md5 = new Dictionary<int, long>();
                            using (FbCommand cmd = new FbCommand(string.Empty, _conw, tr)/*, cmd1_5 = new FbCommand(string.Empty, _con, tr)*/)
                            {
                                long mc = cmd.GetGeneratorValue("GEN_MD5_INDEX");
                                List<string> tgs = new List<string>();

                                var mlMD5 = cmd.CreateParameter();
                                var mlRating = cmd.CreateParameter();
                                var mlMD5Id = cmd.CreateParameter();
                                mlMD5Id.Direction = System.Data.ParameterDirection.Output;
                                cmd.Parameters.AddRange(new FbParameter[] { mlMD5, mlRating, mlMD5Id });

                                cmd.CommandText = "update or insert into MD5_LIST (MD5, RATING) values (?,?) MATCHING (MD5) returning MD5_ID";
                                using (
                                    FbCommand cmd2 = new FbCommand("update or insert into SERVERS_LIST (SERVER_NAME, SERVER_GROUP_ID) values (?,?) matching (SERVER_NAME) returning SERVER_ID", _conw, tr),
                                    cmd3 = new FbCommand("update or insert into DATA_INFO (DATA_MD5_ID, DATA_SERVER_ID, DATA_POST_NUMBER, DATA_EXT_ID, DATA_SIZE, AUTOR_ID, CHILD_POST, PARENT_POST) values (?,?,?,?,?,?,?,?) matching (DATA_MD5_ID, DATA_SERVER_ID)", _conw, tr),
                                    cmd4 = new FbCommand("update or insert into EXT_LIST (EXT) values (?) matching (EXT) returning EXT_ID", _conw, tr),
                                    cmd5 = new FbCommand("update or insert into AUTORS (AUTOR) values (?) matching (AUTOR) returning AUTOR_ID", _conw, tr)
                                )
                                {
                                    #region parameters

                                    #region SERVERS_LIST   
                                    var slServerName = cmd2.CreateParameter();
                                    var slServerGroup = cmd2.CreateParameter();
                                    var slServerId = cmd2.CreateParameter();
                                    slServerId.Direction = System.Data.ParameterDirection.Output;
                                    cmd2.Parameters.AddRange(new[] { slServerName, slServerGroup, slServerId });
                                    #endregion

                                    #region DATA_INFO
                                    var diMD5Id = cmd3.CreateParameter();
                                    var diServerId = cmd3.CreateParameter();
                                    var diPost = cmd3.CreateParameter();
                                    var diExtId = cmd3.CreateParameter();
                                    var diFileSize = cmd3.CreateParameter();
                                    var diAutorId = cmd3.CreateParameter();
                                    var diParentPost = cmd3.CreateParameter();
                                    var diChildPost = cmd3.CreateParameter();
                                    cmd3.Parameters.AddRange(new[] { diMD5Id, diServerId, diPost, diExtId, diFileSize, diAutorId, diChildPost, diParentPost });
                                    #endregion

                                    #region EXT_LIST
                                    var elExt = cmd4.CreateParameter();
                                    var elExtId = cmd4.CreateParameter();
                                    elExtId.Direction = System.Data.ParameterDirection.Output;
                                    cmd4.Parameters.AddRange(new[] { elExt, elExtId });
                                    #endregion

                                    #region AUTORS
                                    var aAutor = cmd5.CreateParameter();
                                    var aAutorId = cmd5.CreateParameter();
                                    aAutorId.Direction = System.Data.ParameterDirection.Output;
                                    cmd5.Parameters.AddRange(new[] { aAutor, aAutorId });
                                    #endregion

                                    #endregion

                                    var autors = new Dictionary<string, int>();
                                    var exts = new Dictionary<string, int>();
                                    var servers = new Dictionary<string, int>();
                                    int i = 0;
                                    foreach (var d in data)
                                        if (!string.IsNullOrWhiteSpace(d.MD5) && d.Servers != null)
                                        {
                                            i++;
                                            mlMD5.Value = d.MD5;
                                            mlRating.Value = (Char)d.Rating;
                                            tc = Helpers.TickCount;
                                            if (cmd.ExecuteNonQuery() > 0)
                                            {
                                                mTc += Helpers.TickCount - tc;
                                                long id = md5[i] = (long)mlMD5Id.Value;
                                                diMD5Id.Value = id;
                                                if (mc < id)
                                                    newData++;
                                                int indx = 0;

                                                #region preparing tags list
                                                if (d.Tags != null)
                                                    foreach (var t in d.Tags)
                                                        if (t.Length <= TAGS_LENGTH)
                                                            if (0 > (indx = tgs.BinarySearch(t)))
                                                                tgs.Insert(~indx, t);
                                                #endregion

                                                #region fill datainfo
                                                tc = Helpers.TickCount;
                                                foreach (var srv in d.Servers)
                                                {
                                                    int si = -1;
                                                    //trying to get server id from storage
                                                    if (!servers.TryGetValue(srv.Server, out si))
                                                    {
                                                        slServerName.Value = srv.Server;
                                                        slServerGroup.Value = null;
                                                        if (cmd2.ExecuteNonQuery() > 0)
                                                            servers[srv.Server] = si = (int)slServerId.Value;
                                                        else
                                                            continue;
                                                    }

                                                    //fill subservers if exists
                                                    if (srv.subServers != null)
                                                        foreach (var ss in srv.subServers)
                                                            if (!servers.ContainsKey(ss))
                                                            {
                                                                slServerName.Value = ss;
                                                                slServerGroup.Value = si;
                                                                cmd2.ExecuteNonQuery();
                                                                servers[ss] = (int)slServerId.Value;
                                                            }

                                                    int tmpId = 0;
                                                    #region Ext data
                                                    diExtId.Value = null;
                                                    if (!string.IsNullOrWhiteSpace(srv.Ext))
                                                    {
                                                        if (exts.TryGetValue(srv.Ext, out tmpId))
                                                            diExtId.Value = tmpId;
                                                        else
                                                        {
                                                            elExt.Value = srv.Ext;
                                                            if (cmd4.ExecuteNonQuery() > 0)
                                                            {
                                                                diExtId.Value = elExtId.Value;
                                                                exts[srv.Ext] = (int)elExtId.Value;
                                                            }
                                                        }
                                                    }
                                                    #endregion

                                                    #region Autors data
                                                    diAutorId.Value = null;
                                                    if (!string.IsNullOrWhiteSpace(srv.Autor))
                                                    {
                                                        if (autors.TryGetValue(srv.Autor, out tmpId))
                                                            diAutorId.Value = tmpId;
                                                        else
                                                        {
                                                            aAutor.Value = srv.Autor;
                                                            if (cmd5.ExecuteNonQuery() > 0)
                                                            {
                                                                diAutorId.Value = aAutorId.Value;
                                                                autors[srv.Autor] = (int)aAutorId.Value;
                                                            }
                                                        }
                                                    }
                                                    #endregion
                                                    diServerId.Value = si;
                                                    diFileSize.Value = srv.Size < 0 ? null : (object)srv.Size;
                                                    diPost.Value = srv.Post < 0 ? null : (object)srv.Post;
                                                    diParentPost.Value = srv.ParentPost < 0 ? null : (object)srv.ParentPost;
                                                    diChildPost.Value = srv.ChildPost < 0 ? null : (object)srv.ChildPost;
                                                    cmd3.ExecuteNonQuery();
                                                }
                                                diTc += Helpers.TickCount - tc;
                                                #endregion
                                            }
                                        }
                                }
                                tc = Helpers.TickCount;
                                var tags = AddTags(tr, tgs, out NewTags);
                                tTc = Helpers.TickCount - tc;
                                //fill tag<->md5 joining table
                                cmd.Parameters.Clear();
                                var tMD5Id = cmd.CreateParameter();
                                var tTagId = cmd.CreateParameter();
                                cmd.Parameters.AddRange(new[] { tMD5Id, tTagId });
                                cmd.CommandText = "update or insert into TAGS (MD5_ID, TAG_ID) values (?,?) MATCHING (MD5_ID, TAG_ID)";
                                mlMD5.Direction = System.Data.ParameterDirection.Input;
                                int j = 0;
                                tc = Helpers.TickCount;
                                foreach (var d in data)
                                {
                                    j++;
                                    if (md5.ContainsKey(j))
                                    {
                                        tMD5Id.Value = md5[j];
                                        long id;
                                        foreach (var t in d.Tags)
                                            if (tags.TryGetValue(t, out id))
                                            {
                                                tTagId.Value = id;
                                                cmd.ExecuteNonQuery();
                                            }
                                    }
                                }
                                tiTc = Helpers.TickCount - tc;
                            }
                            tr.Commit();
                            Statistics = string.Format("Total: {0}; MD5: {1}; DataInfo: {2}, Tags: {3}; MD5-Tags: {4}", Helpers.TickCount - ttTc, mTc, diTc, tTc, tiTc);
                        }
                        catch (Exception e)
                        {
                            tr.Rollback();
                            failed = true;
                            //EmptyMD5Slots.InsertRange(0, removed);
                            Helpers.ConsoleWrite(e.ToString(), ConsoleColor.DarkRed);
                            throw new DataSourceAddRecordsException();
                        }
                    }
                    if (failed)
                        ResetMD5Generator();
                }
                finally
                {
                    StoppedEvent.Set();
                }
            }
        }

        public int MD5Count()
        {
            lock (_conr)
            {
                if (_conr.State == System.Data.ConnectionState.Closed) return 0;
                using (var c = _conr.CreateCommand())
                using (c.Transaction = _conr.BeginTransaction(_readTransactionOptions))
                    try
                    {
                        c.CommandText = "select count(0) from MD5_LIST";
                        return Convert.ToInt32(c.ExecuteScalar());
                    }
                    finally
                    {
                        c.Transaction.Rollback();
                    }
            }
        }

        public int MD5HighId()
        {
            lock (_conr)
            {
                if (_conr.State == System.Data.ConnectionState.Closed) return 0;
                using (var c = _conr.CreateCommand())
                using (c.Transaction = _conr.BeginTransaction(_readTransactionOptions))
                    try
                    {
                        c.CommandText = "select GEN_ID(gen_md5_index,0) from RDB$DATABASE";
                        return Convert.ToInt32(c.ExecuteScalar());
                    }
                    finally
                    {
                        c.Transaction.Rollback();
                    }
            }
        }

        public BitArray Filter(string Tag, int Size)
        {
            lock (_conr)
            {
                long tag_id = -1;
                var curList = new BitArray(Size, false);
                if (_conr.State == System.Data.ConnectionState.Closed) return curList;
                using (var c = _conr.CreateCommand())
                {
                    using (c.Transaction = _conr.BeginTransaction(_readTransactionOptions))
                        try
                        {
                            if (string.IsNullOrWhiteSpace(Tag))
                                c.CommandText = "select m.MD5_ID from md5_list m order by m.MD5_ID";
                            else
                            {
                                int delimIdx = Tag.IndexOf(':');
                                var cmd = delimIdx > 0 ? Tag.Substring(0, delimIdx).ToLower() : string.Empty;
                                var tag = delimIdx > 0 ? Tag.Substring(delimIdx + 1).ToLower() : string.Empty;
                                switch (cmd)
                                {
                                    case "rating":
                                        {
                                            if (tag.Length < 1 || (tag[0] != 'e' && tag[0] != 'q' && tag[0] != 's'))
                                                return curList;
                                            c.CommandText = string.Format("select m.MD5_ID from md5_list m where m.Rating = '{0}' order by m.MD5_ID", tag[0]);
                                        }
                                        break;
                                    case "server":
                                        {
                                            var p = c.CreateParameter();
                                            c.Parameters.Add(p);
                                            p.Value = tag;
                                            c.CommandText = string.Format("select m.MD5_ID from md5_list m join data_info di on di.data_md5_id=m.md5_id join servers_list sl on sl.server_id = di.data_server_id where sl.server_name like ?");
                                        }
                                        break;
                                    default:
                                        {
                                            var p = c.CreateParameter();
                                            p.Value = Tag;
                                            c.Parameters.Add(p);
                                            //check if Tag is cacheable
                                            if (Tag.IndexOf('%') >= 0)
                                                c.CommandText = "select m.MD5_ID from md5_list m join tags t on t.md5_id=m.md5_id join tags_list tl on t.tag_id=tl.tag_id and tl.tag like ? order by m.MD5_ID";
                                            else
                                            {
                                                //try to get cached data or at least tag id
                                                c.CommandText = "select tl.TAG_ID, tfc.CACHE from tags_list tl left join tags_fetch_chache tfc on tfc.tag_id = tl.tag_id where tl.tag = ?";
                                                using (var rdr = c.ExecuteReader())
                                                {
                                                    if (rdr.Read())
                                                    {
                                                        //is there a cached data
                                                        if (rdr.IsDBNull(1))
                                                        {
                                                            //nope read data as is
                                                            p.Value = tag_id = rdr.GetInt64(0);
                                                            c.CommandText = "select distinct MD5_ID from tags where tag_id = ? order by MD5_ID";
                                                        }
                                                        else //yep read data directly to map
                                                        {
                                                            curList = new BitArray((Byte[])rdr[1]);
                                                            curList.Length = Size;
                                                            return curList;
                                                        }
                                                    }
                                                    else
                                                        return curList;
                                                }
                                            }
                                        }
                                        break;
                                }
                            }
                            using (var reader = c.ExecuteReader())
                            {
                                var id = -1;
                                while (reader.Read())
                                {
                                    id = Convert.ToInt32(reader.GetValue(0));
                                    if (id < Size)
                                        curList.Set(id, true);
                                    else
                                        break;
                                }
                                if (id == -1)
                                    tag_id = -1;
                            }
                        }
                        finally
                        {
                            c.Transaction.Rollback();
                        }
                    if (tag_id > -1)
                        using (c.Transaction = _conr.BeginTransaction())
                            try
                            {
                                c.Parameters.Clear();
                                c.CommandText = string.Format("update or insert into TAGS_FETCH_CHACHE (TAG_ID, CACHE) values ({0}, ?) matching (TAG_ID)", tag_id);
                                c.Parameters.Add(new FbParameter(string.Empty, curList.ToBytes()));
                                c.ExecuteNonQuery();
                                c.Transaction.Commit();
                            }
                            catch
                            {
                                c.Transaction.Rollback();
                            }
                }
                return curList;
            }
        }

        static FbTransactionOptions _readTransactionOptions = new FbTransactionOptions();

        public DataRecord getInfo(int MD5id)
        {
            try
            {
                lock (_conr)
                {
                    if (_conr.State == System.Data.ConnectionState.Closed) return DataRecord.Empty;
                    using (var c = _conr.CreateCommand())
                    using (c.Transaction = _conr.BeginTransaction(_readTransactionOptions))
                        try
                        {
                            c.CommandText = "select m.MD5, m.rating, (select list(tl.tag, ' ') MD5_TAGS from tags t join tags_list tl on tl.tag_id=t.tag_id where md5_id=m.md5_id) from md5_list m where m.md5_id = " + MD5id.ToString();
                            var rdr = c.ExecuteReader();
                            if (!rdr.Read()) return null;
                            var md5 = rdr.GetString(0);
                            var rating = rdr.IsDBNull(1) ? DataRating.Questionable : (DataRating)rdr.GetString(1)[0];
                            var tags = rdr.IsDBNull(2) ? string.Empty : rdr.GetString(2);
                            var r = new DataRecord()
                            {
                                MD5 = md5,
                                Rating = rating,
                                Tags = tags.Split(' ')
                            };
                            c.CommandText = "select sl.server_name, el.ext, di.data_size, di.data_post_number, sl.server_id from data_info di left join servers_list sl on sl.server_id = di.data_server_id left join ext_list el on el.ext_id=di.data_ext_id where di.data_md5_id = " + MD5id.ToString();
                            var srvs = new List<DataServer>();
                            var ids = new List<int>();
                            rdr = c.ExecuteReader();
                            while (rdr.Read())
                            {
                                var srv = new DataServer();
                                srv.Server = rdr.GetString(0);
                                srv.Ext = rdr.IsDBNull(1) ? string.Empty : rdr.GetString(1);
                                srv.Size = rdr.IsDBNull(2) ? -1 : rdr.GetInt32(2);
                                srv.Post = rdr.IsDBNull(3) ? -1 : rdr.GetInt32(3);
                                ids.Add(rdr.GetInt32(4));
                                srvs.Add(srv);
                            }
                            r.Servers = srvs.ToArray();
                            for (int i = 0; i < ids.Count; i++)
                            {
                                c.CommandText = "select server_name from servers_list where SERVER_GROUP_ID = " + ids[i].ToString();
                                List<string> l = new List<string>();
                                rdr = c.ExecuteReader();
                                while (rdr.Read())
                                    l.Add(rdr.GetString(0));
                                srvs[i].subServers = l.ToArray();
                            }
                            return r;
                        }
                        finally
                        {
                            c.Transaction.Rollback();
                        }
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
