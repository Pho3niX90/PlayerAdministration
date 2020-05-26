using Facepunch;
using Network;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Clear Night", "Clearshot", "1.1.0")]
    [Description("Always bright nights")]
    class ClearNight : CovalencePlugin
    {
        private PluginConfig _config;
        private EnvSync _envSync;
        private List<DateTime> _fullMoonDates = new List<DateTime> {
            new DateTime(2024, 1, 25),
            new DateTime(2024, 2, 24),
            new DateTime(2024, 3, 25),
            new DateTime(2024, 4, 23),
            new DateTime(2024, 5, 23),
            new DateTime(2024, 6, 21),
            new DateTime(2024, 7, 21),
            new DateTime(2024, 8, 19),
            new DateTime(2024, 9, 17),
            new DateTime(2024, 10, 17),
            new DateTime(2024, 11, 15),
            new DateTime(2024, 12, 15)
        };
        private DateTime _date;
        private int _current = 0;

        [PluginReference("NightVision")]
        RustPlugin NightVisionRef;

        void OnServerInitialized()
        {
            TOD_Sky.Instance.Components.Time.OnDay += OnDay;
            _envSync = BaseNetworkable.serverEntities.OfType<EnvSync>().FirstOrDefault();
            _date = _fullMoonDates[_current];

            timer.Every(5f, () => {
                _envSync.limitNetworking = true;

                if (NightVisionRef != null)
                    NightVisionRef?.CallHook("BlockEnvUpdates", true);

                List<Connection> subscribers = _envSync.net.group.subscribers;
                if (subscribers != null && subscribers.Count > 0)
                {
                    for (int i = 0; i < subscribers.Count; i++)
                    {
                        Connection connection = subscribers[i];
                        global::BasePlayer basePlayer = connection.player as global::BasePlayer;

                        if (NightVisionRef != null && !(basePlayer == null) && (bool)NightVisionRef?.CallHook("IsPlayerTimeLocked", basePlayer)) continue;

                        if (!(basePlayer == null) && Net.sv.write.Start())
                        {
                            connection.validate.entityUpdates = connection.validate.entityUpdates + 1;
                            BaseNetworkable.SaveInfo saveInfo = new global::BaseNetworkable.SaveInfo
                            {
                                forConnection = connection,
                                forDisk = false
                            };
                            Net.sv.write.PacketID(Message.Type.Entities);
                            Net.sv.write.UInt32(connection.validate.entityUpdates);
                            using (saveInfo.msg = Pool.Get<Entity>())
                            {
                                _envSync.Save(saveInfo);
                                saveInfo.msg.environment.dateTime = _date.AddHours(TOD_Sky.Instance.Cycle.Hour).ToBinary();
                                if (_config.disableFogAtNight && TOD_Sky.Instance.IsNight)
                                {
                                    saveInfo.msg.environment.fog = 0;
                                    saveInfo.msg.environment.rain = 0;
                                    saveInfo.msg.environment.clouds = 0;
                                }
                                if (saveInfo.msg.baseEntity == null)
                                {
                                    LogError(this + ": ToStream - no BaseEntity!?");
                                }
                                if (saveInfo.msg.baseNetworkable == null)
                                {
                                    LogError(this + ": ToStream - no baseNetworkable!?");
                                }
                                saveInfo.msg.ToProto(Net.sv.write);
                                _envSync.PostSave(saveInfo);
                                Net.sv.write.Send(new SendInfo(connection));
                            }
                        }
                    }
                }
            });
        }

        void Unload()
        {
            TOD_Sky.Instance.Components.Time.OnDay -= OnDay;
            _envSync.limitNetworking = false;

            if (NightVisionRef != null)
                NightVisionRef?.CallHook("BlockEnvUpdates", false);
        }

        void OnDay()
        {
            if (_current >= _fullMoonDates.Count) _current = 0;

            _date = _fullMoonDates[_current];
            _current++;
        }

        #region Config

        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(GetDefaultConfig(), true);
        }

        private PluginConfig GetDefaultConfig()
        {
            return new PluginConfig();
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<PluginConfig>();
        }

        private class PluginConfig
        {
            public bool disableFogAtNight = true;
        }

        #endregion
    }
}
