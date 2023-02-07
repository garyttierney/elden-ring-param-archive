using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using SoulsFormats;

namespace SoulsParamsConverter.Format.SQLite
{
    static class SqliteExtensions
    {
        public static SQLiteParameter CreateNamedParameter(this SQLiteCommand command, string name)
        {
            var param = command.CreateParameter();
            param.ParameterName = name;

            command.Parameters.Add(param);
            return param;
        }
    }
    public class SqliteParamDumpWriter : ParamDumpWriter
    {
        private readonly SQLiteConnection db;

        public SqliteParamDumpWriter(FileInfo dbPath)
        {
            db = new SQLiteConnection("Data Source=" + dbPath.FullName);
            db.Open();
        }

        public void Write(string name, PARAM param)
        {
            var mapper = new SqliteSchemaMapper();
            var columns = param.AppliedParamdef.Fields
                .ToDictionary(field => field, group => mapper.MapField(group));

            var columnsSchemaSql = String.Join(",\n ", columns.Values.Select(field => field.ToSqlDescription()));
            var schemaSql = $@"
                DROP TABLE IF EXISTS {name};
                CREATE TABLE {name} (
                    --- paramType: {param.ParamType}
                    id INT PRIMARY KEY,
                    name TEXT NULL,
                    {columnsSchemaSql}
                );
            ";

            using (var txn = db.BeginTransaction())
            {
                var cmd = txn.Connection.CreateCommand();
                cmd.CommandText = schemaSql;
                cmd.ExecuteNonQuery();

                txn.Commit();
            }

            using (var txn = db.BeginTransaction())
            {
                var columnsInsertSql = String.Join(", ", columns.Values.Select(field => $"@{field.Name}"));
                var cmd = txn.Connection.CreateCommand();
                cmd.CommandText = $@"
                    INSERT INTO {name} VALUES (@id, @name, {columnsInsertSql})
                    ON CONFLICT DO NOTHING;
                ";

                var id = cmd.CreateNamedParameter("id");
                var description = cmd.CreateNamedParameter("name");
                var dataParams = new Dictionary<PARAMDEF.Field, SQLiteParameter>();

                foreach (var (key, column) in columns)
                {
                    dataParams[key] = cmd.CreateNamedParameter(column.Name);
                }

                foreach (var row in param.Rows)
                {
                    id.Value = row.ID;
                    description.Value = row.Name;

                    foreach (var cell in row.Cells)
                    {
                        dataParams[cell.Def].Value = cell.Value;
                    }

                    cmd.ExecuteNonQuery();
                }

                txn.Commit();
            }
        }

        public void Dispose()
        {
            db.Dispose();
        }
    }
}