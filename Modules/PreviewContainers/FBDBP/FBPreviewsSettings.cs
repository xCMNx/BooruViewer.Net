using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using Booru.Core;
using FirebirdSql.Data.FirebirdClient;

namespace Booru.Base.PreviewConnectors
{
    [ModuleType(typeof(FBPreviewsStorage))]
    public class FBPreviewsSettings : BindableBase, IContainerSettings
    {
        public const string DEFAULT_USER = "SYSDBA";
        public const string DEFAULT_PASS = "masterkey";
        public const string DEFAULT_CHARSET = "UTF8";
        public const string DEFAULT_SERVER = "localhost";
        public const string DEFAULT_PATH = "db.fdb";

        protected string _User = DEFAULT_USER;

        public bool isConfigured => true;

        public string User
        {
            get { return _User; }
            set
            {
                _User = value;
                NotifyPropertyChanged(nameof(User));
            }
        }

        public string ConnectionString
        {
            get
            {
                var sb = new FbConnectionStringBuilder();
                sb.UserID = User;
                sb.Password = Pass;
                sb.DataSource = string.IsNullOrWhiteSpace(Server) ? null : Server.Trim();
                sb.Database = Path;
                sb.ServerType = string.IsNullOrWhiteSpace(Server) ? FbServerType.Embedded : FbServerType.Default;
                return sb.ToString();
            }
        }

        [XmlIgnore]
        public bool isEmbedded { get { return string.IsNullOrWhiteSpace(Server); } }

        protected string _Pass = DEFAULT_PASS;
        public string Pass
        {
            get { return _Pass; }
            set
            {
                _Pass = value;
                NotifyPropertyChanged(nameof(Pass));
            }
        }

        protected string _Server;
        public string Server
        {
            get { return _Server; }
            set
            {
                _Server = value;
                NotifyPropertyChanged(nameof(Server));
            }
        }

        protected string _Path = DEFAULT_PATH;
        public string Path
        {
            get { return _Path; }
            set
            {
                _Path = value;
                NotifyPropertyChanged(nameof(Path));
            }
        }

        public void Reset()
        {
            User = DEFAULT_USER;
            Pass = DEFAULT_PASS;
            Server = null;
            Path = DEFAULT_PATH;
        }

        public int CompareTo(object obj)
        {
            if (obj as FBPreviewsSettings == null) return 1;
            var set = (FBPreviewsSettings)obj;
            var i = string.Compare(_Server, set._Server, true);
            if (i == 0)
            {
                i = string.Compare(_Path, _Path, true);
                if (i == 0)
                {
                    i = string.Compare(_User, _User, true);
                    if (i == 0)
                        i = string.Compare(_Pass, _Pass, true);
                }
            }
            return i;
        }

        public IModuleSettings Clone()
        {
            return EditableModuleHelper.Clone(this);
            //throw new NotImplementedException($"Method Clone() of IEditableModuleSettings in class {GetType().FullName} isn't implemented.");
        }

        #region Serialization

        public XmlSchema GetSchema()
        {
            return null;
        }

        public virtual void ReadXml(XmlReader reader)
        {
            EditableModuleHelper.ReadProperties(this, reader);
        }

        public void WriteXml(XmlWriter writer)
        {
            EditableModuleHelper.WriteProperties(this, writer);
        }

        #endregion
    }
}
