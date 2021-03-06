using System;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("IcefuseStructureGrades", "Pho3niX90", "0.0.1")]
    [Description("Limits which structure grades players can build")]

    public class IcefuseStructureGrades : RustPlugin
    {
        #region Initialization

        void Init()
        {
            var grades = new List<string>();
            foreach (var grade in Enum.GetNames(typeof(BuildingGrade.Enum)))
            {
                if (grade.Equals("None") || grade.Equals("Count")) continue;
                grades.Add(grade);

                // Permissions
                permission.RegisterPermission($"{Title}.{grade}", this);

                // Configuration
                Config[grade] = GetConfig(grade, true);
                SaveConfig();
            }
            Puts("Allowed: " + string.Join(", ", grades.ToArray()));

            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NotAllowed"] = "<color=yellow>{0}</color> has been <color=red>disabled</color>"
            }, this);
        }

        protected override void LoadDefaultConfig()
        {
        }

        #endregion

        object OnStructureUpgrade(BuildingBlock block, BasePlayer player, BuildingGrade.Enum gradeEnum)
        {
            var grade = gradeEnum.ToString();
            if ((bool)Config[grade] || IsAllowed(player.UserIDString, $"{Title}.{grade}")) return null;
            PrintToChat(player, Lang("NotAllowed", player.UserIDString, grade));
            return true;
        }

        #region Helpers

        T GetConfig<T>(string name, T defaultValue) => Config[name] == null ? defaultValue : (T)Convert.ChangeType(Config[name], typeof(T));

        string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        bool IsAllowed(string id, string perm) => permission.UserHasPermission(id, perm);

        #endregion
    }
}