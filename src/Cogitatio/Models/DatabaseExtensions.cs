using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace Cogitatio.Models;

/// <summary>
/// TODO consider using IDataReader in place of DbDataReader
/// </summary>
public static class DatabaseExtensions
{
    public static T ParseEnum<T>(string value)
    {
        return (T) Enum.Parse(typeof(T), value, true);
    }

    public static int AsInt(this DbDataReader rdr, string field)
    {
        if (rdr.IsDBNull(rdr.GetOrdinal(field)))
            return -1;
        
        return Convert.ToInt32(rdr[field]);
    }
    
    public static string AsString(this DbDataReader rdr, string field)
    {
        if (rdr.IsDBNull(rdr.GetOrdinal(field)))
            return string.Empty;
        
        return rdr[field].ToString();
    }

    public static double AsDouble(this DbDataReader rdr, string field)
    {
        if (rdr.IsDBNull(rdr.GetOrdinal(field)))
            return 0.0;

        return Convert.ToDouble(rdr[field]);
    }

    public static DateTime AsDateTime(this DbDataReader rdr, string field)
    {
        if (rdr.IsDBNull(rdr.GetOrdinal(field)))
            return DateTime.MinValue;

        return DateTime.Parse(rdr.AsString(field));
    }
}