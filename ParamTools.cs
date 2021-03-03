using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SoulsParamsConverter.Commands;
using SoulsParamsConverter.Format;
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
    }

    public enum Game
    {
        DS3,
    }

    class ParamTools
    {
        public static async Task<int> Main(params string[] args)
        {
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
                    "Path to SoulsMods Paramdex")
            };

            convert.Handler =
                CommandHandler
                    .Create<Game, ParamFormat, FileInfo, ParamFormat, FileInfo, FileInfo>(ParamsConverter.Run);

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

            var rootCommand = new RootCommand("Tools for working with the FromSoftware PARAM format")
            {
                convert,
                schema
            };

            return await rootCommand.InvokeAsync(args);
        }
    }
}