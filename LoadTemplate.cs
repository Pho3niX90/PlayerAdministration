namespace Oxide.Plugins
{
    [Info("Load Template", "Orange", "1.0.0")]
    [Description("https://rustworkshop.space/")]
    public class LoadTemplate : RustPlugin
    {
        private void Init()
        {
            PrintWarning("I am template. If you see me - orange forgot to load proper file to website...");
        }
    }
}