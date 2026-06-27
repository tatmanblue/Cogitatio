using System.Threading.Channels;
using Cogitatio.Models;

namespace Cogitatio.Interfaces;

public interface INotificationQueue
{
    void Enqueue(BlogPost post);
    ChannelReader<BlogPost> Reader { get; }
}
