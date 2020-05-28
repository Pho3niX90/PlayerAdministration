namespace Oxide.Plugins
{

    [Info("Covalence Info", "Pho3niX90", "0.0.1")]
    class CovalenceTest : CovalencePlugin
    {
        void Loaded() {
            Puts($"Covalence Information\n ServerName: {server.Name}\n ServerIp: {server.Address}\n ServerLocalAddress: {server.LocalAddress}\n Protocol: {server.Protocol}\n Version: {server.Version}");
        }
    }
}
