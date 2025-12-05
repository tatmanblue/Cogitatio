using System.Data.Common;
using Cogitatio.Interfaces;
using Cogitatio.Models;
using Microsoft.Data.SqlClient;

namespace Cogitatio.Logic;

/// <summary>
/// TODO consider using IDataReader in place of DbDataReader
/// </summary>
public static class DatabaseExtensions
{
    public static T ParseEnum<T>(string value)
    {
        return (T) Enum.Parse(typeof(T), value, true);
    }

    extension(DbDataReader rdr)
    {
        public int AsInt(string field)
        {
            if (rdr.IsDBNull(rdr.GetOrdinal(field)))
                return -1;
        
            return Convert.ToInt32(rdr[field]);
        }

        public string AsString(string field)
        {
            if (rdr.IsDBNull(rdr.GetOrdinal(field)))
                return string.Empty;
        
            return rdr[field].ToString();
        }

        public double AsDouble(string field)
        {
            if (rdr.IsDBNull(rdr.GetOrdinal(field)))
                return 0.0;

            return Convert.ToDouble(rdr[field]);
        }

        public DateTime AsDateTime(string field)
        {
            if (rdr.IsDBNull(rdr.GetOrdinal(field)))
                return DateTime.MinValue;

            return DateTime.Parse(rdr.AsString(field));
        }
    }

    extension(IDatabase database)
    {
        public bool GetSettingAsBool(BlogSettings setting, bool defaultValue = false)
        {
            string value = database.GetSetting(setting, defaultValue ? "true" : "false");
            return Convert.ToBoolean(value);
        }

        public int GetSettingAsInt(BlogSettings setting, int defaultValue = 0)
        {
            string value = database.GetSetting(setting, defaultValue.ToString());
            return Convert.ToInt32(value);
        }
    }
}