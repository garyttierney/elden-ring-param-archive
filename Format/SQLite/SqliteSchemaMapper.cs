using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SoulsFormats;
using SoulsParamsConverter.Format.SQLite.Schema;

namespace SoulsParamsConverter.Format.SQLite
{
    namespace Schema
    {
        public struct Column
        {
            public string Type { get; }
            public string Name { get; }

            public Column(string type, string name)
            {
                Type = type;
                Name = name;
            }

            public string ToSqlDescription()
            {
                return $@"{Name} {Type}";
            }
        }
    }

    public class SqliteSchemaMapper
    {
        private readonly Dictionary<string, int> _nameOccurrences = new Dictionary<string, int>();

        public Column MapField(PARAMDEF.Field field)
        {
            var sqlType = field.DisplayType switch
            {
                PARAMDEF.DefType _ when field.BitSize == 1 => "BOOLEAN", 
                PARAMDEF.DefType.s8 => "INTEGER",
                PARAMDEF.DefType.u8 => "INTEGER",
                PARAMDEF.DefType.s16 => "INTEGER",
                PARAMDEF.DefType.u16 => "INTEGER",
                PARAMDEF.DefType.s32 => "INTEGER",
                PARAMDEF.DefType.u32 => "INTEGER",
                PARAMDEF.DefType.f32 => "REAL",
                PARAMDEF.DefType.dummy8 => "BLOB",
                PARAMDEF.DefType.fixstr => "TEXT",
                PARAMDEF.DefType.fixstrW => "TEXT",
                _ => throw new ArgumentOutOfRangeException()
            };

            var name = Regex.Replace(field.DisplayName, @"[^_a-z0-9]", "_", RegexOptions.IgnoreCase);
            if (Regex.IsMatch(name, @"^\d"))
            {
                name = "f_" + name;
            }

            var occurrencesKey = name.ToLower();
            // if it didn't add, we've already seen the name.
            _nameOccurrences.TryAdd(occurrencesKey, 0);

            var index = _nameOccurrences[occurrencesKey]++;
            if (index != 0)
            {
                name = name + "_" + index;
            }

            return new Column(sqlType, name);
        }
    }
}