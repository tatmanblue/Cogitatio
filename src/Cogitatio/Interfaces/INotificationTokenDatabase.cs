using Cogitatio.Models;

namespace Cogitatio.Interfaces;

public interface INotificationTokenDatabase
{
    NotificationToken Save(NotificationToken token);
    NotificationToken? LoadByUserAndType(int userId, NotificationTokenType type);
    NotificationToken? LoadByToken(string token);
    void MarkUsed(NotificationToken token);
}
