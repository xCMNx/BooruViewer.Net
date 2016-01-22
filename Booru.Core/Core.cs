using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;
using Booru.Core.Utils;

namespace Booru.Core
{
    public enum ControlType { WindowsForms, Xaml, Any }
    public class ControlTypeNotImplementedException : Exception
    {
        public ControlTypeNotImplementedException(ControlType cType, Type ContainerType) : base(string.Format($"Control for {cType} is not implemented in module {ContainerType.FullName}.")) { }
    }

    public class TypesComparer : Comparer<Type>
    {
        public override int Compare(Type t1, Type t2)
        {
            return string.Compare(t1?.FullName, t2?.FullName);
        }

        public static readonly TypesComparer Comparer = new TypesComparer();
    }

    public static class Core
    {
        public static SynchronizationContext MainContext = SynchronizationContext.Current;

        public static void MainContextSend(Action A) => MainContext.Send(_ => A(), null);
        public static void MainContextPost(Action A) => MainContext.Post(_ => A(), null);
        public static TResult MainContextGet<TResult>(Func<TResult> A) where TResult : class
        {
            TResult res = null;
            MainContext.Send(_ => res = A(), null);
            return res;
        }

        public static CoreNotifier Notifier = new CoreNotifier();

        public static event PropertyChangedEventHandler PropertyChanged;

        public static void NotifyPropertyChanged(String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(null, new PropertyChangedEventArgs(propertyName));
            }
        }

        public static void NotifyPropertiesChanged(params String[] propertyNames)
        {
            if (PropertyChanged != null)
            {
                foreach (var prop in propertyNames)
                    PropertyChanged(null, new PropertyChangedEventArgs(prop));
            }
        }

        public static CancellationTokenSource globalCTS = new CancellationTokenSource();
        public static Type[] DataContainerTypes { get; private set; }
        public static Type[] ManagersTypes { get; private set; }
        public static Type[] LoadersTypes { get; private set; }
        public static Type[] ParsersTypes { get; private set; }
        public static Type[] PageGeneratorsTypes { get; private set; }
        public static Type[] PreviewsContainerTypes { get; private set; }

        static SortedList<Type, IPageGenerator> Generators = new SortedList<Type, IPageGenerator>(TypesComparer.Comparer);
        static SortedList<Type, IDataParser> Parsers = new SortedList<Type, IDataParser>(TypesComparer.Comparer);

        public static SortedItems<Type> TypesContainer { get; private set; } = new SortedItems<Type>() { Comparer = TypesComparer.Comparer };

        static SortedList<Type, SortedList<ControlType, Type>> SettingsToEditorTypes = new SortedList<Type, SortedList<ControlType, Type>>(TypesComparer.Comparer);
        static SortedList<Type, SortedList<ControlType, Type>> SettingsToMultivalueEditorTypes = new SortedList<Type, SortedList<ControlType, Type>>(TypesComparer.Comparer);
        static SortedList<Type, Type> Pairs = new SortedList<Type, Type>(TypesComparer.Comparer);
        static SortedItems<Type> TypesRequieredSettings = new SortedItems<Type>() { Comparer = TypesComparer.Comparer };

        public static bool IsSettingsRequiered(Type type)
        {
            return TypesRequieredSettings.Contain(type);
        }

        public static Type FindTypeByName(string FullName)
        {
            return TypesContainer.FirstOrDefault(t => t.FullName == FullName);
        }

        public static string LastConnectorTypeName
        {
            get { return Helpers.ReadFromConfig(Helpers.LAST_CONNECTOR); }
            private set
            {
                Helpers.WriteToConfig(Helpers.LAST_CONNECTOR, value);
                NotifyPropertyChanged(nameof(LastConnectorTypeName));
            }
        }
        public static string LastPreviewsConnectorTypeName
        {
            get { return Helpers.ReadFromConfig(Helpers.LAST_PREVIEWS_CONNECTOR); }
            private set
            {
                Helpers.WriteToConfig(Helpers.LAST_PREVIEWS_CONNECTOR, value);
                NotifyPropertyChanged(nameof(LastPreviewsConnectorTypeName));
            }
        }
        public static string LastDataManagerTypeName
        {
            get { return Helpers.ReadFromConfig(Helpers.LAST_LoadManager); }
            private set
            {
                Helpers.WriteToConfig(Helpers.LAST_LoadManager, value);
                NotifyPropertyChanged(nameof(LastDataManagerTypeName));
            }
        }

        static IDataContainer _DataContainer;
        public static IDataContainer DataContainer
        {
            get { return _DataContainer; }
            set
            {
                if (_DataContainer == null)
                    _DataContainer = value;
                LastConnectorTypeName = _DataContainer == null ? null : _DataContainer.GetType().ToString();
                NotifyPropertiesChanged(nameof(DataContainer), nameof(DataContainerType));
            }
        }

        public static Type DataContainerType
        {
            get
            {
                return _DataContainer?.GetType();
            }
        }

        static IPreviewsContainer _PreviewsContainer;
        public static IPreviewsContainer PreviewsContainer
        {
            get { return _PreviewsContainer; }
            set
            {
                _PreviewsContainer = value;
                LastPreviewsConnectorTypeName = _PreviewsContainer == null ? null : _PreviewsContainer.GetType().ToString();
                NotifyPropertiesChanged(nameof(PreviewsContainer), nameof(PreviewsContainerType));
            }
        }

        public static Type PreviewsContainerType
        {
            get
            {
                return _PreviewsContainer?.GetType();
            }
        }

        static IDataLoadManager _DataLoadManager;
        public static IDataLoadManager DataLoadManager
        {
            get { return _DataLoadManager; }
            set
            {
                LastDataManagerTypeName = value?.GetType().ToString();
                if (_DataLoadManager == null)
                {
                    _DataLoadManager = value;
                    NotifyPropertiesChanged(nameof(DataLoadManager), nameof(DataLoadManagerType));
                }
            }
        }

        public static Type DataLoadManagerType
        {
            get
            {
                return _DataLoadManager?.GetType();
            }
        }

        static T InitModule<T>(string Name, IEnumerable<Type> modules, string SettingsName = "default") where T : class, IEditableModule
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(Name))
                {
                    var type = modules.FirstOrDefault(c => c.FullName.Equals(Name));
                    if (type != null)
                    {
                        var settings = SettingsFor(type, SettingsName);
                        var tmp = (T)Activator.CreateInstance(type, settings.Count == 0 ? null : settings[0]);
                        return tmp;
                    }
                }
            }
            catch (Exception e)
            {
                Helpers.ConsoleWrite(e.ToString(), ConsoleColor.Red);
            }
            return null;
        }

        public static readonly ObservableCollection<IDataLoader> DataLoaders = new ObservableCollection<IDataLoader>();

        static void AddEditor(SortedList<Type, SortedList<ControlType, Type>> List, Type SettingsType, ControlType cType, Type EditorType)
        {
            SortedList<ControlType, Type> lst;
            if (!List.TryGetValue(SettingsType, out lst))
                List.Add(SettingsType, lst = new SortedList<ControlType, Type>());
            lst.Add(cType, EditorType);
        }

        static void FindPairsForSettings(string InterfaceName, IEnumerable<Assembly> Assemblies)
        {
            var Types = Helpers.getModules(InterfaceName, Assemblies);
            foreach (var type in Types)
                try
                {
                    TypesContainer.Add(type);
                    var attribute = type.GetCustomAttribute<SettingsType>();
                    if (attribute != null)
                    {
                        switch (InterfaceName)
                        {
                            case nameof(IModule):
                            case nameof(IEditableModule):
                                Pairs.Add(type, attribute.Type);
                                TypesContainer.Add(attribute.Type);
                                break;
                            case nameof(IModuleSettingsEditor):
                            case nameof(IModuleMultivalueSettingsEditor):
                                var ctAttribute = type.GetCustomAttribute<EditorControlType>();
                                if (ctAttribute != null && ctAttribute.Type != ControlType.Any)
                                    AddEditor(InterfaceName == nameof(IModuleSettingsEditor) ? SettingsToEditorTypes : SettingsToMultivalueEditorTypes, attribute.Type, ctAttribute.Type, type);
                                else
                                    Helpers.ConsoleWrite($"Editor {type.FullName} doesn't contain ControlType attribute.", ConsoleColor.Red);
                                break;
                        }
                    }
                    if (type.GetCustomAttribute<SettingsRequiered>() != null)
                        TypesRequieredSettings.Add(type);
                }
                catch (Exception ex)
                {
                    Helpers.ConsoleWrite(ex.ToString(), ConsoleColor.Red);
                }
        }

        public static void FindPairs(IEnumerable<Assembly> Assemblies)
        {
            FindPairsForSettings(nameof(IModule), Assemblies);
            FindPairsForSettings(nameof(IModuleSettingsEditor), Assemblies);
            FindPairsForSettings(nameof(IModuleMultivalueSettingsEditor), Assemblies);

            var Types = Helpers.getModules(typeof(IModuleSettings), Assemblies);
            foreach (var type in Types)
                try
                {
                    TypesContainer.Add(type);
                    var attribute = type.GetCustomAttribute<ModuleType>();
                    if (attribute != null)
                    {
                        Pairs.Add(type, attribute.Type);
                        TypesContainer.Add(attribute.Type);
                    }
                }
                catch (Exception ex)
                {
                    Helpers.ConsoleWrite(ex.ToString(), ConsoleColor.Red);
                }
        }

        static Core()
        {
            try
            {
                var parsers = Helpers.LoadLibraries(Helpers.ParsersPath, SearchOption.AllDirectories);
                ParsersTypes = Helpers.getModules(typeof(IDataParser), parsers);
                FindPairs(parsers);
                foreach (var pt in ParsersTypes)
                    Parsers.Add(pt, (IDataParser)Activator.CreateInstance(pt));

                var pageGenerators = Helpers.LoadLibraries(Helpers.PageGeneratorsPath, SearchOption.AllDirectories);
                PageGeneratorsTypes = Helpers.getModules(typeof(IPageGenerator), pageGenerators);
                FindPairs(pageGenerators);
                foreach (var pt in PageGeneratorsTypes)
                    Generators.Add(pt, (IPageGenerator)Activator.CreateInstance(pt));

                var loaders = Helpers.LoadLibraries(Helpers.LoadersPath, SearchOption.AllDirectories);
                LoadersTypes = Helpers.getModules(typeof(IDataLoader), loaders);
                FindPairs(loaders);
                InitLoaders();
                DataLoaders.CollectionChanged += DataLoaders_CollectionChanged;

                var DataContainers = Helpers.LoadLibraries(Helpers.DataContainersPath, SearchOption.AllDirectories);
                DataContainerTypes = Helpers.getModules(typeof(IDataContainer), DataContainers);
                FindPairs(DataContainers);
                DataContainer = InitModule<IDataContainer>(LastConnectorTypeName, DataContainerTypes);

                var PreviewsContainers = Helpers.LoadLibraries(Helpers.PreviewContainersPath, SearchOption.AllDirectories);
                PreviewsContainerTypes = Helpers.getModules(typeof(IPreviewsContainer), PreviewsContainers);
                FindPairs(PreviewsContainers);
                PreviewsContainer = InitModule<IPreviewsContainer>(LastPreviewsConnectorTypeName, PreviewsContainerTypes);

                var managers = Helpers.LoadLibraries(Helpers.ManagersPath, SearchOption.AllDirectories);
                ManagersTypes = Helpers.getModules(typeof(IDataLoadManager), managers);
                FindPairs(managers);
                DataLoadManager = InitModule<IDataLoadManager>(LastDataManagerTypeName, ManagersTypes);
                if (_DataLoadManager != null)
                    DataLoaders = new ObservableCollection<IDataLoader>(Core.ReadLoaders(DataLoadManagerType));
                //to do: Check editors and settings existens for modules requiered settings 
            }
            catch (Exception)
            {
            }
        }

        public static IPageGenerator GetPageGenerator(Type PageGeneratorType)
        {
            IPageGenerator generator;
            if (Generators.TryGetValue(PageGeneratorType, out generator))
                return generator;
            return null;
        }

        public static IDataParser GetParser(Type ParserType)
        {
            IDataParser parser;
            if (Parsers.TryGetValue(ParserType, out parser))
                return parser;
            return null;
        }

        private static void DataLoaders_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            WriteLoaders();
        }

        static int _srvCounter = 0;
        static SortedList<string, int> _srvIndexing = new SortedList<string, int>();
        static int[] _srvIdList = new int[0];

        public static int[] ServersID { get { return _srvIdList; } }

        static object _srvIdxLock = new object();
        static public int getServerID(Uri uri)
        {
            lock (_srvIdxLock)
            {
                int idx;
                if (!_srvIndexing.TryGetValue(uri.Host, out idx))
                {
                    _srvIndexing.Add(uri.Host, idx = _srvCounter++);
                    _srvIdList = _srvIndexing.Select(it => it.Value).ToArray();
                }
                return idx;
            }
        }

        static public int getServerID(string host)
        {
            lock (_srvIdxLock)
            {
                int idx;
                if (_srvIndexing.TryGetValue(host, out idx))
                    return idx;
                return -1;
            }
        }

        public static void Terminate()
        {
            globalCTS.Cancel();
            DataContainer?.WaitCompletition();
            PreviewsContainer?.WaitCompletition();
        }

        public static Type FindSettingsTypeFor(Type sometype)
        {
            if (sometype == null || typeof(IModuleSettings).IsAssignableFrom(sometype))
                return sometype;
            Type tmp;
            Pairs.TryGetValue(sometype, out tmp);
            return tmp;
        }

        /// <summary>
        /// Find and read settings for given module or its settings type
        /// </summary>
        /// <param name="ModuleOrSettingsType">Module or settings type</param>
        /// <param name="Name">Name of settings it should be * to find all settings availiable for this module</param>
        /// <returns></returns>
        public static IList<IModuleSettings> SettingsFor(Type ModuleOrSettingsType, string Name = "*")
        {
            List<IModuleSettings> lst = new List<IModuleSettings>();
            var SettingsType = FindSettingsTypeFor(ModuleOrSettingsType);
            if (SettingsType == null) return lst;
            if (!Directory.Exists(Helpers.SettingsPath)) return lst;
            foreach (var file in System.IO.Directory.EnumerateFiles(Helpers.SettingsPath, SettingsType.FullName + $"[{Name}].xml"))
                try
                {
                    XmlSerializer serializer = new XmlSerializer(SettingsType);
                    using (Stream reader = new FileStream(file, FileMode.Open))
                    {
                        lst.Add((IModuleSettings)serializer.Deserialize(reader));
                    }
                }
                catch (Exception e)
                {
                    Helpers.ConsoleWrite(e.Message, ConsoleColor.Red);
                }
            return lst;
        }

        public static Type EditorTypeFor(Type ModuleOrSettingsType, ControlType cType, bool Multivalue = false)
        {
            var SettingsType = FindSettingsTypeFor(ModuleOrSettingsType);
            if (SettingsType == null) return null;
            SortedList<ControlType, Type> tmp;
            Type EditorType;
            if ((Multivalue ? SettingsToMultivalueEditorTypes : SettingsToEditorTypes).TryGetValue(SettingsType, out tmp))
                if (cType == ControlType.Any)
                    return tmp[0];
                else if (tmp.TryGetValue(cType, out EditorType))
                    return EditorType;
            return null;
        }

        public static void ClearLoaders(Type LodersType)
        {
            for (int i = DataLoaders.Count - 1; i >= 0; i--)
                if (DataLoaders[i].GetType() == LodersType)
                    DataLoaders.RemoveAt(i);
        }

        public static void AddLoaders(IEnumerable<IDataLoader> Loaders)
        {
            DataLoaders.Clear();
            foreach (var loader in Loaders)
                DataLoaders.Add(loader);
        }

        public static void InitLoaders()
        {
            if (DataLoadManager != null && DataLoadManagerType != null)
            {
                DataLoaders.Clear();
                var lst = ReadLoaders(DataLoadManagerType);
                foreach (var loader in lst)
                    DataLoaders.Add(loader);
            }
        }

        static Type FindType(this IEnumerable<Type> Types, string TypeFullName) => Types.FirstOrDefault(t => t.FullName == TypeFullName);

        public static IList<IDataLoader> ReadLoaders(Type ManagerType)
        {
            return ReadLoaders(Path.Combine(Helpers.SettingsPath, $"Loaders[{ManagerType.FullName}].xml"));
        }

        static IList<IDataLoader> ReadLoaders(string FileName)
        {
            var res = new List<IDataLoader>();
            if (File.Exists(FileName))
            {
                using (XmlReader Reader = new XmlTextReader(FileName))
                {
                    while (Reader.Read())
                    {
                        if (Reader.Name == "Loaders")
                        {
                            while (Reader.Read())
                                switch (Reader.NodeType)
                                {
                                    case XmlNodeType.Element:
                                        {
                                            var ldrtype = LoadersTypes.FindType(Reader.Name);
                                            if (ldrtype == null)
                                                EditableModuleHelper.SkipElement(Reader);
                                            else
                                            {
                                                var ldrsettingstype = FindSettingsTypeFor(ldrtype);
                                                IModuleSettings settings = null;
                                                if (ldrsettingstype != null)
                                                {
                                                    Reader.Read();
                                                    XmlSerializer serializer = new XmlSerializer(ldrsettingstype);
                                                    settings = (IModuleSettings)serializer.Deserialize(Reader);
                                                    Reader.Read();
                                                }
                                                try
                                                {
                                                    var ldr = (IDataLoader)Activator.CreateInstance(ldrtype, settings);
                                                    res.Add(ldr);
                                                }
                                                catch (Exception e)
                                                {
                                                    Helpers.ConsoleWrite(e.Message, ConsoleColor.Red);
                                                }
                                                Reader.Read();
                                            }
                                        }
                                        break;
                                    case XmlNodeType.EndElement: break;
                                }
                        }
                        else
                            EditableModuleHelper.SkipElement(Reader);
                    }
                }
            }
            return res;
        }

        public static void WriteLoaders(IEnumerable<IDataLoader> Loaders, string FileName)
        {
            using (XmlWriter Writer = new XmlTextWriter(FileName, System.Text.Encoding.UTF8))
            {
                Writer.WriteStartElement("Loaders");
                foreach (IDataLoader ldr in Loaders)
                {
                    Writer.WriteStartElement(ldr.GetType().FullName);
                    XmlSerializer serializer = new XmlSerializer(ldr.CurrentSettings.GetType());
                    serializer.Serialize(Writer, ldr.CurrentSettings);
                    Writer.WriteEndElement();
                }
                Writer.WriteEndElement();
            }
        }

        public static void WriteLoaders(IEnumerable<IDataLoader> Loaders, Type ManagerType)
        {
            if (ManagerType == null) return;
            Directory.CreateDirectory(Helpers.SettingsPath);
            WriteLoaders(DataLoaders, Path.Combine(Helpers.SettingsPath, $"Loaders[{ManagerType.FullName}].xml"));
        }

        public static void WriteLoaders()
        {
            WriteLoaders(DataLoaders, DataLoadManagerType);
        }

        public static void SaveSettings(IModuleSettings settings, string Name = "default")
        {
            if (settings != null)
            {
                var type = settings.GetType();
                System.IO.Directory.CreateDirectory(Helpers.SettingsPath);
                var name = Path.Combine(Helpers.SettingsPath, $"{type.FullName}[{Name}].xml");
                XmlSerializer serializer = new XmlSerializer(type);
                using (Stream writer = new FileStream(name, FileMode.Create))
                    serializer.Serialize(writer, settings);
            }
        }
    }

    public class CoreNotifier : INotifyPropertyChanged
    {
        public string LastConnectorTypeName => Core.LastConnectorTypeName;
        public string LastDataManagerTypeName => Core.LastDataManagerTypeName;
        public IDataContainer DataContainer
        {
            get { return Core.DataContainer; }
            set { Core.DataContainer = value; }
        }
        public Type DataContainerType => Core.DataContainerType;
        public IDataLoadManager DataLoadManager
        {
            get { return Core.DataLoadManager; }
            set { Core.DataLoadManager = value; }
        }
        public Type DataLoadManagerType => Core.DataLoadManagerType;

        public event PropertyChangedEventHandler PropertyChanged;

        public CoreNotifier()
        {
            Core.PropertyChanged += Core_PropertyChanged;
        }

        private void Core_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, e);
        }
    }
}
