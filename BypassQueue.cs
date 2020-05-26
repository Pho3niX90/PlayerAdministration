using Network;

namespace Oxide.Plugins
{
    [Info("Bypass Queue", "Orange", "1.0.2")]
    public class BypassQueue : RustPlugin
    {
        private const string permUse = "bypassqueue.allow";

        private void Init()
        {
            permission.RegisterPermission(permUse, this);
        }

        private object CanBypassQueue(Connection connection)
        {
            if (permission.UserHasPermission(connection.userid.ToString(), permUse))
            {
                return true;
            }
            else
            {
                return null;
            }
        }
    }
}
