using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SoulsParamsConverter.Commands;
using SoulsParamsConverter.Format;
using SoulsParamsConverter.Format.Csv;
using SoulsParamsConverter.Format.Excel;
using SoulsParamsConverter.Format.Json;
using SoulsParamsConverter.Format.SQLite;

namespace SoulsParamsConverter
{
    static class ParamFormatExtensions
    {
        public static ParamDumpReader CreateReader(this ParamFormat format, Game game, FileInfo path)
        {
            return format switch
            {
                ParamFormat.Regulation => new RegulationFileParamDumpReader(path),
                ParamFormat.Sqlite => new SqliteParamDumpReader(path),
                ParamFormat.Excel => new ExcelParamDumpReader(path),
                ParamFormat.Csv => new CsvParamDumpReader(path),
                _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
            };
        }

        public static ParamDumpWriter CreateWriter(this ParamFormat format, Game game, FileInfo path)
        {
            return format switch
            {
                ParamFormat.Regulation => new RegulationFileParamDumpWriter(path),
                ParamFormat.Sqlite => new SqliteParamDumpWriter(path),
                ParamFormat.Excel => new ExcelParamDumpWriter(path),
                ParamFormat.Json => new JsonParamDumpWriter(path),
                ParamFormat.Csv => new CsvParamDumpWriter(path),
                _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
            };
        }
    }

    public enum ParamFormat
    {
        Regulation,
        Sqlite,
        Excel,
        Json,
        Csv
    }

    public enum Game
    {
        DS3,
        SDT,
        ER,
    }

    class ParamTools
    {
        public static async Task<int> Main(params string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var convert = new Command("convert", "Convert a set of PARAM files in one format to another")
            {
                new Option<Game>(
                    "--game",
                    getDefaultValue: () => Game.DS3,
                    description: "The game PARAMs are being loaded for"),
                new Option<ParamFormat>("--input-format", "The format of the input params"),
                new Option<FileInfo>(
                    "--input-path",
                    "An option whose argument is parsed as a bool"),
                new Option<ParamFormat>("--output-format", "The format of the output param dump"),
                new Option<FileInfo>(
                    "--output-path",
                    "An option whose argument is parsed as a FileInfo"),
                new Option<FileInfo>(
                    "--paramdex-path",
                    "Path to SoulsMods Paramdex"),
                new Option<Regex>(
                    "--type-filter",
                    description: "Regex to filter param types",
                    getDefaultValue: () => new Regex(@".*"))
            };

            convert.Handler =
                CommandHandler
                    .Create<Game, ParamFormat, FileInfo, ParamFormat, FileInfo, FileInfo, Regex>(ParamsConverter.Run);

            var schema = new Command("generate-schema", "Generate a schema for PARAMDEF files")
            {
                new Option<Game>(
                    "--game",
                    getDefaultValue: () => Game.DS3,
                    description: "The game PARAMs are being loaded for"),
                new Option<FileInfo>(
                    "--paramdex-path",
                    "Path to SoulsMods Paramdex"),
                new Option<Regex>(
                    "--type-filter",
                    description: "Regex to filter param types",
                    getDefaultValue: () => new Regex(@".*"))
            };

            schema.Handler = CommandHandler.Create<Game, FileInfo, Regex>(ParamsSchemaGenerator.Run);

            var paramCommands = new Command("params", "Tools for working with FromSoftware PARAMs")
            {
                convert,
                schema
            };

            var flverPropertiesUpdater = new Command("properties", "Update FLVER properties")
            {
                new Option<bool?>(
                    "--backface-culling",
                    getDefaultValue: () => null,
                    description: "Toggle backface culling in all FLVER submeshes"
                ),
                new Option<string>(
                    "--source",
                    description: "Path to a MAPBND to edit FLVERs in. May be multiple paths separated by semi-colons.")
            };

            flverPropertiesUpdater.Handler = CommandHandler.Create<bool?, string>(FlverPropertiesUpdater.Run);

            var flverCommands = new Command("flver", "Tools for working with FLVER models")
            {
                flverPropertiesUpdater,
            };

            var msbExporter = new Command("export", "Export an MSB to JSON")
            {
                new Option<FileInfo>(
                    "--path",
                    description: "Path to an MSB file"
                ),
            };


            msbExporter.Handler = CommandHandler.Create<FileInfo>(MsbExporter.Run);

            var msbCommands = new Command("msb", "Tools for working with MSBs")
            {
                msbExporter,
            };

            var rootCommand = new RootCommand("Tools for working with the FromSoftware PARAM format")
            {
                paramCommands,
                flverCommands,
                msbCommands
            };

            return await rootCommand.InvokeAsync(args);
        }
    }
}