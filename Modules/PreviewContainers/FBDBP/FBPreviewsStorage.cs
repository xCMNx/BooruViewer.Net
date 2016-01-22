using System;
using System.Data;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Booru.Core;
using Booru.Core.Utils;
using FirebirdSql.Data.FirebirdClient;
using FirebirdSql.Data.Isql;

namespace Booru.Base.PreviewConnectors
{
    [SettingsType(typeof(FBPreviewsSettings))]
    public class FBPreviewsStorage : IPreviewsContainer, IDisposable
    {
        FbConnection _conw;
        FbConnection _conr;
        protected FBPreviewsSettings _CurrentSettings = null;
        public IModuleSettings CurrentSettings { get { return _CurrentSettings; } }

        public const string DEFAULT_CONSTR = "character set=UTF8;user id=SYSDBA;password=masterkey;dialect=3;initial catalog=dbp.fdb;server type=Default;data source=localhost";

        static CancellationTokenSource cts = new CancellationTokenSource();
        static ManualResetEvent StoppedEvent = new ManualResetEvent(true);

        static FBPreviewsStorage()
        {
            _readTransactionOptions.TransactionBehavior = FbTransactionBehavior.ReadCommitted | FbTransactionBehavior.Read | FbTransactionBehavior.RecVersion | FbTransactionBehavior.NoWait;
            _writeTransactionOptions.TransactionBehavior = FbTransactionBehavior.Write | FbTransactionBehavior.Wait | FbTransactionBehavior.Concurrency;
        }

        public FBPreviewsStorage(IContainerSettings settings)
        {
            var set = (FBPreviewsSettings)settings;
            _conw = new FbConnection(set.ConnectionString);
            _conr = new FbConnection(set.ConnectionString);
            try
            {
                _conr.Open();
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
            if (_conr.State == System.Data.ConnectionState.Closed)
                _conr.Open();
            _CurrentSettings = (FBPreviewsSettings)set.Clone();
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

        public void Dispose()
        {
            WaitCompletition();
        }

        void CreateDb(string ConnStr, string ScriptPath)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var stream = assembly.GetManifestResourceStream(typeof(FBPreviewsStorage).Namespace + ".struct.sql");
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

        public int RecordsCount()
        {
            lock (_conr)
            {
                if (_conr.State == System.Data.ConnectionState.Closed) return 0;
                using (var c = _conr.CreateCommand())
                using (c.Transaction = _conr.BeginTransaction(_readTransactionOptions))
                    try
                    {
                        c.CommandText = "select COUNT(0) from LIST";
                        return Convert.ToInt32(c.ExecuteScalar());
                    }
                    finally
                    {
                        c.Transaction.Rollback();
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

        static FbTransactionOptions _writeTransactionOptions = new FbTransactionOptions();
        static FbTransactionOptions _readTransactionOptions = new FbTransactionOptions();

        public void setPreview(string md5, byte[] data, string ext)
        {
            lock (_conw)
            {
                if (_conw.State != System.Data.ConnectionState.Open)
                    _conw.Open();
                using (var c = _conw.CreateCommand())
                using (c.Transaction = _conw.BeginTransaction(_writeTransactionOptions))
                {
                    c.CommandText = $"update or insert into LIST (MD5, DATA) values (?,?) MATCHING (MD5)";
                    c.Parameters.Add(new FbParameter(string.Empty, md5));
                    c.Parameters.Add(new FbParameter(string.Empty, data));
                    c.ExecuteNonQuery();
                    c.Transaction.Commit();
                }
            }
        }

        public void setPreview(string md5, Stream data, string ext)
        {
            byte[] bData = new byte[data.Length];
            data.Position = 0;
            data.Read(bData, 0, bData.Length);
            setPreview(md5, bData, ext);
        }

        public Stream getPreview(string md5)
        {
            lock (_conr)
            {
                if (_conr.State == System.Data.ConnectionState.Closed) return null;
                using (var c = _conr.CreateCommand())
                using (c.Transaction = _conr.BeginTransaction(_readTransactionOptions))
                    try
                    {
                        c.CommandText = $"select DATA from LIST where MD5=?";
                        c.Parameters.Add(new FbParameter(string.Empty, md5));
                        var data = c.ExecuteScalar();
                        if (data != null)
                            return new MemoryStream((byte[])data);
                    }
                    finally
                    {
                        c.Transaction.Rollback();
                    }
            }
            return null;
        }
    }
}
