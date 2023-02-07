using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using NPOI.Util;
using SoulsFormats;

namespace SoulsParamsConverter.Format.Csv
{
    public class CsvParamDumpReader : ParamDumpReader
    {
        private readonly FileInfo _inputDirectory;

        public CsvParamDumpReader(FileInfo inputDirectory)
        {
            _inputDirectory = inputDirectory;
        }

        public void Dispose()
        {
        }

        public PARAM Read(string name, PARAMDEF def)
        {
            var param = new PARAM();
            param.ParamType = def.ParamType;
            param.Rows = new List<PARAM.Row>();
            param.ApplyParamdef(def);

            var path = $"{_inputDirectory.FullName}/{name}.csv";
            using (var csv = new CsvReader(new StreamReader(path),
                CultureInfo.InvariantCulture))
            {
                
                if (!csv.Read())
                {
                    throw new RuntimeException("Unable to read type comment");
                }

                if (!csv.Read() || !csv.ReadHeader())
                {
                    return param;
                }
            
                while (csv.Read())
                {
                    var id = csv.GetField<int>(0);
                    var rowName = csv.GetField<string>( 1);
                    var row = new PARAM.Row(id, rowName, def);

                    for (var cellIndex = 0; cellIndex < def.Fields.Count; cellIndex++)
                    {
                        var cellValue = csv.GetField<string>(cellIndex + 2);
                        if (cellValue == "-")
                        {
                            // padding, we don't store this in excel files
                            continue;
                        }

                        if (def.Fields[cellIndex].DisplayType == PARAMDEF.DefType.dummy8)
                        {
                            String[] arr=cellValue.Split('-');
                            byte[] array=new byte[arr.Length];
                            for(int i=0; i<arr.Length; i++) array[i]=Convert.ToByte(arr[i],16);
                            
                            row.Cells[cellIndex].Value = array;
                        }
                        else
                        {
                            row.Cells[cellIndex].Value = cellValue;
                        }
                    }

                    param.Rows.Add(row);
                }

            }

            return param;
        }

        public IEnumerable<ParamFileReference> List()
        {
            var files = Directory.GetFiles(_inputDirectory.FullName, "*.csv");

            return files.Select(file =>
            {
                using var reader = new CsvReader(new StreamReader(file), CultureInfo.InvariantCulture);

                if (!reader.Read() || !reader.Context.Record[0].StartsWith("#"))
                {
                    throw new RuntimeException($"Unable to get param type from CSV file: {file}");
                }

                var line = reader.Context.Record[0];
                var fields = line.TrimStart('#').Trim().Split("=");

                return new ParamFileReference(fields[1], fields[0]);
            });
        }
    }
}