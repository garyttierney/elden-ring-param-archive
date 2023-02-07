using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Text;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using SoulsFormats;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace SoulsParamsConverter.Commands
{
    public class MsbExporter
    {
        public static void WriteVec3(Vector3 value, JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("x");
            writer.WriteValue(value.X);
            writer.WritePropertyName("y");
            writer.WriteValue(value.Y);
            writer.WritePropertyName("z");
            writer.WriteValue(value.Z);
            writer.WriteEndObject();
        }
        
        public static void Run(FileInfo path)
        {
            var msb = MSB3.Read(path.FullName);
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);
            using var writer =
                new JsonTextWriter(sw);

            writer.Formatting = Formatting.Indented;
            writer.WriteStartObject();
            writer.WritePropertyName("mapId");
            writer.WriteValue(path.Name.Replace(".msb.dcx", ""));
            writer.WritePropertyName("mapPieces");
            writer.WriteStartArray();;
            foreach (MSB3.Part.MapPiece piece in msb.Parts.MapPieces)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("id");
                writer.WriteValue(piece.EntityID);
                writer.WritePropertyName("model");
                writer.WriteValue(piece.ModelName.Substring(1));
                writer.WritePropertyName("position");
                WriteVec3(piece.Position, writer);
                writer.WritePropertyName("rotation");
                WriteVec3(piece.Rotation, writer);
                writer.WritePropertyName("scale");
                WriteVec3(piece.Scale, writer);
                writer.WriteEndObject();
            }
            
            writer.WriteEndArray();;
            writer.WriteEndObject();
            Console.WriteLine(sb.ToString());

        }
    }
}