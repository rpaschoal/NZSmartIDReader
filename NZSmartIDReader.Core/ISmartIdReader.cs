using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NZSmartIDReader.Core
{
    public interface ISmartIdReader
    {
        bool IsInitialized { get; }

        bool IsValid(Stream imageData);

        string Read();
    }
}
