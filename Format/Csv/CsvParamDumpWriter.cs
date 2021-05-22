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
                csv.WriteField(field.DisplayName);
            }

            csv.NextRecord();

            foreach (var row in param.Rows)
            {
                csv.WriteField(row.ID);
                csv.WriteField(row.Name);

                foreach (var cell in row.Cells)
                {
                    if (cell.Value is byte[])
                    {
                        csv.WriteField("-");
                    }
                    else
                    {
                        csv.WriteField(cell.Value);
                    }
                }

                csv.NextRecord();
            }
        }
    }
}