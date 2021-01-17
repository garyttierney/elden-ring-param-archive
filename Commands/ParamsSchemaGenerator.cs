using System;
using System.IO;
using System.Text.RegularExpressions;
using SoulsFormats;

namespace SoulsParamsConverter.Commands
{
    public class ParamsSchemaGenerator
    {
        public static void Run(Game game, FileInfo paramdexPath, Regex typeFilter)
        {
            var paramdefPaths = Directory.GetFiles($"{paramdexPath.FullName}/{game}/Defs", "*.xml");

            foreach (var paramdefPath in paramdefPaths)
            {
                var paramdef = PARAMDEF.XmlDeserialize(paramdefPath);
                if (!typeFilter.IsMatch(paramdef.ParamType))
                {
                    continue;
                }   
                
                Console.WriteLine($"struct {paramdef.ParamType} {{");
                
                foreach (var field in paramdef.Fields)
                {
                    var ctype = field.DisplayType switch
                    {
                        PARAMDEF.DefType _ when field.BitSize == 1 => "bool", 
                        PARAMDEF.DefType.s8 => "char",
                        PARAMDEF.DefType.u8 => "unsigned char",
                        PARAMDEF.DefType.s16 => "short",
                        PARAMDEF.DefType.u16 => "unsigned short",
                        PARAMDEF.DefType.s32 => "int",
                        PARAMDEF.DefType.u32 => "unsigned int",
                        PARAMDEF.DefType.f32 => "float",
                        PARAMDEF.DefType.dummy8 => "char",
                        PARAMDEF.DefType.fixstr => "char*",
                        PARAMDEF.DefType.fixstrW => "wchar_t*",
                        _ => throw new ArgumentOutOfRangeException()
                    };

                    Console.Write($"    {ctype} {field.DisplayName}");

                    if (field.BitSize != -1 && field.BitSize % 8 != 0)
                    {
                        Console.Write($" : {field.BitSize}");
                    } else if (field.ArrayLength > 1)
                    {
                        Console.Write($"[{field.ArrayLength}]");
                    }
                    
                    Console.WriteLine(";");
                }

                Console.WriteLine("};");
            }
        }
    }
}