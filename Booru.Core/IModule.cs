using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Booru.Core
{
    public class SettingsReadException : Exception { public SettingsReadException(string Message) : base(Message) { } };

    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class SettingsRequiered : System.Attribute { }

    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class SettingsType : System.Attribute
    {
        public Type Type;
        public SettingsType(Type SettingsType)
        {
            Type = SettingsType;
        }
    }

    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class ModuleType : System.Attribute
    {
        public Type Type;
        public ModuleType(Type ModuleType)
        {
            Type = ModuleType;
        }
    }

    public class EditorControlType : System.Attribute
    {
        public ControlType Type;
        public EditorControlType(ControlType ControlType)
        {
            Type = ControlType;
        }
    }

    /// <summary>
    /// Identifier of modules settings
    /// </summary>
    public interface IModuleSettings : IXmlSerializable, IComparable
    {
        /// <summary>
        /// Return a copy of settings
        /// </summary>
        /// <returns>Copy of instance</returns>
        IModuleSettings Clone();
        bool isConfigured { get; }
        void Reset();
    }

    public interface IModuleSettingsEditorBase : INotifyPropertyChanged
    {
        bool Changed { get; }
    }

    /// <summary>
    /// Editor that returned by IEditableModuleEditorProvider
    /// Must contain constructor that accept IModuleSettings as parameter
    /// </summary>
    public interface IModuleSettingsEditor : IModuleSettingsEditorBase
    {
        /// <summary>
        /// Return edited settings
        /// </summary>
        IModuleSettings Settings { get; }
    }

    /// <summary>
    /// Editor that returned by IEditableModuleMultivalueEditorProvider
    /// Must contain constructor that accept IModuleSettings[] as parameter
    /// </summary>
    public interface IModuleMultivalueSettingsEditor : IModuleSettingsEditorBase
    {
        /// <summary>
        /// Return edited settings list
        /// </summary>
        IList<IModuleSettings> Settings { get; }
    }

    /// <summary>
    /// Dummy interface to identify modules
    /// </summary>
    public interface IModule
    {
    }

    /// <summary>
    /// Customizeble module.
    /// Must contain constructor that accept IModuleSettings as parameter
    /// example : public SomeModule(ModuleSettings Settings)
    /// </summary>
    public interface IEditableModule : IModule
    {
        /// <summary>
        /// Current module settings
        /// </summary>
        IModuleSettings CurrentSettings { get; }
    }

}
