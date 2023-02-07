﻿using System;
using System.Drawing;
using System.IO;
using NPOI.SS.Formula.Functions;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using SoulsFormats;

namespace SoulsParamsConverter.Format.Excel
{
    static class FieldExtensions
    {
        public static int CalculateBitSize(this PARAMDEF.Field field)
        {
            if (field.BitSize != -1)
            {
                return field.BitSize;
            }

            var byteSize = field.DisplayType switch
            {
                PARAMDEF.DefType.s8 => 1,
                PARAMDEF.DefType.u8 => 1,
                PARAMDEF.DefType.s16 => 2,
                PARAMDEF.DefType.u16 => 2,
                PARAMDEF.DefType.s32 => 4,
                PARAMDEF.DefType.u32 => 4,
                PARAMDEF.DefType.f32 => 4,
                PARAMDEF.DefType.dummy8 => 1,
                PARAMDEF.DefType.fixstr => 1,
                PARAMDEF.DefType.fixstrW => 2,
                _ => throw new ArgumentOutOfRangeException()
            };

            return byteSize * 8 * field.ArrayLength;
        }
    }

    public class ExcelParamDumpWriter : ParamDumpWriter
    {
        public ExcelPackage Spreadsheet { get; }

        public ExcelParamDumpWriter(FileInfo path)
        {
            Spreadsheet = new ExcelPackage(path);
        }

        public void Dispose()
        {
            Spreadsheet.Save();
        }

        public void Write(string name, PARAM param)
        {
            Console.WriteLine(name);

            var worksheet = Spreadsheet.Workbook.Worksheets.Add(name);
            worksheet.HeaderFooter.FirstFooter.RightAlignedText = param.ParamType;

            worksheet.Cells[1, 1].Value = "ID";
            worksheet.Cells[1, 2].Value = "Name";

            for (var rowIndex = 0; rowIndex < param.Rows.Count; rowIndex++)
            {
                var row = param.Rows[rowIndex];
                worksheet.Cells[rowIndex + 3, 1].Value = row.ID;
                worksheet.Cells[rowIndex + 3, 2].Value = row.Name;

                for (var cellIndex = 0; cellIndex < row.Cells.Count; cellIndex++)
                {
                    var cell = worksheet.Cells[rowIndex + 3, cellIndex + 3];
                    var value = row.Cells[cellIndex].Value;

                    if (value is byte[])
                    {
                        cell.Value = "-";
                    }
                    else
                    {
                        cell.Value = value;
                    }
                }
            }

            var bitSize = 0;
            var fields = param.AppliedParamdef.Fields;

            for (var columnIndex = 0; columnIndex < fields.Count; columnIndex++)
            {
                using var cells = worksheet.Cells[3, columnIndex + 3, param.Rows.Count + 2, columnIndex + 3];

                var style = cells.Style;
                var field = fields[columnIndex];
                var fieldOffset = (bitSize / 8).ToString("X");
                var fieldBitOffset = bitSize % 8;

                if (fieldBitOffset != 0 || field.BitSize == 1)
                {
                    fieldOffset = fieldOffset + $":{fieldBitOffset}";
                }
                
                if (field.BitSize == 1)
                {
                    style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                    var trueFormatter = worksheet.ConditionalFormatting.AddEqual(new ExcelAddress(cells.Address));
                    trueFormatter.Formula = "1";
                    trueFormatter.Style.Fill.BackgroundColor.Color = Color.LightGreen;
                    trueFormatter.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;

                    var falseFormatter = worksheet.ConditionalFormatting.AddEqual(new ExcelAddress(cells.Address));
                    falseFormatter.Formula = "0";
                    falseFormatter.Style.Fill.BackgroundColor.Color = Color.LightPink;
                    falseFormatter.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                }
                else if (field.DisplayType == PARAMDEF.DefType.fixstr ||
                         field.DisplayType == PARAMDEF.DefType.fixstrW)
                {
                    style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
                }
                else
                {
                    style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                }

                worksheet.Cells[2, columnIndex + 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[2, columnIndex + 3].Value = $"{field.DisplayName}";
                worksheet.Cells[1, columnIndex + 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[1, columnIndex + 3].Value = $"0x{fieldOffset}";

                bitSize += field.CalculateBitSize();
            }

            for (var columnIndex = 0; columnIndex < fields.Count + 2; columnIndex++)
            {
                worksheet.Column(columnIndex + 1).AutoFit();
            }

            worksheet.View.FreezePanes(3, 3);
        }
    }
}