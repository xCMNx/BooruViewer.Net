using System;

namespace Booru.Core
{
    public interface IPageGeneratorSettings : IModuleSettings
    {
    }
    public interface IPageGeneratorSettingsEditor : IModuleSettingsEditor
    {
        Uri Host { get; }
    };
    public interface IPageGenerator : IModule
    {
        Uri GetNext(Uri Host, string Tags, byte[] Data, IPageGeneratorSettings Settings);
        Uri Init(Uri Host, string Tags, IPageGeneratorSettings Settings);
    }
}
