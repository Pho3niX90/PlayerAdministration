using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Our Servers", "Hougan / rostov114", "0.0.8")] 
    [Description("Show information about other servers (with fetching current online)")]
    public class OurServers : RustPlugin 
    {
        #region Classes

        private class Server
        {
            [JsonProperty("Name of your server in interface")] 
            public string DisplayName;

            [JsonProperty("Description of your server in interface")] 
            public string Description;

            [JsonProperty("IP:Port of server (ex: 127.0.0.1:12000)")]
            public string IPAndPort;

            [JsonProperty("Interface width (increase it, if your description is very length)")]
            public int ServerWidth = 250;

            [JsonIgnore] 
            public int CurrentOnline; 

            [JsonIgnore] 
            public int MaxOnline;

            [JsonIgnore]
            public bool Status = false;

            public string IP()                      => IPAndPort.Split(':')[0];
            public int    Port()                    => int.Parse(IPAndPort.Split(':')[1]);
            public int    GetOnline()               => Mathf.Min(MaxOnline, CurrentOnline);
            public void   UpdateStatus(bool status) => Status = status;
        } 

        private class Configuration
        {
            [JsonProperty("Servers configure")]
            public List<Server> Servers = new List<Server>();

            [JsonProperty("Command to open interface")]
            public string Command = "servers";

            [JsonProperty("Use chat instead of UI")]
            public bool UseChat = false;

            [JsonProperty("Information update interval in seconds (should be > 30)")]
            public int Interval = 60;

            [JsonProperty("Which API to use? (Possible options: steampowered or battlemetrics) (Default: battlemetrics)")]
            public string API = "battlemetrics";

            [JsonProperty("Steampowered API key")]
            public string SteamWebApiKey = "!!! You can get it HERE > https://steamcommunity.com/dev/apikey < and you need to insert HERE !!!";

            public static Configuration Generate()
            {
                return new Configuration
                {
                    Servers = new List<Server>
                    {
                        new Server
                        {
                            DisplayName = "UMOD-SERVER #1 - PROCEDURAL",
                            Description = "Example server, with example description",
                            IPAndPort   = "8.8.8.8:12000",
                            ServerWidth = 250
                        },
                        new Server
                        {
                            DisplayName = "UMOD-SERVER #2 - BARREN",
                            Description = "Example server, with example description",
                            IPAndPort   = "1.1.1.1:13000"
                        }
                    }
                };
            }
        }

        public class BattlemetricsApiResponse
        {
            [JsonProperty("data")]
            public List<ServerData> data;

            public class ServerData
            {
                [JsonProperty("id")]
                public string id;

                [JsonProperty("type")]
                public string type;

                [JsonProperty("attributes")]
                public Attributes attributes;
                
                public class Attributes
                {
                    [JsonProperty("id")]
                    public string id;

                    [JsonProperty("name")]
                    public string name;

                    [JsonProperty("ip")]
                    public string ip;

                    [JsonProperty("port")]
                    public int port;

                    [JsonProperty("portQuery")]
                    public int portQuery;

                    [JsonProperty("players")]
                    public int players;

                    [JsonProperty("maxPlayers")]
                    public int maxPlayers;

                    [JsonProperty("status")]
                    public string status;
                }
            }
        }

        public class SteampoweredApi
        {
            [JsonProperty("response")]
            public Response response;

            public class Response
            {
                [JsonProperty("servers")]
                public List<ServerData> servers;

                public class ServerData
                {
                    [JsonProperty("addr")]
                    public string addr;

                    [JsonProperty("gameport")]
                    public int gameport;

                    [JsonProperty("steamid")]
                    public string steamid;

                    [JsonProperty("name")]
                    public string name;
                    
                    [JsonProperty("appid")]
                    public int appid;

                    [JsonProperty("gamedir")]
                    public string gamedir;

                    [JsonProperty("version")]
                    public string version;

                    [JsonProperty("product")]
                    public string product;

                    [JsonProperty("region")]
                    public int region;

                    [JsonProperty("players")]
                    public int players;

                    [JsonProperty("max_players")]
                    public int max_players;

                    [JsonProperty("bots")]
                    public int bots;

                    [JsonProperty("map")]
                    public string map;

                    [JsonProperty("secure")]
                    public bool secure;

                    [JsonProperty("dedicated")]
                    public bool dedicated;

                    [JsonProperty("os")]
                    public string os;

                    [JsonProperty("gametype")]
                    public string gametype;
                }
            }
        }

        #endregion

        #region Variables

        // Coroutines
        private Coroutine UpdateAction;

        // Configuration
        private Configuration Settings;
        private bool          Initialized;
        private bool          Broken;

        #endregion

        #region Initialization

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                Settings = Config.ReadObject<Configuration>();
                if (Settings?.Servers == null) LoadDefaultConfig();
                SaveConfig();

                if (Settings.API == "steampowered" && (Settings.SteamWebApiKey == null || Settings.SteamWebApiKey == string.Empty || Settings.SteamWebApiKey.Length != 32))
                {
                    PrintError("Steampowered API requires an Steam Web API key to work! Check your configuration!");
                    Broken = true;
                }
            }
            catch
            {
                PrintError("Error reading config, please check!");
                Broken = true;
            }
        }

        protected override void SaveConfig() => Config.WriteObject(Settings);
        protected override void LoadDefaultConfig()
        {
            Settings = Configuration.Generate();
            SaveConfig();
        }

        private void OnServerInitialized()
        {
            if (Broken) return;

            cmd.AddChatCommand(Settings.Command, this, nameof(CmdShowServers));
            if (Settings.Interval < 30)
            {
                PrintError($"Update interval should be bigger then 30 s!");
                Settings.Interval = 30;
            }

            UpdateAction = ServerMgr.Instance.StartCoroutine(UpdateOnline());
        }

        private void Unload()
        {
            if (UpdateAction != null)
                ServerMgr.Instance.StopCoroutine(UpdateAction);
        }

        #endregion

        #region Functions

        private IEnumerator UpdateOnline()
        {
            while (true)
            {
                foreach (var check in Settings.Servers)
                {
                    if (Settings.API == "steampowered")
                    {
                        string url = $"https://api.steampowered.com/IGameServersService/GetServerList/v1/?format=json&key={Settings.SteamWebApiKey}&filter=\\gameaddr\\{check.IP()}:{check.Port()}";
                        webrequest.Enqueue(url, "", (code, response) =>
                        {
                            switch (code)
                            {
                                case 200:
                                    try
                                    {
                                        SteampoweredApi data = JsonConvert.DeserializeObject<SteampoweredApi>(response);
                                        if (data.response.servers != null)
                                        {
                                            switch (data.response.servers.Count)
                                            {
                                                case 1:
                                                    SteampoweredApi.Response.ServerData serverData = data.response.servers.Last();
                                                    if (serverData.addr != $"{check.IP()}:{check.Port()}")
                                                    {
                                                        PrintError($"Steampowered API: The response from Steampowered does not match the IP or port of the expected server! (Request: {check.IP()}:{check.Port()}, Response: {serverData.addr})");
                                                    }
                                                    else
                                                    {
                                                        check.CurrentOnline = serverData.players;
                                                        check.MaxOnline = serverData.max_players;
                                                        check.UpdateStatus(true);

                                                        Initialized = true;
                                                    }
                                                    break;

                                                default:
                                                    PrintError($"Steampowered API: The number of results is different than expected! Contact the plugin developer! (Server: {check.IP()}:{check.Port()}, Count: {data.response.servers.Count})");
                                                    break;
                                            }
                                        }
                                        else
                                        {
                                            check.UpdateStatus(false);
                                        }
                                    }
                                    catch
                                    {
                                        PrintError($"Steampowered API: Error parsing response from server. Perhaps the format has changed. Contact the plugin developer!");
                                    }
                                    break;

                                case 403:
                                    PrintError($"Steampowered API: Invalid Steam Web API key! Check it with your key indicated on the page: https://steamcommunity.com/dev/apikey");
                                    break;

                                default:
                                    PrintError($"Steampowered API HTTP CODE: {code}");
                                    break;
                            }
                        }, this);
                    }
                    else
                    {
                        string url = $"https://api.battlemetrics.com/servers?filter[search]=\"{check.IP()}:{check.Port()}\"";
                        webrequest.Enqueue(url, "", (code, response) =>
                        {
                            if (code == 200)
                            {
                                try
                                {
                                    BattlemetricsApiResponse apiResponse = JsonConvert.DeserializeObject<BattlemetricsApiResponse>(response);
                                    switch (apiResponse.data.Count)
                                    {
                                        case 1:
                                            BattlemetricsApiResponse.ServerData serverData = apiResponse.data.Last();
                                            if (check.IP() != serverData.attributes.ip || check.Port() != serverData.attributes.port)
                                            {
                                                PrintError($"Battlemetrics API: The response from Battlemetrics does not match the IP or port of the expected server! (Request: {check.IP()}:{check.Port()}, Response: {serverData.attributes.ip}:{serverData.attributes.port})");
                                            }
                                            else
                                            {
                                                check.CurrentOnline = serverData.attributes.players;
                                                check.MaxOnline = serverData.attributes.maxPlayers;
                                                check.UpdateStatus((serverData.attributes.status == "online"));

                                                Initialized = true;
                                            }
                                            break;

                                        case 0:
                                            PrintError($"Battlemetrics API: Server '{check.IP()}:{check.Port()}' not found! Perhaps he has not yet been interviewed. Wait a while.");
                                            break;

                                        default:
                                            PrintError($"Battlemetrics API: The number of results is different than expected! Contact the plugin developer! (Server: {check.IP()}:{check.Port()}, Count: {apiResponse.data.Count})");
                                            break;
                                    }
                                }
                                catch
                                {
                                    PrintError($"Battlemetrics API: Error parsing response from server. Perhaps the format has changed. Contact the plugin developer!");
                                }
                            }
                            else
                            {
                                PrintError($"Battlemetrics API HTTP CODE: {code}");
                            }
                        }, this);
                    }

                    yield return new WaitForSeconds(1f);
                }

                yield return new WaitForSeconds(Settings.Interval);
            }
        }

        #endregion 

        #region Commands

        [ConsoleCommand("UI_OurServersHandler")]
        private void CmdConsoleHandler(ConsoleSystem.Arg args)
        {
            BasePlayer player = args.Player();
            if (player == null || !args.HasArgs(1)) return;

            switch (args.Args[0].ToLower())
            {
                case "show":
                {
                    if (!args.HasArgs(2)) return;

                    int index = 0;
                    if (!int.TryParse(args.Args[1], out index) || Settings.Servers.ElementAtOrDefault(index) == null) return;
                    UI_DrawInterface(player, index);
                    break;
                }
                case "hide":
                {
                    UI_DrawInterface(player, -2);
                    break;
                }
            }
        }

        private void CmdShowServers(BasePlayer player, string command, string[] args)
        {
            if (!Initialized) return;

            if (Settings.UseChat)
            {
                string resultMessage = "";
                foreach (var server in Settings.Servers)
                    resultMessage += $"{server.DisplayName} (IP: {server.IPAndPort}) - " + ((server.Status) ? $"{server.GetOnline()}/{server.MaxOnline} players online" : "OFFLINE") + "\n\n";
                
                player.ChatMessage(resultMessage); 
            }
            else
            {
                UI_DrawInterface(player);
            }
        }

        #endregion

        #region Interface

        private const string Layer = "UI_OurServersLayer";

        private void UI_DrawInterface(BasePlayer player, int index = -1)
        {
            CuiHelper.DestroyUi(player, Layer);
            CuiElementContainer container = new CuiElementContainer();

            float  fadeSpeed     = 1f;
            Server choosedServer = null;
            if (index >= 0)
            {
                fadeSpeed     = 0f;
                choosedServer = Settings.Servers.ElementAtOrDefault(index);
            }
            else if (index == -2)
            {
                fadeSpeed = 0;
            }

            container.Add(new CuiPanel
            {
                CursorEnabled = true,
                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0"},
                Image         = {FadeIn    = fadeSpeed, Color = "0 0 0 0.7"}
            }, "Overlay", Layer);

            container.Add(new CuiButton
            {
                RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMax = "0 0"},
                Button        = {Color     = "0 0 0 0", Close = Layer},
                Text          = {Text      = ""}
            }, Layer);

            float panelWidth  = 80;
            var   list        = Settings.Servers.Where(p => p.CurrentOnline > -1).ToList();
            float topPosition = list.Count / 2f * panelWidth + (list.Count - 1) / 2f * 15;

            float startPosition = 0.5f;
            if (list.Count >= 6)
            {
                startPosition = 0.25f;
                topPosition /= 2f;
            }

            foreach (var check in list)
            {
                float leftPosition = check.ServerWidth * -1 - 83f;
                if (list.IndexOf(check) == list.Count - 1 && startPosition == 0.25f)
                    startPosition = 0.5f;

                container.Add(new CuiPanel
                {
                    RectTransform = {AnchorMin = $"{startPosition} 0.5", AnchorMax = $"{startPosition} {0.5f}", OffsetMin = $"{leftPosition} {topPosition - panelWidth}", OffsetMax = $"{check.ServerWidth} {topPosition}"},
                    Image         = {FadeIn    = fadeSpeed, Color     = "1 1 1 0.03008521"}
                }, Layer, check.IPAndPort);

                if (check != choosedServer)
                {
                    container.Add(new CuiPanel
                    {
                        RectTransform = {AnchorMin = "1 0", AnchorMax = "1 1", OffsetMin = "3 0", OffsetMax = "83 0"},
                        Image         = {FadeIn    = fadeSpeed, Color = "1 1 1 0.03008521"}
                    }, check.IPAndPort, check.IPAndPort + ".Help");

                    container.Add(new CuiButton
                    {
                        RectTransform = {AnchorMin = "0 0", AnchorMax  = "1 1", OffsetMin                                 = "10 10", OffsetMax = "-10 -10"},
                        Button        = {Color     = "1 1 1 0", Sprite = "assets/icons/circle_closed_toedge.png", Command = $"UI_OurServersHandler show {Settings.Servers.IndexOf(check)}"},
                        Text          = {FadeIn    = fadeSpeed, Text   = "i", Align                                       = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 50, Color = "1 1 1 0.7"}
                    }, check.IPAndPort + ".Help");

                    container.Add(new CuiLabel
                    {
                        RectTransform = {AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin        = "15 -45", OffsetMax                  = "-15 -10"},
                        Text          = {FadeIn    = fadeSpeed, Text  = check.DisplayName, Font = "robotocondensed-bold.ttf", FontSize = 20, Align = TextAnchor.LowerLeft}
                    }, check.IPAndPort);

                    container.Add(new CuiLabel
                    {
                        RectTransform = {AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin      = "15 -25", OffsetMax                     = "-15 -5"},
                        Text          = {FadeIn    = fadeSpeed, Text  = check.IPAndPort, Font = "robotocondensed-regular.ttf", FontSize = 16, Color = "1 1 1 0.6", Align = TextAnchor.MiddleLeft}
                    }, check.IPAndPort);

                    container.Add(new CuiLabel
                    {
                        RectTransform = {AnchorMin = "0 1", AnchorMax = "1 1", OffsetMin                                 = "15 -45", OffsetMax                     = "-15 -5"},
                        Text          = {FadeIn    = fadeSpeed, Text  = ((check.Status) ? $"{check.GetOnline()} / {check.MaxOnline}" : "OFFLINE"), Font = "robotocondensed-regular.ttf", FontSize = 32, Color = "1 1 1 0.4", Align = TextAnchor.MiddleRight}
                    }, check.IPAndPort);

                    container.Add(new CuiPanel
                    {
                        RectTransform = {AnchorMin = "0 0", AnchorMax = "1 0", OffsetMin = "15 10", OffsetMax = "-15 30"},
                        Image         = {FadeIn    = fadeSpeed, Color = ((check.Status) ? "1 1 1 0.4" : "1 0.2 0 0.4")}
                    }, check.IPAndPort, check.IPAndPort + ".Online");

                    container.Add(new CuiPanel
                    {
                        RectTransform = {AnchorMin = "0 0", AnchorMax = $"{(float) check.GetOnline() / check.MaxOnline} 1", OffsetMin = "0 0", OffsetMax = "0 0"},
                        Image         = {FadeIn    = fadeSpeed, Color = "0.6 0.9 0.6 0.8"}
                    }, check.IPAndPort + ".Online");
                }
                else 
                {
                    container.Add(new CuiPanel
                    {
                        RectTransform = {AnchorMin = "1 0", AnchorMax = "1 1", OffsetMin = "3 0", OffsetMax = "83 0"},
                        Image         = {FadeIn    = fadeSpeed, Color = "1 1 1 0.03008521"}
                    }, check.IPAndPort, check.IPAndPort + ".Help");

                    container.Add(new CuiButton
                    {
                        RectTransform = {AnchorMin = "0 0", AnchorMax  = "1 1", OffsetMin                                 = "10 10", OffsetMax = "-10 -10"},
                        Button        = {Color     = "1 1 1 0", Sprite = "assets/icons/circle_closed_toedge.png", Command = $"UI_OurServersHandler hide"},
                        Text          = {FadeIn    = fadeSpeed, Text   = "X", Align                                       = TextAnchor.MiddleCenter, Font = "robotocondensed-bold.ttf", FontSize = 50, Color = "1 1 1 0.7"}
                    }, check.IPAndPort + ".Help");

                    container.Add(new CuiLabel
                    {
                        RectTransform = {AnchorMin = "0 0", AnchorMax = "1 1", OffsetMin        = "15 15", OffsetMax                      = "-15 -15"},
                        Text          = {FadeIn    = fadeSpeed, Text  = check.Description, Font = "robotocondensed-regular.ttf", FontSize = 16, Color = "1 1 1 1", Align = TextAnchor.MiddleCenter}
                    }, check.IPAndPort);
                }

                if (startPosition < 0.5f)
                {
                    startPosition = 0.75f;
                }
                else if (startPosition > 0.5f)
                {
                    startPosition = 0.25f;
                    topPosition -= panelWidth + 15;
                }
                else
                {
                    topPosition -= panelWidth + 15;
                }
            }

            CuiHelper.AddUi(player, container);
        }

        #endregion
    }
}