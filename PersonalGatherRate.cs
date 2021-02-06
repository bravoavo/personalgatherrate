using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using Oxide.Core.Plugins;
using UnityEngine;
using Oxide.Core;
using System;
using Oxide.Core.Configuration;

namespace Oxide.Plugins
{
    [Info("PersonalGatherRate", "bravoavo", "0.0.1")]
    [Description("Take control of the map")]

    class PersonalGatherRate : RustPlugin
    {
        ConfigData configData;
        private DynamicConfigFile data;
        readonly bool debug = true;
        private const string permUse = "personalgatherrate.admin";
        public Dictionary<string, int> gatherMultiplier = new Dictionary<string, int>();
        readonly string datafile_name = "personalgatherrate.data";

        #region Localization
        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoPermission"] = "<color=orange>[PGR]</color> You do not have the permissions to use this command.",
                ["Usage"] = "<color=orange>[PGR]</color> Usage: /pgr set STEAMID RATE1-50 | remove STEAMID | check STEAMID",
                ["StatsErr"] = "<color=orange>[PGR]</color> Player Not Found",
                ["Stats"] = "<color=orange>[PGR]</color> Player {0} has Gather Rate {1}"
            }, this);
        }

        #endregion

        #region OxideHooks
        private void Init()
        {
            permission.RegisterPermission(permUse, this);
            if (Interface.Oxide.DataFileSystem.ExistsDatafile(datafile_name))
            {
                data = Interface.Oxide.DataFileSystem.GetDatafile(datafile_name);
            }
            LoadData();
        }


        void OnCollectiblePickup(Item item, BasePlayer player)
        {
            string teampid;
            teampid = player.UserIDString;
            if (debug) Puts("Collect. Start amount is " + item.amount);
            if (gatherMultiplier.ContainsKey(teampid))
            {
                if (gatherMultiplier[teampid] > 0)
                {
                    float multiplier = (float)gatherMultiplier[teampid];
                    item.amount = (int)(item.amount * multiplier);
                }
            }
        }


        object OnDispenserGather(ResourceDispenser dispenser, BasePlayer player, Item item)
        {
            string teampid;
            teampid = player.UserIDString;
            if (debug) Puts("Dispenser. Start amount is " + item.amount);
            if (gatherMultiplier.ContainsKey(teampid))
            {
                if (gatherMultiplier[teampid] > 0)
                {
                    float multiplier = (float)gatherMultiplier[teampid];
                    if (debug) Puts("Multiplier is " + multiplier);
                    item.amount = (int)(item.amount * multiplier);
                }
            }
            return null;
        }
        #endregion

        #region ChatCommand
        [ChatCommand("pgr")]
        void pgrCommand(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, permUse))
            {
                player.ChatMessage(Lang("NoPermission", player.UserIDString));
                return;
            }
            if (debug) Puts("Args lenght " + args.Length);     
            if (args.Length > 1)
            {
                switch (args[0])
                {
                    default:
                        player.ChatMessage(Lang("Usage", player.UserIDString));
                        break;
                    case "set":
                        if (args.Length == 3)
                        {
                            int grate = Convert.ToInt32(args[2]);
                            if (grate > 1 && grate < 50)
                            {
                                string teampid = args[1];
                                if (!gatherMultiplier.ContainsKey(teampid))
                                {
                                    gatherMultiplier.Add(teampid, grate);
                                    if (debug) Puts("Gather rate " + grate);
                                }
                                else
                                {
                                    gatherMultiplier[teampid] = grate;
                                    if (debug) Puts("Gather rate increased up to X" + grate);
                                }
                            }
                            SaveData();
                        }
                        break;

                    case "remove":
                        if (args.Length == 2)
                        {
                            string teampid = args[1]; 
                            if (gatherMultiplier.ContainsKey(teampid))
                            {
                                gatherMultiplier.Remove(teampid);
                            }
                            else player.ChatMessage(Lang("StatsErr", player.UserIDString));
                            SaveData();
                        }
                        break;

                    case "check":
                        if (args.Length == 2)
                        {
                            string teampid = args[1];
                            if (gatherMultiplier.ContainsKey(teampid))
                            {
                                player.ChatMessage(Lang("Stats", player.UserIDString, teampid, gatherMultiplier[teampid]));
                            }
                            else player.ChatMessage(Lang("StatsErr", player.UserIDString));
                        }
                        break;
            }
 
                return;
            }
            else
            {
                player.ChatMessage(Lang("Usage", player.UserIDString));
                return;
            }
        }
        #endregion

        #region DataManagement

        class ConfigData
        {
            public Dictionary<string, int> GatherRateData = new Dictionary<string, int>();
        }

        private void LoadData()
        {
            gatherMultiplier.Clear();
            try
            {
                if (debug) Puts("A try to use existing configuration");
                configData = data.ReadObject<ConfigData>();
                gatherMultiplier = configData.GatherRateData;
            }
            catch
            {
                if (debug) Puts("Existing configuration not found. A new configuration creating.");
                configData = new ConfigData();
                gatherMultiplier = configData.GatherRateData;
            }

        }

        private void SaveData()
        {
            data = Interface.Oxide.DataFileSystem.GetDatafile(datafile_name);
            data.WriteObject(configData);
        }
        #endregion
    }
}