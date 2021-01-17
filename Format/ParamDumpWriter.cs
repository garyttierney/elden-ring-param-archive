using System;
using SoulsFormats;

namespace SoulsParamsConverter.Format
{
    public interface ParamDumpWriter : IDisposable
    {
        public void Write(string name, PARAM param);
    }
}