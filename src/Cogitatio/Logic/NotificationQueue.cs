using System.Threading.Channels;
using Cogitatio.Interfaces;
using Cogitatio.Models;

namespace Cogitatio.Logic;

public class NotificationQueue : INotificationQueue
{
    private readonly Channel<BlogPost> channel = Channel.CreateUnbounded<BlogPost>();

    public void Enqueue(BlogPost post) => channel.Writer.TryWrite(post);

    public ChannelReader<BlogPost> Reader => channel.Reader;
}
