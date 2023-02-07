using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using SoulsFormats;

namespace SoulsParamsConverter.Format.SQLite
{
    public class SqliteParamDumpReader : ParamDumpReader
    {
        private readonly SQLiteConnection db;

        public SqliteParamDumpReader(FileInfo dbPath)
        {
            db = new SQLiteConnection("Data Source=" + dbPath.FullName);
            db.Open();
        }
        
        public void Dispose()
        {
            db.Dispose();
        }

        public PARAM Read(string name, PARAMDEF def)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<ParamFileReference> List()
        {
            throw new System.NotImplementedException();
        }
    }
}