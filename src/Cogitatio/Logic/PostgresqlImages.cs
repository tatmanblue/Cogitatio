using System.Data;
using Cogitatio.Interfaces;
using Cogitatio.Models;
using Npgsql;

namespace Cogitatio.Logic;

public class PostgresqlImages : AbstractDB<NpgsqlConnection>, IImageDatabase
{
    private readonly ILogger<IImageDatabase> logger;

    public PostgresqlImages(ILogger<IImageDatabase> logger, string connectionStr, int tenantId)
        : base(connectionStr, tenantId)
    {
        this.logger = logger;
    }

    protected override void Connect()
    {
        if (null != connection) return;
        connection = new NpgsqlConnection(connectionString);
        connection.Open();
    }

    public BlogImage Save(BlogImage image)
    {
        Connect();
        using NpgsqlCommand cmd = new NpgsqlCommand();
        cmd.CommandType = CommandType.Text;
        cmd.Connection = connection;
        cmd.CommandText = @"INSERT INTO blog_images
                (tenant_id, file_name, storage_path, thumb_path, content_type, width, height, size_bytes, alt_text, created_date)
            VALUES (@tenantId, @fileName, @storagePath, @thumbPath, @contentType, @width, @height, @sizeBytes, @altText, @createdDate)
            RETURNING id";
        cmd.Parameters.AddWithValue("tenantId", tenantId);
        cmd.Parameters.AddWithValue("fileName", image.FileName);
        cmd.Parameters.AddWithValue("storagePath", image.StoragePath);
        cmd.Parameters.AddWithValue("thumbPath", (object?)image.ThumbPath ?? DBNull.Value);
        cmd.Parameters.AddWithValue("contentType", image.ContentType);
        cmd.Parameters.AddWithValue("width", image.Width);
        cmd.Parameters.AddWithValue("height", image.Height);
        cmd.Parameters.AddWithValue("sizeBytes", image.SizeBytes);
        cmd.Parameters.AddWithValue("altText", image.AltText);
        cmd.Parameters.AddWithValue("createdDate", image.CreatedDate);

        image.Id = (int)cmd.ExecuteScalar();
        image.TenantId = tenantId;
        logger.LogInformation("Image {ImageId} saved ({StoragePath}, {SizeBytes} bytes)", image.Id, image.StoragePath, image.SizeBytes);
        return image;
    }

    public BlogImage? Load(int id)
    {
        BlogImage? result = null;
        string sql = @"SELECT id, tenant_id, file_name, storage_path, thumb_path, content_type, width, height, size_bytes, alt_text, created_date
            FROM blog_images
            WHERE id = @id AND tenant_id = @tenantId";

        ExecuteReader<NpgsqlCommand, NpgsqlDataReader>(sql, () => new NpgsqlCommand(), reader =>
        {
            result = ReadImageRecord(reader);
            return false;
        }, setup =>
        {
            setup.Parameters.AddWithValue("id", id);
            setup.Parameters.AddWithValue("tenantId", tenantId);
        });

        return result;
    }

    private static BlogImage ReadImageRecord(NpgsqlDataReader reader)
    {
        int thumbOrdinal = reader.GetOrdinal("thumb_path");
        return new BlogImage
        {
            Id = reader.AsInt("id"),
            TenantId = reader.AsInt("tenant_id"),
            FileName = reader.AsString("file_name"),
            StoragePath = reader.AsString("storage_path"),
            ThumbPath = reader.IsDBNull(thumbOrdinal) ? null : reader.AsString("thumb_path"),
            ContentType = reader.AsString("content_type"),
            Width = reader.AsInt("width"),
            Height = reader.AsInt("height"),
            SizeBytes = Convert.ToInt64(reader["size_bytes"]),
            AltText = reader.AsString("alt_text"),
            CreatedDate = reader.AsDateTime("created_date")
        };
    }
}
