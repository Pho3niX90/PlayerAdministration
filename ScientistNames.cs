using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("Scientist Names", "Ultra", "1.0.3")]
    [Description("Gives real names to scientists and bandits (instead of numbers)")]

    class ScientistNames : RustPlugin
    {
        #region Fields

        Random rnd;
        bool initialized = false;
        int renameCount = 0;
        List<string> surnameList = new List<string>();
        List<string> currentActiveNames = new List<string>();
        bool forced = false;

        #endregion       

        #region Oxide Hooks

        void Init()
        {
            Unsubscribe(nameof(OnEntitySpawned));
        }

        void OnServerInitialized()
        {
            rnd = new Random();
            renameCount = 0;

            foreach (BaseNetworkable baseNetworkable in BaseNetworkable.serverEntities.Where(w => w is BasePlayer && (w is NPCPlayer || w is HTNPlayer)).Cast<BasePlayer>())
            {
                if (!baseNetworkable.ShortPrefabName.Contains("scientist") && !baseNetworkable.ShortPrefabName.Contains("bandit")) continue;

                BasePlayer basePlayer = (BasePlayer)baseNetworkable;
                string oldName = basePlayer.displayName;
                RenameBasePlayer(basePlayer, GetAbbrevation(basePlayer));
                if (oldName != basePlayer.displayName)
                {
                    Log($"{oldName} renamed to {basePlayer.displayName}", console: true);
                    renameCount++;
                }
            }

            Log($"{renameCount} scientists (bandits) renamed", console: true);
            if (currentActiveNames.Count - renameCount > 0) Log($"{currentActiveNames.Count - renameCount} already named scientists (bandits) found", console: true);

            Subscribe(nameof(OnEntitySpawned));
            initialized = true;
        }

        void OnEntitySpawned(BaseEntity entity)
        {
            if (entity is BasePlayer && (entity is NPCPlayer || entity is HTNPlayer))
            {
                BasePlayer basePlayer = (BasePlayer)entity;
                string oldName = basePlayer.displayName;
                RenameBasePlayer(basePlayer, GetAbbrevation(basePlayer));

                if (oldName != basePlayer.displayName)
                {
                    Log($"{oldName} renamed to {basePlayer.displayName}", console: true);
                }
            }
        }

        #endregion

        #region Death Hook

        void OnDeathNotice(Dictionary<string, object> data, string message)
        {
            if (!initialized) return;
            object victim = data["VictimEntity"];
            if (victim == null) return;

            BasePlayer basePlayer = victim as BasePlayer;

            if (basePlayer != null && currentActiveNames.Contains(basePlayer.displayName))
            {
                currentActiveNames.Remove(basePlayer.displayName);
            }
        }
        #endregion

        #region Core

        void RenameBasePlayer(BasePlayer basePlayer, string abbrevation)
        {
            int n = 0;
            if (int.TryParse(basePlayer.displayName, out n) || forced)
            {
                for (int attempt = 0; attempt < 5; attempt++)
                {
                    string newName = RenameBasePlayer(abbrevation);
                    if (!string.IsNullOrEmpty(newName))
                    {
                        basePlayer.displayName = newName;
                        return;
                    }
                }

                Log("Name generator failed. No unique name found", console: true, logType: LogType.ERROR);
            }

            if (!string.IsNullOrEmpty(basePlayer.displayName)) currentActiveNames.Add(basePlayer.displayName);
        }

        string RenameBasePlayer(string abbrevation)
        {
            if (surnameList.Count > 0)
            {
                string newName = (string.Format("{0} {1} {2}", abbrevation, GetFirstnameFirstLetter().ToUpper(), surnameList[rnd.Next(0, surnameList.Count)].Trim())).Trim().Replace("  ", " ");
                if (!currentActiveNames.Contains(newName))
                {
                    return newName;
                }
            }

            return null;
        }

        string GetAbbrevation(BasePlayer basePlayer)
        {
            if (!configData.UseTitle) return string.Empty;
            else if (basePlayer.ShortPrefabName.Contains("bandit")) return configData.BanditTitle;
            else if (basePlayer.ShortPrefabName.Contains("scientist")) return configData.ScientistTitle;
            else
            {
                Log($"Unknown ShortPrefabName: {basePlayer.ShortPrefabName}", console: true, logType: LogType.WARNING);
                return string.Empty;
            }
        }

        string GetFirstnameFirstLetter()
        {
            if (!configData.UseFirstNameFirstLetter) return string.Empty;

            int num = rnd.Next(0, 26); // Zero to 25
            char letter = (char)('a' + num);
            return letter.ToString() + ".";
        }

        #endregion

        #region Config

        private ConfigData configData;

        private class ConfigData
        {
            [JsonProperty(PropertyName = "UseTitle")]
            public bool UseTitle;

            [JsonProperty(PropertyName = "UseFirstNameFirstLetter")]
            public bool UseFirstNameFirstLetter;

            [JsonProperty(PropertyName = "ScientistTitle")]
            public string ScientistTitle;

            [JsonProperty(PropertyName = "BanditTitle")]
            public string BanditTitle;

            [JsonProperty(PropertyName = "SurnameList")]
            public string SurnameList;

            [JsonProperty(PropertyName = "LogInFile")]
            public bool LogInFile;

            [JsonProperty(PropertyName = "LogInConsole")]
            public bool LogInConsole;
        }

        protected override void LoadConfig()
        {
            try
            {
                base.LoadConfig();
                configData = Config.ReadObject<ConfigData>();
                if (configData == null)
                {
                    LoadDefaultConfig();
                }
            }
            catch
            {
                LoadDefaultConfig();
            }

            surnameList = configData.SurnameList.Split(',').Distinct().ToList();
            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            configData = new ConfigData()
            {
                LogInConsole = false,
                LogInFile = false,
                UseTitle = true,
                UseFirstNameFirstLetter = true,
                ScientistTitle = "scientist",
                BanditTitle = "bandit",
                SurnameList = "Smith, Johnson, Williams, Jones, Brown, Davis, Miller, Wilson, Moore, Taylor, Anderson, Thomas, Jackson, White, Harris, Martin, Thompson, Garcia, Martinez, Robinson, Clark, Rodriguez, Lewis, Lee, Walker, Hall, Allen, Young, Hernandez, King, Wright, Lopez, Hill, Scott, Green, Adams, Baker, Gonzalez, Nelson, Carter, Mitchell, Perez, Roberts, Turner, Phillips, Campbell, Parker, Evans, Edwards, Collins, Stewart, Sanchez, Morris, Rogers, Reed, Cook, Morgan, Bell, Murphy, Bailey, Rivera, Cooper, Richardson, Cox, Howard, Ward, Torres, Peterson, Gray, Ramirez, James, Watson, Brooks, Kelly, Sanders, Price, Bennett, Wood, Barnes, Ross, Henderson, Coleman, Jenkins, Perry, Powell, Long, Patterson, Hughes, Flores, Washington, Butler, Simmons, Foster, Gonzales, Bryant, Alexander, Russell, Griffin, Diaz, Hayes, Myers, Ford, Hamilton, Graham, Sullivan, Wallace, Woods, Cole, West, Jordan, Owens, Reynolds, Fisher, Ellis, Harrison, Gibson, McDonald, Cruz, Marshall, Ortiz, Gomez, Murray, Freeman, Wells, Webb, Simpson, Stevens, Tucker, Porter, Hunter, Hicks, Crawford, Henry, Boyd, Mason, Morales, Kennedy, Warren, Dixon, Ramos, Reyes, Burns, Gordon, Shaw, Holmes, Rice, Robertson, Hunt, Black, Daniels, Palmer, Mills, Nichols, Grant, Knight, Ferguson, Rose, Stone, Hawkins, Dunn, Perkins, Hudson, Spencer, Gardner, Stephens, Payne, Pierce, Berry, Matthews, Arnold, Wagner, Willis, Ray, Watkins, Olson, Carroll, Duncan, Snyder, Hart, Cunningham, Bradley, Lane, Andrews, Ruiz, Harper, Fox, Riley, Armstrong, Carpenter, Weaver, Greene, Lawrence, Elliott, Chavez, Sims, Austin, Peters, Kelley, Franklin, Lawson, Fields, Gutierrez, Ryan, Schmidt, Carr, Vasquez, Castillo, Wheeler, Chapman, Oliver, Montgomery, Richards, Williamson, Johnston, Banks, Meyer, Bishop, McCoy, Howell, Alvarez, Morrison, Hansen, Fernandez, Garza, Harvey, Little, Burton, Stanley, Nguyen, George, Jacobs, Reid, Kim, Fuller, Lynch, Dean, Gilbert, Garrett, Romero, Welch, Larson, Frazier, Burke, Hanson, Day, Mendoza, Moreno, Bowman, Medina, Fowler, Brewer, Hoffman, Carlson, Silva, Pearson, Holland, Douglas, Fleming, Jensen, Vargas, Byrd, Davidson, Hopkins, May, Terry, Herrera, Wade, Soto, Walters, Curtis, Neal, Caldwell, Lowe, Jennings, Barnett, Graves, Jimenez, Horton, Shelton, Barrett, Obrien, Castro, Sutton, Gregory, Mckinney, Lucas, Miles, Craig, Rodriquez, Chambers, Holt, Lambert, Fletcher, Watts, Bates, Hale, Rhodes, Pena, Beck, Newman, Haynes, McDaniel, Mendez, Bush, Vaughn, Parks, Dawson, Santiago, Norris, Hardy, Love, Steele, Curry, Powers, Schultz, Barker, Guzman, Page, Munoz, Ball, Keller, Chandler, Weber, Leonard, Walsh, Lyons, Ramsey, Wolfe, Schneider, Mullins, Benson, Sharp, Bowen, Daniel, Barber, Cummings, Hines, Baldwin, Griffith, Valdez, Hubbard, Salazar, Reeves, Warner, Stevenson, Burgess, Santos, Tate, Cross, Garner, Mann, Mack, Moss, Thornton, Dennis, McGee, Farmer, Delgado, Aguilar, Vega, Glover, Manning, Cohen, Harmon, Rodgers, Robbins, Newton, Todd, Blair, Higgins, Ingram, Reese, Cannon, Strickland, Townsend, Potter, Goodwin, Walton, Rowe, Hampton, Ortega, Patton, Swanson, Joseph, Francis, Goodman, Maldonado, Yates, Becker, Erickson, Hodges, Rios, Conner, Adkins, Webster, Norman, Malone, Hammond, Flowers, Cobb, Moody, Quinn, Blake, Maxwell, Pope, Floyd, Osborne, Paul, McCarthy, Guerrero, Lindsey, Estrada, Sandoval, Gibbs, Tyler, Gross, Fitzgerald, Stokes, Doyle, Sherman, Saunders, Wise, Colon, Gill, Alvarado, Greer, Padilla, Simon, Waters, Nunez, Ballard, Schwartz, McBride, Houston, Christensen, Klein, Pratt, Briggs, Parsons, McLaughlin, Zimmerman, French, Buchanan, Moran, Copeland, Roy, Pittman, Brady, McCormick, Holloway, Brock, Poole, Frank, Logan, Owen, Bass, Marsh, Drake, Wong, Jefferson, Park, Morton, Abbott, Sparks, Patrick, Norton, Huff, Clayton, Massey, Lloyd, Figueroa, Carson, Bowers, Roberson, Barton, Tran, Lamb, Harrington, Casey, Boone, Cortez, Clarke, Mathis, Singleton, Wilkins, Cain, Bryan, Underwood, Hogan, Mckenzie, Collier, Luna, Phelps, McGuire, Allison, Bridges, Wilkerson, Nash, Summers, Atkins"
            };
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(configData, true);
            base.SaveConfig();
        }

        #endregion

        #region Log

        void Log(string message, bool console = false, LogType logType = LogType.INFO, string fileName = "")
        {
            if (string.IsNullOrEmpty(fileName)) fileName = this.Title;

            if (configData.LogInFile)
            {
                LogToFile(fileName, $"[{DateTime.Now.ToString("hh:mm:ss")}] {logType} > {message}", this);
            }

            if (configData.LogInConsole)
            {
                Puts($"{message.Replace("\n", " ")}");
            }
        }

        enum LogType
        {
            INFO = 0,
            WARNING = 1,
            ERROR = 2
        }

        #endregion
    }
}
