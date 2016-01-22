using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Booru.Core.Utils
{
    public static class Helpers
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AllocConsole();
        [DllImport("kernel32.dll")]
        public static extern bool FreeConsole();
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.Winapi)]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint msg, uint wParam, [MarshalAs(UnmanagedType.LPWStr)] string lParam);

        public const int EM_SETCUEBANNER = 0x1501;
        public const string LAST_CONNECTOR = "LastDataCntainer";
        public const string LAST_PREVIEWS_CONNECTOR = "LastPreviewContainer";
        public const string LAST_LoadManager = "LastLoadManager";

        public static string PageGeneratorsPath { get; private set; }
        public static string ParsersPath { get; private set; }
        public static string DataContainersPath { get; private set; }
        public static string PreviewContainersPath { get; private set; }
        public static string ManagersPath { get; private set; }
        public static string LoadersPath { get; private set; }
        public static string ProgramPath { get; private set; }
        public static string SettingsPath { get; private set; }

        static Configuration config;

        static Helpers()
        {
            ProgramPath = System.AppDomain.CurrentDomain.BaseDirectory;
            //config = ConfigurationManager.OpenExeConfiguration(Path.Combine(ProgramPath, ".exe.config"));
            config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var fn = config.FilePath.ToLower();
            if (fn.Contains(".vshost."))
                config = ConfigurationManager.OpenExeConfiguration(fn.Replace(".vshost.", ".").Replace(".config", null));
            SettingsPath = ReadFromConfig("SettingsPath") ?? System.IO.Path.Combine(ProgramPath, @"Settings");
            ParsersPath = ReadFromConfig("ParsersPath") ?? System.IO.Path.Combine(ProgramPath, @"Modules\Parsers");
            LoadersPath = ReadFromConfig("LoadersPath") ?? System.IO.Path.Combine(ProgramPath, @"Modules\Loaders");
            DataContainersPath = ReadFromConfig("DataContainersPath") ?? System.IO.Path.Combine(ProgramPath, @"Modules\DataContainers");
            PreviewContainersPath = ReadFromConfig("PreviewContainersPath") ?? System.IO.Path.Combine(ProgramPath, @"Modules\PreviewContainers");
            ManagersPath = ReadFromConfig("ManagersPath") ?? System.IO.Path.Combine(ProgramPath, @"Modules\Managers");
            PageGeneratorsPath = ReadFromConfig("PageGeneratorsPath") ?? System.IO.Path.Combine(ProgramPath, @"Modules\PageGenerators");
        }

        static bool ConsoleEnabled = false;
        static object ConsoleLocker = true;
        public static void ConsoleState(bool Enable)
        {
            lock (ConsoleLocker)
            {
                if (Enable != ConsoleEnabled)
                {
                    ConsoleEnabled = Enable;
                    if (Enable)
                        AllocConsole();
                    else
                        FreeConsole();
                }
            }
        }

        public static void ConsoleWrite(string Str, ConsoleColor clr = ConsoleColor.Gray)
        {
            lock (ConsoleLocker)
            {
                if (ConsoleEnabled)
                {
                    Console.ForegroundColor = clr;
                    Console.WriteLine(Str);
                }
            }
        }

        public static long TickCount
        {
            get { return System.Environment.TickCount & int.MaxValue; }
        }

        public static Type[] getModules(string InterfaceName, IEnumerable<Assembly> assemblies = null)
        {
            var asmbls = assemblies ?? AppDomain.CurrentDomain.GetAssemblies();
            List<Type> modules = new List<Type>();
            foreach (var assembly in asmbls)
            {
                var a_types = assembly.GetTypes();
                foreach (var type in a_types)
                    if (type.GetInterface(InterfaceName) != null)
                        modules.Add(type);
            }
            return modules.ToArray();
        }

        public static Type[] getModules(Type type, IEnumerable<Assembly> assemblies = null)
        {
            return getModules(type.FullName, assemblies);
        }

        public static Assembly LoadLibrary(string dllName)
        {
            try
            {
                return Assembly.LoadFrom(Path.GetFullPath(dllName));
            }
            catch (Exception e)
            {
                Helpers.ConsoleWrite("Error while loading " + dllName);
                Helpers.ConsoleWrite(e.Message);
                Helpers.ConsoleWrite(string.Empty);
            }
            return null;
        }

        public static Assembly[] LoadLibraries(string path, SearchOption Options = SearchOption.TopDirectoryOnly)
        {
            List<Assembly> assemblies = new List<Assembly>();
            if (Directory.Exists(path))
                foreach (var f in Directory.GetFiles(path, "*.dll", Options))
                {
                    var assembly = LoadLibrary(f);
                    if (assembly != null)
                        assemblies.Add(assembly);
                }
            return assemblies.ToArray();
        }
        /*
		public static void SetCueBanner(this TextBox textBox, string Text)
		{
			SendMessage(textBox.Handle, EM_SETCUEBANNER, 0, Text);
		}
		//*/
        public static string AssemblyDirectory(Assembly asmbl)
        {
            string codeBase = asmbl.CodeBase;
            return Path.GetDirectoryName(codeBase.Substring(8));
        }

        public static void WriteToConfig(string Key, string Value)
        {
            lock (config)
            {
                var k = config.AppSettings.Settings[Key];
                if (k == null)
                    config.AppSettings.Settings.Add(Key, Value);
                else
                    k.Value = Value;
                config.Save(ConfigurationSaveMode.Minimal);
            }
        }

        public static string ReadFromConfig(string Key, string Default = null, bool CreateRecord = false)
        {
            lock (config)
            {
                var r = config.AppSettings.Settings[Key];
                if (r == null)
                {
                    if (CreateRecord)
                        WriteToConfig(Key, Default);
                    return Default;
                }
                return r.Value;
            }
        }

        public static int TrueCnt(this BitArray bts)
        {
            var cnt = 0;
            for (int i = 0; i < bts.Count; i++)
                if (bts[i])
                    cnt++;
            return cnt;
        }

        public static byte[] ToBytes(this BitArray bts)
        {
            byte[] bytes = new byte[bts.Length / 8 + (bts.Length % 8 == 0 ? 0 : 1)];
            bts.CopyTo(bytes, 0);
            return bytes;
        }

        public static int ToStream(this BitArray bts, Stream Stream)
        {
            var size = bts.Length / 8 + (bts.Length % 8 == 0 ? 0 : 1);
            byte[] bytes = new byte[size];
            bts.CopyTo(bytes, 0);
            Stream.Write(bytes, 0, size);
            return size;
        }

        public static BitArray ToBits(this Stream Stream)
        {
            byte[] bytes = new byte[Stream.Length];
            Stream.Read(bytes, 0, bytes.Length);
            return new BitArray(bytes);
        }

        public static int intParseOrDefault(string s, int Default)
        {
            int val;
            if (!int.TryParse(s, out val))
                return Default;
            return val;
        }

    }
}
