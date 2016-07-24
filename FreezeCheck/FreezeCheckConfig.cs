using Newtonsoft.Json;
using System;
using System.IO;
using TShockAPI;
using TShockAPI.Hooks;

namespace FreezeCheck
{
    public class FreezeCheckConfig
    {
        private static string ConfigFile = Path.Combine("tshock", "FreezeCheck.json");

        public string BannedGroup = "guest";

        public int BannedTime = 60;

        public string GuardianName = "AntiCheat";

        public string GuardianMode = "";

        public Security Autoactions = new Security();

        public FreezeCheckConfig()
        {
            Autoactions = new Security();
        }

        private FreezeCheckConfig Write()
        {
            File.WriteAllText(ConfigFile, JsonConvert.SerializeObject(this, Formatting.Indented));
            return this;
        }

        private static FreezeCheckConfig Read()
        {
            if (!File.Exists(ConfigFile))
            {
                WriteDefaults();
            }
            return JsonConvert.DeserializeObject<FreezeCheckConfig>(File.ReadAllText(ConfigFile));
        }

        private static void WriteDefaults()
        {
            FreezeCheckConfig freezeCheckConfig = new FreezeCheckConfig();
            TShock.Log.ConsoleInfo("Create default");
            freezeCheckConfig.Autoactions.Ban.Add(new ItemGroup());
            freezeCheckConfig.Autoactions.Ban[0].Reason = "invedit";
            freezeCheckConfig.Autoactions.Ban[0].Items.AddRange(new string[]
            {
                "Gravity Globe",
                "S.D.M.G.",
                "Steampunk Wings",
                "White Tuxedo Shirt",
                "White Tuxedo Pants",
                "Zebra Skin",
                "Leopard Skin",
                "Tiger Skin"
            });
            freezeCheckConfig.Autoactions.Ban.Add(new ItemGroup());
            freezeCheckConfig.Autoactions.Ban[1].Reason = "invedit(overstack)";
            freezeCheckConfig.Autoactions.Ban[1].Items.Add("%overstack%");
            freezeCheckConfig.Autoactions.Ban.Add(new ItemGroup());
            freezeCheckConfig.Autoactions.Ban[2].Reason = "invedit(zerostack)";
            freezeCheckConfig.Autoactions.Ban[2].Items.Add("%zerostack%");
            freezeCheckConfig.Autoactions.Kick.Add(new ItemGroup());
            freezeCheckConfig.Autoactions.Kick[0].Reason = "invedit";
            freezeCheckConfig.Autoactions.Kick[0].Items.AddRange(new string[]
            {
                "Aaron's Breastplate",
                "Aaron's Helmet",
                "Aaron's Leggings",
                "Jim's Breastplate",
                "Jim's Helmet",
                "Jim's Leggings"
            });
            freezeCheckConfig.Autoactions.Kick[0].Items.AddRange(new string[]
            {
                "Cenx's Breastplate",
                "Cenx's Dress",
                "Cenx's Dress Pants",
                "Cenx's Leggings",
                "Cenx's Tiara",
                "Cenx's Wings"
            });
            freezeCheckConfig.Autoactions.Kick[0].Items.AddRange(new string[]
            {
                "Red's Armor",
                "Red's Breastplate",
                "Red's Helmet",
                "Red's Leggings",
                "Red's Wings"
            });
            freezeCheckConfig.Autoactions.Kick[0].Items.AddRange(new string[]
            {
                "Crowno's Breastplate",
                "Crowno's Leggings",
                "Crowno's Mask",
                "Crowno's Wings"
            });
            freezeCheckConfig.Autoactions.Kick[0].Items.AddRange(new string[]
            {
                "D-Town's Breastplate",
                "D-Town's Helmet",
                "D-Town's Leggings",
                "D-Town's Wings"
            });
            freezeCheckConfig.Autoactions.Kick[0].Items.AddRange(new string[]
            {
                "Will's Breastplate",
                "Will's Helmet",
                "Will's Leggings",
                "Will's Wings"
            });
            freezeCheckConfig.Autoactions.Kick.Add(new ItemGroup());
            freezeCheckConfig.Autoactions.Kick[1].Reason = "destruction";
            freezeCheckConfig.Autoactions.Kick[1].Items.Add("Explosives");
            freezeCheckConfig.Autoactions.Kick.Add(new ItemGroup());
            freezeCheckConfig.Autoactions.Kick[2].Reason = "dirty";
            freezeCheckConfig.Autoactions.Kick[2].Items.Add("Dirt Rod");
            TShock.Log.ConsoleInfo("Fill default");
            File.WriteAllText(ConfigFile, JsonConvert.SerializeObject(freezeCheckConfig, Formatting.Indented));
            TShock.Log.ConsoleInfo("Write default");
        }

        public static void Load()
        {
            try
            {
                FreezeCheck.Config = Read().Write();
            }
            catch (Exception ex)
            {
                TShock.Log.ConsoleError("[FreezeCheck] Config failed. Check logs for more details.");
                TShock.Log.Error(ex.ToString());
            }
        }

        public static void Reload(ReloadEventArgs args)
        {
            try
            {
                FreezeCheck.Config = Read().Write();
                args.Player.SendSuccessMessage("[FreezeCheck] Config reloaded successfully.");
            }
            catch (Exception ex)
            {
                args.Player.SendErrorMessage("[FreezeCheck] Reload failed. Check logs for more details.");
                TShock.Log.Error("[FreezeCheck] Config failed:\n" + ex.ToString());
            }
        }
    }
}
