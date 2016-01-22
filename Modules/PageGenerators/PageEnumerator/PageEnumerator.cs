using System;
using System.IO;
using Booru.Core;

namespace Booru.Base.PageGenerators
{
    [SettingsType(typeof(PageEnumeratorSettings)), SettingsRequiered]
    public class PageEnumerator : IPageGenerator
    {
        public const string Default = "$Server/post/index.xml?page=" + EnumeratorMask + "&limit=1000&tags=$Tags";
        public const string EnumeratorMask = "%PageNumber%";
        public static string ApplyMask(string Host, string Tags, string mask, object value)
        {
            return mask?.Replace(EnumeratorMask, value?.ToString()).Replace("$Server", Host).Replace("$Tags", Tags);
        }

        public Uri GetNext(Uri Host, string Tags, byte[] Data, IPageGeneratorSettings Settings)
        {
            var settings = (PageEnumeratorSettings)Settings;
            if (!settings.CanContinue)
                return null;
            return new Uri(ApplyMask(Host.AbsoluteUri, Tags, settings.Mask, settings.Current += settings.Increment));
        }

        public Uri Init(Uri Host, string Tags, IPageGeneratorSettings Settings)
        {
            var settings = (PageEnumeratorSettings)Settings;
            return new Uri(ApplyMask(Host.AbsoluteUri, Tags, settings.Mask, settings.Current));
        }
    }
}