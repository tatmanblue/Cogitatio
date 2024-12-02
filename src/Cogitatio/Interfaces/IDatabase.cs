using Cogitatio.Models;

namespace Cogitatio.Interfaces;

public interface IDatabase
{
    string ConnectionString { get; }
    BlogPost GetMostRecent();
}