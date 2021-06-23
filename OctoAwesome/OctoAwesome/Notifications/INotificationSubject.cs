using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OctoAwesome.Notifications
{
    public interface INotificationSubject<TNotification> where TNotification : Notification
    {
        void OnNotification(TNotification notification);

        void Push(TNotification notification);
    }
}
