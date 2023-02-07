using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Win32.SafeHandles;
using OfficeOpenXml;
using SoulsFormats;

namespace SoulsParamsConverter.Format.Excel
{
    public class ExcelParamDumpReader : ParamDumpReader
    {
        public ExcelPackage Spreadsheet { get; }

        public ExcelParamDumpReader(FileInfo path)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using var file = File.Open(path.FullName, FileMode.Open);

            Spreadsheet = new ExcelPackage();
            Spreadsheet.Load(file);
        }

        public void Dispose()
        {
            Spreadsheet.Dispose();
        }

        public PARAM Read(string name, PARAMDEF def)
        {
            
            var worksheet = Spreadsheet.Workbook.Worksheets.First(sheet => sheet.Name.Equals(name));
            var rowCount = worksheet.Dimension.Rows;

            var param = new PARAM();
            param.ParamType = def.ParamType;
            param.Rows = new List<PARAM.Row>(rowCount - 2);
            param.ApplyParamdef(def);
            
            for (var rowIndex = 3; rowIndex <= rowCount; rowIndex++)
            {
                var id = int.Parse(worksheet.Cells[rowIndex, 1].Value.ToString());
                var rowName = worksheet.Cells[rowIndex, 2].Value ?? string.Empty;
                var row = new PARAM.Row(id, (string?)rowName, def);

                for (var cellIndex = 0; cellIndex < def.Fields.Count; cellIndex++)
                {
                    var value = worksheet.Cells[rowIndex, 3 + cellIndex].Value;
                    if (value is string v && v == "-")
                    {
                        // padding, we don't store this in excel files
                        continue;
                    }

                    if (value is null)
                    {
                        Console.WriteLine($"Row ID {id} and field ${def.Fields[cellIndex].DisplayName} has null value, assuming default");
                        continue;
                    }
                    
                    row.Cells[cellIndex].Value = value;
                }
                
                param.Rows.Add(row);
            }

            return param;
        }

        public IEnumerable<ParamFileReference> List()
        {
            return Spreadsheet.Workbook.Worksheets.Select(worksheet =>
            {
                var name = worksheet.Name;
                var type = worksheet.Cells[1, 1].Comment.Text;

                return new ParamFileReference(type, name);
            });
        }
    }
}