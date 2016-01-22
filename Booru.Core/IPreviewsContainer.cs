using System.IO;

namespace Booru.Core
{
    public interface IPreviewsContainerSettings : IModuleSettings { }
    public interface IPreviewsContainer : IEditableModule
    {
        void setPreview(string md5, Stream data, string ext);
        Stream getPreview(string md5);
        void Stop();
        void WaitCompletition();
    }
}
