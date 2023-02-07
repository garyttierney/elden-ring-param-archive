using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using SoulsFormats;
using SoulsParamsConverter.Format.Excel;

namespace SoulsParamsConverter.Commands
{
    public abstract class CField
    {
        public int Size { get; protected set; }
        public string Type { get; protected set; }
    }

    public class CScalar : CField
    {
        public string Name { get; }

        public string Description { get; }
        public CScalar(string name, string description, string type, int size)
        {
            Name = name;
            Description = description;
            Size = size;
            Type = type;
        }
    }

    public class CBitfield : CField
    {
        public List<PARAMDEF.Field> Fields = new List<PARAMDEF.Field>();

        public CBitfield(string type, int size)
        {
            Size = size;
            Type = type;
        }
    }

    
  
    
    public class ParamsSchemaGenerator
    {

        public static string FieldName(PARAMDEF.Field field) {
            if (!string.IsNullOrEmpty(field.UnkB8))
            {
                return
                    $"{field.UnkB8}{field.InternalName.First().ToString().ToUpper() + field.InternalName.Substring(1)}";
            }
            else
            {
                return field.InternalName;
            }
        }

        public static string FieldDescription(PARAMDEF.Field field) => string.IsNullOrEmpty(field.Description)
            ? field.DisplayName
            : field.Description;
        public static string ToSnakeCase(string camelCase)
        {
            return Regex.Replace(camelCase, @"(\G(?!^)|\b[a-zA-Z][a-z]*)([A-Z][a-z]*|\d+)", (match) =>
            {
                return match.Groups[1].Value.ToUpper() + "_" + match.Groups[2].Value;
            });
        }
        public static void Run(Game game, FileInfo paramdexPath, Regex typeFilter)
        {
            using StreamWriter sw = File.CreateText(@"C:\Development\workspace\ds3-param-dump\out.rs") ;

            var identifierRegex = new Regex("[_%a][_%w]*");
            var paramdefPaths = Directory.GetFiles($"{paramdexPath.FullName}/{game}/Defs", "*.xml");

            foreach (var paramdefPath in paramdefPaths)
            {
                try
                {
                    var paramdef = PARAMDEF.XmlDeserialize(paramdefPath);
                    if (!typeFilter.IsMatch(paramdef.ParamType))
                    {
                        continue;
                    }

                    var bitSize = 0;

                    var cfields = new List<CField>();
                    for (var index = 0; index < paramdef.Fields.Count; index++)
                    {
                        var field = paramdef.Fields[index];
                        var byteSize = field.CalculateByteSize();
                        var ctype = field.DisplayType switch
                        {
                            PARAMDEF.DefType.s8 => "i8",
                            PARAMDEF.DefType.u8 => "u8",
                            PARAMDEF.DefType.s16 => "i16",
                            PARAMDEF.DefType.u16 => "u16",
                            PARAMDEF.DefType.s32 => "i32",
                            PARAMDEF.DefType.u32 => "u32",
                            PARAMDEF.DefType.f32 => "f32",
                            PARAMDEF.DefType.dummy8 => $"[u8; {byteSize}]",
                            PARAMDEF.DefType.fixstr => null,
                            PARAMDEF.DefType.fixstrW => null,
                            _ => throw new ArgumentOutOfRangeException()
                        };

                        if (field.BitSize != -1)
                        {
                            var bitfield = new CBitfield(ctype, byteSize);
                            do
                            {
                                bitfield.Fields.Add(paramdef.Fields[index]);
                                index++;
                            } while (paramdef.Fields[index + 1]?.BitSize != -1);

                            cfields.Add(bitfield);
                        }
                        else
                        {
                            var scalar = new CScalar(FieldName(field), FieldDescription(field), ctype, byteSize);
                            cfields.Add(scalar);
                        }
                    }

                    var bitfieldIndex = 0;
                    foreach (var field in cfields)
                    {
                        if (field is CBitfield bitfield)
                        {
                            sw.WriteLine(@$"
                            bitflags! {{
                                struct {paramdef.ParamType}Bitfield{bitfieldIndex++} : {field.Type} {{
                                    {string.Join('\n', bitfield.Fields.Select((innerField, index) => {
                                        return @$"
                                            /// {FieldDescription(innerField)}
                                            const {ToSnakeCase(FieldName(innerField)).ToUpper()} = 0b{new string('0', bitfield.Size * 8 - 1 - index)}1{new string('0', index)};
                                        ";
                                    }))}
                                }}
                            }}
                        ");
                        }
                    }

                    sw.WriteLine($"pub struct {paramdef.ParamType} {{");

                    bitfieldIndex = 0;
                    foreach (var field in cfields)
                    {
                        if (field is CScalar scalar)
                        {
                            sw.WriteLine($"/// {scalar.Description}");
                            sw.WriteLine($"{ToSnakeCase(scalar.Name).ToLower()} : {scalar.Type},");
                        }
                        else if (field is CBitfield bitfield)
                        {
                            sw.WriteLine($"bitfield_{bitfieldIndex} : {paramdef.ParamType}Bitfield{bitfieldIndex},");
                            bitfieldIndex++;
                        }
                    }

                    sw.WriteLine("}");
                }
                catch (Exception ex)
                {
                    continue;
                }
            }
        }
    }
}