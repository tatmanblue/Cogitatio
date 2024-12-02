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
        return Convert.ToInt32(rdr[field]);
    }
    
    public static string AsString(this SqlDataReader rdr, string field)
    {
        return rdr[field].ToString();
    }

    public static double AsDouble(this SqlDataReader rdr, string field)
    {
        return Convert.ToDouble(rdr[field]);
    }

    public static DateTime AsDateTime(this SqlDataReader rdr, string field)
    {
        return DateTime.Parse(rdr.AsString(field));
    }
}