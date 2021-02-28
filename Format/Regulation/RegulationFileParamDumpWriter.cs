using System.IO;
using System.Linq;
using SoulsFormats;
using SoulsParamsConverter.Format;

namespace SoulsParamsConverter
{
    public class RegulationFileParamDumpWriter : ParamDumpWriter
    {
        private BND4 Regulation { get; }

        private FileInfo RegulationPath { get;  }
        
        public RegulationFileParamDumpWriter(FileInfo path)
        {
            RegulationPath = path;
            Regulation = RegulationFile.Load(path);
        }

        public void Dispose()
        {
            if (RegulationPath.Extension == "bdt")
            {
                RegulationFile.EncryptDS3Regulation(RegulationPath.FullName, Regulation);
            }
            else
            {
                Regulation.Write(RegulationPath.FullName);
            }
        }

        public void Write(string name, PARAM param)
        {
            var existingParamFile = Regulation.Files
                .First(f => f.Name.EndsWith($"{name}.param"));
            var existingParam = PARAM.Read(existingParamFile.Bytes);
            existingParam.Rows = param.Rows;
            existingParam.ApplyParamdef(param.AppliedParamdef);

            existingParamFile.Bytes = existingParam.Write();
        }
    }
}