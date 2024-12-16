namespace Cogitatio.Models;

public class ContactRecord
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string Message { get; set; }
    public string Slug { get; set; }
    public DateTime DateAdded { get; set; }
}