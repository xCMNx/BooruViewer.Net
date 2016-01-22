using System.Xml;
using System.Xml.Serialization;
using Booru.Core;

namespace Booru.Base.Parsers
{
    [ModuleType(typeof(RegEx))]
    class RegExSettings : BindableBase, IParserSettings
    {
        public const string PageDataRegexp = "<span .*id=p(?<post>[\\d]*).*src=\"/(?<server>.*?)/.+?/.+?/.+?/.+?/(?<md5>[a-f\\d]+)*.?(?<ext>\\.[\\.\\w]*).*title=\"(?<tags>.+?)\".*</span>";

        protected string _Regexp = PageDataRegexp;
        public string Expression
        {
            get
            {
                return _Regexp;
            }
            set
            {
                _Regexp = value;
                NotifyPropertyChanged(nameof(Expression));
            }
        }

        public bool isConfigured => !string.IsNullOrWhiteSpace(_Regexp);

        public IModuleSettings Clone()
        {
            return EditableModuleHelper.Clone(this);
        }

        public void Reset()
        {
            Expression = PageDataRegexp;
        }

        public int CompareTo(object obj)
        {
            var pe = obj as RegExSettings;
            if (pe == null) return 1;
            return string.Compare(pe._Regexp, _Regexp);
        }

        #region Serialization

        System.Xml.Schema.XmlSchema IXmlSerializable.GetSchema()
        {
            return null;
        }

        void IXmlSerializable.ReadXml(XmlReader reader)
        {
            EditableModuleHelper.ReadProperties(this, reader);
        }

        void IXmlSerializable.WriteXml(XmlWriter writer)
        {
            EditableModuleHelper.WriteProperties(this, writer);
        }

        #endregion

    }
}
