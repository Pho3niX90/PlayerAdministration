// Requires: Clans

using Newtonsoft.Json.Linq;
using Oxide.Core.Plugins;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Clan Team", "deivismac", "1.0.5")]
    [Description("Adds clan members to the same team")]
    class ClanTeam : CovalencePlugin
    {
        #region Definitions

        [PluginReference]
        private Plugin Clans;

        private readonly Dictionary<string, List<ulong>> clans = new Dictionary<string, List<ulong>>();

        #endregion Definitions

        #region Functions

        private bool CompareTeams(List<ulong> currentIds, List<ulong> clanIds)
        {
            foreach (ulong clanId in clanIds)
            {
                if (!currentIds.Contains(clanId))
                {
                    return false;
                }
            }

            return true;
        }

        private void GenerateClanTeam(List<ulong> memberIds)
        {
            if (clans.ContainsKey(ClanTag(memberIds[0])))
            {
                clans.Remove(ClanTag(memberIds[0]));
            }

            clans[ClanTag(memberIds[0])] = new List<ulong>();
            RelationshipManager.PlayerTeam team = RelationshipManager.Instance.CreateTeam();

            foreach (ulong memberId in memberIds)
            {
                BasePlayer player = BasePlayer.FindByID(memberId);
                if (player != null)
                {
                    if (player.currentTeam != 0UL)
                    {
                        RelationshipManager.PlayerTeam current = RelationshipManager.Instance.FindTeam(player.currentTeam);
                        current.RemovePlayer(player.userID);
                    }
                    team.AddPlayer(player);

                    clans[ClanTag(memberId)].Add(player.userID);

                    if (IsAnOwner(player))
                    {
                        team.SetTeamLeader(player.userID);
                    }
                }
            }
        }

        private bool IsAnOwner(BasePlayer player)
        {
            JObject clanInfo = Clans.Call<JObject>("GetClan", Clans.Call<string>("GetClanOf", player.userID));
            return (string)clanInfo["owner"] == player.UserIDString;
        }

        private string ClanTag(ulong memberId)
        {
            return Clans.Call<string>("GetClanOf", memberId);
        }

        private List<ulong> ClanPlayers(BasePlayer player)
        {
            JObject clanInfo = Clans.Call<JObject>("GetClan", Clans.Call<string>("GetClanOf", player.userID));
            return clanInfo["members"].ToObject<List<ulong>>();
        }

        private List<ulong> ClanPlayersTag(string tag)
        {
            JObject clanInfo = Clans.Call<JObject>("GetClan", tag);
            return clanInfo["members"].ToObject<List<ulong>>();
        }

        #endregion Functions

        #region Hooks

        private void OnClanCreate(string tag)
        {
            timer.Once(1f, () =>
            {
                List<ulong> clanPlayers = new List<ulong>();
                JObject clanInfo = Clans.Call<JObject>("GetClan", tag);
                JArray players = clanInfo["members"] as JArray;
                foreach (string memberId in players)
                {
                    ulong clanId;
                    ulong.TryParse(memberId, out clanId);
                    if (clanId != 0UL)
                    {
                        clanPlayers.Add(clanId);
                    }
                }
                GenerateClanTeam(clanPlayers);
            });
        }

        private void OnClanUpdate(string tag)
        {
            GenerateClanTeam(ClanPlayersTag(tag));
        }

        private void OnClanDestroy(string tag)
        {
            BasePlayer player = BasePlayer.FindByID(clans[tag][0]);
            if (player != null)
            {
                RelationshipManager.PlayerTeam team = RelationshipManager.Instance.FindTeam(player.currentTeam);

                foreach (ulong memberId in clans[tag])
                {
                    team.RemovePlayer(memberId);
                }

                RelationshipManager.Instance.DisbandTeam(team);
                clans.Remove(tag);
            }
        }

        private void OnPlayerSleepEnded(BasePlayer player)
        {
            if (!string.IsNullOrEmpty(ClanTag(player.userID)))
            {
                List<ulong> clanPlayers = ClanPlayers(player);
                if (player.currentTeam != 0UL)
                {
                    RelationshipManager.PlayerTeam team = RelationshipManager.Instance.FindTeam(player.currentTeam);
                    if (team != null && CompareTeams(team.members, clanPlayers))
                    {
                        return;
                    }
                }

                GenerateClanTeam(clanPlayers);
            }
        }
    }

    #endregion Hooks
}
