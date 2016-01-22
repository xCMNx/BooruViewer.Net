using System;
using System.Collections.Generic;

namespace Booru.Core
{
    public class ParserException : Exception { public ParserException(Exception inner) : base(inner?.Message, inner) { } }

    public interface IParserSettings : IModuleSettings
    {
    }
    public interface IDataParser : IModule
    {
        IList<DataRecord> GetData(byte[] Data, string Host, IParserSettings Settings);
    }
}
