using Facepunch;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    // Credit to Orange for advising to use Reserved8 flag
    [Info( "Powerless Turrets", "August", "3.0.3" )]
    class PowerlessTurrets : RustPlugin
    {
        #region Fields
        public static PowerlessTurrets Instance;

        private TurretManager turretManager;
        private Configuration config;
        private Data data;

        private const string permUse = "powerlessturrets.use";
        private const string permUseRadius = "powerlessturrets.radius";
        private const string permUseSamRadius = "powerlessturrets.samradius";
        #endregion

        #region Configuration
        private class Configuration
        {
            [JsonProperty( PropertyName = "Range at which turrets can be toggled", Order = 0 )]
            public float Range { get; set; }
            [JsonProperty( PropertyName = "Command for toggling individual turrets", Order = 1 )]
            public string ToggleCommand { get; set; }
            [JsonProperty( PropertyName = "Command for toggling turrets in TC zone", Order = 2 )]
            public string ToggleTCCommand { get; set; }
            [JsonProperty( PropertyName = "Command for toggling sams in TC zone", Order = 3 )]
            public string ToggleSamTcCommand { get; set; }
        }
        protected override void LoadDefaultConfig()
        {
            Config.WriteObject( GetDefaultConfig(), true );
        }
        private void SaveConfig()
        {
            Config.WriteObject( config, true );
        }
        private Configuration GetDefaultConfig()
        {
            return new Configuration
            {
                Range = 10f,
                ToggleCommand = "turret",
                ToggleTCCommand = "turret.tc",
                ToggleSamTcCommand = "sam.tc"
            };
        }
        #endregion

        #region Data

        private class Data
        {
            public List<uint> AutoTurrets = new List<uint>();
            public List<uint> SamSites = new List<uint>();
        }

        private void SaveData()
        {
            data.AutoTurrets = turretManager.OnlineTurrets;
            data.SamSites = turretManager.OnlineSams;

            Interface.Oxide.DataFileSystem.WriteObject( Name, data );
        }

        #endregion

        #region Localization
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages( new Dictionary<string, string>
            {
                [ "TurretsToggled" ] = "You have toggled {0} turrets.",
                [ "NoPermission" ] = "You do not have permission to use this command.",
                [ "InvalidEntity" ] = "This is not a valid entity.",
                [ "NoTurretsFound" ] = "There are no valid turrets in your area.",
                [ "NoPermThisTurret" ] = "You do not have permission to toggle this turret."
            }, this );
        }

        private string Lang( string key, string id = null, params object[] args ) => string.Format( lang.GetMessage( key, this, id ), args );
        #endregion

        #region Hooks
        private void Init()
        {
            config = Config.ReadObject<Configuration>();
            data = Interface.Oxide.DataFileSystem.ReadObject<Data>( Name );
            turretManager = new TurretManager();
            Instance = this;

            permission.RegisterPermission( permUse, this );
            permission.RegisterPermission( permUseRadius, this );
            permission.RegisterPermission( permUseSamRadius, this );

            cmd.AddChatCommand( config.ToggleCommand, this, "TurretCommand" );
            cmd.AddChatCommand( config.ToggleTCCommand, this, "ToggleTurretsInTc" );
            cmd.AddChatCommand( config.ToggleSamTcCommand, this, "ToggleSamsInTc" );
        }
        private void OnServerInitialized()
        {
            foreach (var ent in BaseEntity.serverEntities)
            {
                if (ent is AutoTurret)
                {
                    AutoTurret turret = ent as AutoTurret;
                    if (data.AutoTurrets.Contains(turret.net.ID))
                    {
                        turretManager.PowerTurretOn( turret );
                    }
                }
                else if (ent is SamSite)
                {
                    SamSite sam = ent as SamSite;
                    if (data.SamSites.Contains(sam.net.ID))
                    {
                        turretManager.PowerSamsiteOn( sam );
                    }                    
                }
            }
        }
        private void OnServerSave()
        {
            SaveData();
        }
        #endregion

        #region Commands
        private void TurretCommand( BasePlayer player )
        {
            if ( permission.UserHasPermission( player.UserIDString, permUse ) == false )
            {
                player.ChatMessage( Lang( "NoPermission", player.UserIDString ) );
                return;
            }

            RaycastHit hit;

            if ( Physics.Raycast(player.eyes.position, ( player.eyes.rotation * Vector3.forward ), out hit, config.Range) == false )
            {
                return;
            }

            BaseEntity hitEntity = hit.GetEntity();
            if (hitEntity is AutoTurret)
            {
                turretManager.ToggleTurret( hitEntity as AutoTurret, player );
            }
            else if (hitEntity is SamSite)
            {
                turretManager.ToggleSamsite( hitEntity as SamSite, player );
            }
            else
            {
                player.ChatMessage( Lang( "InvalidEntity", player.UserIDString ) );
            }
        }

        private void ToggleTurretsInTc( BasePlayer player )
        {
            if ( !permission.UserHasPermission( player.UserIDString, permUseRadius ) )
            {
                player.ChatMessage( Lang( "NoPermission", player.UserIDString ) );
            }
            turretManager.ToggleTurretsInTcRange( player );
        }
        private void ToggleSamsInTc( BasePlayer player )
        {
            if ( !permission.UserHasPermission( player.UserIDString, permUseRadius ) )
            {
                player.ChatMessage( Lang( "NoPermission", player.UserIDString ) );
            }
            turretManager.ToggleSamsInTcRange( player );
        }
        #endregion

        #region Turret Manager
        private class TurretManager
        {

            #region Autoturrets
            public List<uint> OnlineTurrets 
            { 
                get 
                {
                    List<uint> ids = new List<uint>();
                    foreach (AutoTurret turret in onlineTurrets)
                    {
                        if (turret.net != null)
                        {
                            ids.Add( turret.net.ID );
                        }
                    }
                    return ids;
                } 
                private set { }
            }
            private List<AutoTurret> onlineTurrets = new List<AutoTurret>();
            public void ToggleTurret( AutoTurret turret, BasePlayer player = null )
            {
                if ( turret == null )
                {
                    return;
                }
                if (player != null)
                {
                    if (!turret.IsAuthed(player))
                    {
                        player.ChatMessage( Instance.Lang( "NoPermThisTurret", player.UserIDString ) );
                        return;
                    }
                }
                // Turn turret on if off, off if on, make sure data is right
                if ( turret.IsOnline() )
                {
                    PowerTurretOff( turret );
                }
                else
                {
                    PowerTurretOn( turret );
                }
            }
            public void PowerTurretOn(AutoTurret turret)
            {
                
                turret.SetFlag( BaseEntity.Flags.Reserved8, true );
                turret.InitiateStartup();
                if ( !onlineTurrets.Contains( turret ) )
                {
                    onlineTurrets.Add( turret );
                }
            }
            private void PowerTurretOff(AutoTurret turret)
            {
                turret.SetFlag( BaseEntity.Flags.Reserved8, false );
                turret.InitiateShutdown();
                if ( onlineTurrets.Contains( turret ) )
                {
                    onlineTurrets.Remove( turret );
                }
            }
            public void ToggleTurretsInTcRange( BasePlayer player )
            {
                var tc = player.GetBuildingPrivilege();

                if ( tc == null )
                {
                    return;
                }
                // This line may be kind of slow ... is there any other way to do this?
                List<AutoTurret> turretList = Pool.GetList<AutoTurret>();
                foreach (AutoTurret turret in BaseEntity.serverEntities.OfType<AutoTurret>())
                {
                    if ( turret.GetBuildingPrivilege() == tc && turret.IsAuthed( player ) ) 
                    {
                        turretList.Add( turret );
                    }
                }
                if ( turretList.Count < 1 )
                {
                    return;
                }          
                foreach ( AutoTurret turret in turretList )
                {
                    ToggleTurret( turret );
                }
                player.ChatMessage( Instance.Lang( "TurretsToggled", player.UserIDString, turretList.Count ) );
                Pool.FreeList( ref turretList );
            }
            #endregion

            #region SAM Turrets
            public List<uint> OnlineSams
            {
                get
                {
                    List<uint> ids = new List<uint>();
                    foreach ( SamSite sam in onlineSams )
                    {
                        if ( sam.net != null )
                        {
                            ids.Add( sam.net.ID );
                        }
                    }
                    return ids;
                }
                private set { }
            }
            private List<SamSite> onlineSams = new List<SamSite>();
            public void ToggleSamsite(SamSite sam, BasePlayer player = null)
            {
                if (player != null)
                {
                    if (sam.GetBuildingPrivilege() != player.GetBuildingPrivilege())
                    {
                        player.ChatMessage( Instance.Lang( "NoPermThisTurret", player.UserIDString ) );
                        return;
                    }             
                }
                if ( sam.IsPowered() || onlineSams.Contains( sam ) )
                {
                    PowerSamsiteOff( sam );
                }
                else
                {
                    PowerSamsiteOn( sam );
                }
                sam.SendNetworkUpdate();
            }

            public void PowerSamsiteOn(SamSite sam)
            {
                sam.UpdateHasPower( 25, 0 );
                sam.SetFlag( BaseEntity.Flags.Reserved8, true );
                if ( !onlineSams.Contains( sam ) )
                {
                    onlineSams.Add( sam );
                }               
            }
            private void PowerSamsiteOff(SamSite sam)
            {
                sam.UpdateHasPower( 0, 0 );
                sam.SetFlag( BaseEntity.Flags.Reserved8, false );
                if (onlineSams.Contains(sam))
                {
                    onlineSams.Remove( sam );
                }
            }
            public void ToggleSamsInTcRange( BasePlayer player )
            {
                var tc = player.GetBuildingPrivilege();

                if ( tc == null )
                {
                    return;
                }
                // This line may be kind of slow ... is there any other way to do this?
                List<SamSite> turretList = Pool.GetList<SamSite>();
                foreach ( SamSite turret in BaseEntity.serverEntities.OfType<SamSite>() )
                {
                    if ( turret.GetBuildingPrivilege() == tc)
                    {
                        turretList.Add( turret );
                    }
                }
                if ( turretList.Count < 1 )
                {
                    return;
                }
                foreach ( SamSite sam in turretList )
                {
                    ToggleSamsite( sam );
                }
                player.ChatMessage( Instance.Lang( "TurretsToggled", player.UserIDString, turretList.Count ) );
                Pool.FreeList( ref turretList );
            }
            #endregion
        } 
        #endregion
    }
}
