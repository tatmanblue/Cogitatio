using Microsoft.Data.SqlClient;

namespace Cogitatio.Models;

public static class DatabaseExtensions
{
    public static T ParseEnum<T>(string value)
    {
        return (T) Enum.Parse(typeof(T), value, true);
    }

    public static int AsInt(this SqlDataReader rdr, string field)
    {
        if (rdr.IsDBNull(rdr.GetOrdinal(field)))
            return -1;
        
        return Convert.ToInt32(rdr[field]);
    }
    
    public static string AsString(this SqlDataReader rdr, string field)
    {
        if (rdr.IsDBNull(rdr.GetOrdinal(field)))
            return string.Empty;
        
        return rdr[field].ToString();
    }

    public static double AsDouble(this SqlDataReader rdr, string field)
    {
        if (rdr.IsDBNull(rdr.GetOrdinal(field)))
            return 0.0;

        return Convert.ToDouble(rdr[field]);
    }

    public static DateTime AsDateTime(this SqlDataReader rdr, string field)
    {
        if (rdr.IsDBNull(rdr.GetOrdinal(field)))
            return DateTime.MinValue;

        return DateTime.Parse(rdr.AsString(field));
    }
}