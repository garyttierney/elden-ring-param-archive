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
            FileInfo paramdexPath)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            using var reader = inputFormat.CreateReader(game, inputPath);
            using var writer = outputFormat.CreateWriter(game, outputPath);

            foreach (var paramRef in reader.List())
            {
                var paramName = paramRef.Name;
                var paramType = paramRef.Type;

                var paramDef = PARAMDEF.XmlDeserialize($"{paramdexPath.FullName}/{game}/Defs/{paramType}.xml");
                var param = reader.Read(paramName, paramDef);

                /* the same names.txt parsing code from yapped */
                var paramRowNames = new Dictionary<long, string>();
                var paramNamesText = File.ReadAllText($@"{paramdexPath.FullName}/{game}/Names/{paramName}.txt");

                foreach (var line in Regex.Split(paramNamesText, @"\s*[\r\n]+\s*"))
                {
                    if (line.Length <= 0) continue;
                    var match = Regex.Match(line, @"^(\d+) (.+)$");
                    var id = long.Parse(match.Groups[1].Value);
                    var name = match.Groups[2].Value;

                    paramRowNames[id] = name;
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