using System.Globalization;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;
using NPOI.HSSF.Record;
using SoulsFormats;

namespace SoulsParamsConverter.Format.Csv
{
    public class CsvParamDumpWriter : ParamDumpWriter
    {
        private readonly FileInfo _outputDirectory;

        public CsvParamDumpWriter(FileInfo outputDirectory)
        {
            _outputDirectory = outputDirectory;
            Directory.CreateDirectory(_outputDirectory.FullName);
        }

        public void Dispose()
        {
        }

        public static object GetFieldValue(PARAMDEF.Field Def, object value)
        {
            switch (Def.DisplayType)
            {
                case PARAMDEF.DefType.s8: return Convert.ToSByte(value); 
                case PARAMDEF.DefType.u8: return Convert.ToByte(value); 
                case PARAMDEF.DefType.s16: return Convert.ToInt16(value); 
                case PARAMDEF.DefType.u16: return Convert.ToUInt16(value); 
                case PARAMDEF.DefType.s32: return Convert.ToInt32(value); 
                case PARAMDEF.DefType.u32: return Convert.ToUInt32(value); 
                case PARAMDEF.DefType.f32: return Convert.ToSingle(value); 
                case PARAMDEF.DefType.fixstr: return Convert.ToString(value); 
                case PARAMDEF.DefType.fixstrW: return Convert.ToString(value); 

                default:
                    throw new NotImplementedException($"Conversion not specified for type {Def.DisplayType}");
            }
        }
        
        public void Write(string name, PARAM param)
        {
            using var csv = new CsvWriter(new StreamWriter($"{_outputDirectory.FullName}/{name}.csv"),
                CultureInfo.InvariantCulture);

            csv.WriteComment($"{name}={param.ParamType}");
            csv.NextRecord();
            csv.WriteField("ID");
            csv.WriteField("Name");

            foreach (var field in param.AppliedParamdef.Fields)
            {
                csv.WriteField($"{field.InternalName} - {field.DisplayName} - {field.InternalType}");
            }

            csv.NextRecord();

            foreach (var row in param.Rows)
            {
                csv.WriteField(row.ID);
                csv.WriteField(row.Name);

                foreach (var cell in row.Cells)
                {
                    if (cell.Def.DisplayType == PARAMDEF.DefType.dummy8)
                    {
                        if (cell.Def.DisplayName.Contains("pad"))
                        {
                            csv.WriteField("-");
                        }
                        else
                        {
                            
                            csv.WriteField(BitConverter.ToString((byte[]) cell.Value));
                        }
                    }
                    else
                    {
                        csv.WriteField(GetFieldValue(cell.Def, cell.Value));
                    }
                }

                csv.NextRecord();
            }
        }
    }
}