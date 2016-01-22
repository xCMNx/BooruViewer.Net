using System.Xml;
using System.Xml.Serialization;
using Booru.Core;

namespace Booru.Base.PageGenerators
{
    [ModuleType(typeof(PageEnumerator))]
    public class PageEnumeratorSettings : BindableBase, IPageGeneratorSettings
    {
        public string _Mask = PageEnumerator.Default;
        public int _Current = 0;
        public int _Increment = 1;
        public int _StopAt = int.MaxValue;

        public bool isConfigured => (!string.IsNullOrWhiteSpace(_Mask) && _Mask.Contains(PageEnumerator.EnumeratorMask)) && _Increment != 0 && CanContinue;

        public string Mask
        {
            get { return _Mask; }
            set
            {
                _Mask = value;
                NotifyPropertyChanged(nameof(Mask));
            }
        }
        public int Current
        {
            get { return _Current; }
            set
            {
                _Current = value;
                NotifyPropertyChanged(nameof(Current));
            }
        }
        public int Increment
        {
            get { return _Increment; }
            set
            {
                _Increment = value;
                NotifyPropertyChanged(nameof(Increment));
            }
        }
        public int StopAt
        {
            get { return _StopAt; }
            set
            {
                _StopAt = value;
                NotifyPropertyChanged(nameof(StopAt));
            }
        }

        [XmlIgnore]
        public bool CanContinue => (Increment < 0 && Current >= StopAt) || (Increment > 0 && Current <= StopAt);

        public void Reset()
        {
            Current = 0;
            Increment = 1;
            StopAt = int.MaxValue;
        }

        public IModuleSettings Clone()
        {
            return EditableModuleHelper.Clone(this);
        }

        public int CompareTo(object obj)
        {
            var pe = obj as PageEnumeratorSettings;
            if (pe == null) return 1;
            return string.Compare(pe.Mask, Mask);
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
