using System.Data;
using Cogitatio.Interfaces;
using Cogitatio.Models;
using Microsoft.Data.SqlClient;

namespace Cogitatio.Logic;

public class SqlServerImages : AbstractDB<SqlConnection>, IImageDatabase
{
    private readonly ILogger<IImageDatabase> logger;

    public SqlServerImages(ILogger<IImageDatabase> logger, string connectionStr, int tenantId)
        : base(connectionStr, tenantId)
    {
        this.logger = logger;
    }

    protected override void Connect()
    {
        if (null != connection) return;
        connection = new SqlConnection(connectionString);
        connection.Open();
    }

    public BlogImage Save(BlogImage image)
    {
        Connect();
        using SqlCommand cmd = new SqlCommand();
        cmd.CommandType = CommandType.Text;
        cmd.Connection = connection;
        cmd.CommandText = @"INSERT INTO Blog_Images
                (TenantId, FileName, StoragePath, ThumbPath, ContentType, Width, Height, SizeBytes, AltText, CreatedDate)
            OUTPUT INSERTED.Id
            VALUES (@tenantId, @fileName, @storagePath, @thumbPath, @contentType, @width, @height, @sizeBytes, @altText, @createdDate)";
        cmd.Parameters.AddWithValue("@tenantId", tenantId);
        cmd.Parameters.AddWithValue("@fileName", image.FileName);
        cmd.Parameters.AddWithValue("@storagePath", image.StoragePath);
        cmd.Parameters.AddWithValue("@thumbPath", (object?)image.ThumbPath ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@contentType", image.ContentType);
        cmd.Parameters.AddWithValue("@width", image.Width);
        cmd.Parameters.AddWithValue("@height", image.Height);
        cmd.Parameters.AddWithValue("@sizeBytes", image.SizeBytes);
        cmd.Parameters.AddWithValue("@altText", image.AltText);
        cmd.Parameters.AddWithValue("@createdDate", image.CreatedDate);

        image.Id = (int)cmd.ExecuteScalar();
        image.TenantId = tenantId;
        logger.LogInformation("Image {ImageId} saved ({StoragePath}, {SizeBytes} bytes)", image.Id, image.StoragePath, image.SizeBytes);
        return image;
    }

    public BlogImage? Load(int id)
    {
        BlogImage? result = null;
        string sql = @"SELECT Id, TenantId, FileName, StoragePath, ThumbPath, ContentType, Width, Height, SizeBytes, AltText, CreatedDate
            FROM Blog_Images
            WHERE Id = @id AND TenantId = @tenantId";

        ExecuteReader<SqlCommand, SqlDataReader>(sql, () => new SqlCommand(), reader =>
        {
            result = ReadImageRecord(reader);
            return false;
        }, setup =>
        {
            setup.Parameters.AddWithValue("@id", id);
            setup.Parameters.AddWithValue("@tenantId", tenantId);
        });

        return result;
    }

    private static BlogImage ReadImageRecord(SqlDataReader reader)
    {
        int thumbOrdinal = reader.GetOrdinal("ThumbPath");
        return new BlogImage
        {
            Id = reader.AsInt("Id"),
            TenantId = reader.AsInt("TenantId"),
            FileName = reader.AsString("FileName"),
            StoragePath = reader.AsString("StoragePath"),
            ThumbPath = reader.IsDBNull(thumbOrdinal) ? null : reader.AsString("ThumbPath"),
            ContentType = reader.AsString("ContentType"),
            Width = reader.AsInt("Width"),
            Height = reader.AsInt("Height"),
            SizeBytes = Convert.ToInt64(reader["SizeBytes"]),
            AltText = reader.AsString("AltText"),
            CreatedDate = reader.AsDateTime("CreatedDate")
        };
    }
}
