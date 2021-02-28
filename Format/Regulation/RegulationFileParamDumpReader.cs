using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SoulsFormats;
using SoulsParamsConverter.Format;

namespace SoulsParamsConverter
{
    public class RegulationFileParamDumpReader : ParamDumpReader
    {
        private Dictionary<string, PARAM> Params { get; }

        public RegulationFileParamDumpReader(FileInfo path)
        {
            var regulation = RegulationFile.Load(path);
          
            Params = regulation.Files
                .Where(f => f.Name.EndsWith(".param"))
                .ToDictionary(file => Path.GetFileNameWithoutExtension(file.Name), file => PARAM.Read(file.Bytes));
        }


        public void Dispose()
        {
        }

        public PARAM Read(string name, PARAMDEF def)
        {
            var param = Params[name];
            param.ApplyParamdef(def);

            return param;
        }

        public IEnumerable<ParamFileReference> List()
        {
            return Params.Select(entry => new ParamFileReference(entry.Value.ParamType, entry.Key));
        }
    }
}