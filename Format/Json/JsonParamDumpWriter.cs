using System.IO;
using Newtonsoft.Json;
using SoulsFormats;

namespace SoulsParamsConverter.Format.Json
{
    public class JsonParamDumpWriter : ParamDumpWriter
    {
        private JsonTextWriter writer;
        private Stream stream;

        public JsonParamDumpWriter(FileInfo outputFilePath)
        {
            stream = File.Open(outputFilePath.FullName, FileMode.CreateNew, FileAccess.Write);
            writer = new JsonTextWriter(new StreamWriter(stream));
            writer.Formatting = Formatting.None;
            writer.WriteStartObject();
        }

        public void Dispose()
        {
            writer.WriteEndObject();
            writer.Close();
            stream.Close();
        }

        public void Write(string name, PARAM param)
        {
            writer.WritePropertyName(name);
            writer.WriteStartObject();

            foreach (var row in param.Rows)
            {
                writer.WritePropertyName(row.ID.ToString());
                writer.WriteStartObject();
                
                for (var cellIndex = 0; cellIndex < row.Cells.Count; cellIndex++)
                {
                    var cell = row.Cells[cellIndex];
                    
                    writer.WritePropertyName(cell.Def.DisplayName);

                    if (cell.Value is byte[])
                    {
                        writer.WriteValue("-");
                    }
                    else
                    {
                        writer.WriteValue(cell.Value);
                    }
                }
                
                writer.WriteEndObject();
            }
            
            writer.WriteEndObject();
        }
    }
}