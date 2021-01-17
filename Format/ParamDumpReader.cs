using System;
using System.Collections.Generic;
using SoulsFormats;

namespace SoulsParamsConverter.Format
{
    public struct ParamFileReference
    {
        public string Name { get; }
        public string Type { get; }

        public ParamFileReference(string type, string name)
        {
            this.Type = type;
            this.Name = name;
        }
    }

    public interface ParamDumpReader : IDisposable
    {
        public PARAM Read(string name, PARAMDEF def);

        public IEnumerable<ParamFileReference> List();
    }
}