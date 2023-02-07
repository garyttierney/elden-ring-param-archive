using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SoulsFormats;

namespace SoulsParamsConverter.Commands
{
    public class ParamsConverter
    {
        public static void Run(
            Game game,
            ParamFormat inputFormat,
            FileInfo inputPath,
            ParamFormat outputFormat,
            FileInfo outputPath,
            FileInfo paramdexPath,
            Regex typeFilter)
        {
            using var reader = inputFormat.CreateReader(game, inputPath);
            using var writer = outputFormat.CreateWriter(game, outputPath);
            var paramdefSources = Directory.GetFiles($"{paramdexPath.FullName}/{game}/Defs/", "*.xml");
            var paramdefs = paramdefSources.Select(source => PARAMDEF.XmlDeserialize(source))
                .ToDictionary(def => def.ParamType);

            foreach (var paramRef in reader.List())
            {
                var paramName = paramRef.Name;
                var paramType = paramRef.Type;

                if (!typeFilter.IsMatch(paramName) && !typeFilter.IsMatch(paramType))
                {
                    continue;
                }

                Console.WriteLine($"Reading {paramName} with type {paramType}");

                if (!paramdefs.ContainsKey(paramType))
                {
                    Console.WriteLine($"No paramdef for {paramName}, {paramType}. Skipping.");
                    continue;
                }

                var paramDef = paramdefs[paramType];
                var param = reader.Read(paramName, paramDef);

                /* the same names.txt parsing code from yapped */
                var paramRowNames = new Dictionary<long, string>();

                var paramNamesPath = $@"{paramdexPath.FullName}/{game}/Names/{paramName}.txt";
                if (File.Exists(paramNamesPath))
                {
                    var paramNamesText = File.ReadAllText(paramNamesPath);

                    foreach (var line in Regex.Split(paramNamesText, @"\s*[\r\n]+\s*"))
                    {
                        if (line.Length <= 0) continue;
                        var match = Regex.Match(line, @"^(\d+) (.+)$");
                        var id = long.Parse(match.Groups[1].Value);
                        var name = match.Groups[2].Value;

                        paramRowNames[id] = name;
                    }
                }

                foreach (var row in param.Rows.Where(row => paramRowNames.ContainsKey(row.ID)))
                {
                    row.Name = paramRowNames[row.ID];
                }

                writer.Write(paramName, param);
            }
        }
    }
}