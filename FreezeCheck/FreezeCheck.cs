using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Streams;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Timers;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace FreezeCheck
{
    [ApiVersion(1, 23)]
    public class FreezeCheck : TerrariaPlugin
    {
        public static FreezeCheckConfig Config = new FreezeCheckConfig();

        public static Timer[] BannedTimers = new Timer[Main.maxNetPlayers];

        public override Version Version
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version;
            }
        }

        public override string Name
        {
            get
            {
                return "FreezeCheck";
            }
        }

        public override string Author
        {
            get
            {
                return "Commaster";
            }
        }

        public override string Description
        {
            get
            {
                return "Exposes cheat validation to users.";
            }
        }

        public FreezeCheck(Main game) : base(game)
        {
            Order = -1;
        }

        public override void Initialize()
        {
            List<Command> arg_44_0 = Commands.ChatCommands;
            Command command = new Command(Permissions.cancheck, new CommandDelegate(FreezeCheck.OnFreezeCheck), new string[]
            {
                "unfreeze",
                "fc",
                "freezecheck"
            });
            command.AllowServer = true;
            arg_44_0.Add(command);
            List<Command> arg_7D_0 = Commands.ChatCommands;
            Command command2 = new Command(Permissions.cancheckothers, new CommandDelegate(OnLogInv), new string[]
            {
                "loginventory"
            });
            command2.AllowServer = false;
            arg_7D_0.Add(command2);
            ServerApi.Hooks.GameInitialize.Register(this, new HookHandler<EventArgs>(OnInitialize));
            ServerApi.Hooks.NetGreetPlayer.Register(this, new HookHandler<GreetPlayerEventArgs>(OnNetGreetPlayer));
            ServerApi.Hooks.NetGetData.Register(this, new HookHandler<GetDataEventArgs>(OnNetGetData));
            ServerApi.Hooks.ServerLeave.Register(this, new HookHandler<LeaveEventArgs>(OnServerLeave));
        }

        private void OnInitialize(EventArgs e)
        {
            GeneralHooks.ReloadEvent += new GeneralHooks.ReloadEventD(this.OnReload);
            FreezeCheckConfig.Load();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GameInitialize.Deregister(this, new HookHandler<EventArgs>(OnInitialize));
                ServerApi.Hooks.NetGreetPlayer.Deregister(this, new HookHandler<GreetPlayerEventArgs>(OnNetGreetPlayer));
                ServerApi.Hooks.NetGetData.Deregister(this, new HookHandler<GetDataEventArgs>(OnNetGetData));
                ServerApi.Hooks.ServerLeave.Deregister(this, new HookHandler<LeaveEventArgs>(OnServerLeave));
            }
            base.Dispose(disposing);
        }

        private static void SendHelp(TSPlayer player)
        {
            string text = "Invalid syntax! Proper syntax: /unfreeze";
            if (player.Group.HasPermission(Permissions.cancheckothers))
            {
                text += " [player]";
            }
            if (player.Group.HasPermission(Permissions.canforce))
            {
                text += " [-force]";
            }
            player.SendErrorMessage(text);
        }

        private static string GetChecks(TSPlayer player, bool ignoreLogin = false)
        {
            string str = "";
            if (!player.IsLoggedIn && TShock.Config.RequireLogin && !ignoreLogin)
                str += "Login to move. ";
            if (TShock.Config.PvPMode == "always" && player.TPlayer.hostile == false)
                str += "Enable PvP to move. ";
            foreach (Item obj in player.TPlayer.inventory)
            {
                if (obj.type != null)
                {
                    if (!player.Group.HasPermission(TShockAPI.Permissions.ignorestackhackdetection) && obj.stack > obj.maxStack)
                        str = str + "Remove item " + (object)obj.name + " (" + obj.stack + ") exceeds max stack of " + obj.maxStack + ". ";
                    else if (!player.Group.HasPermission(TShockAPI.Permissions.usebanneditem) && TShock.Itembans.ItemIsBanned(obj.name, player))
                    {
                        if (obj.stack > 1 || obj.maxStack > 1)
                            str = str + "Remove item " + (object)obj.name + " (" + obj.stack + ") is banned. ";
                        else
                            str = str + "Remove item " + obj.name + " is banned. ";
                    }
                }
            }
            if (player.IgnoreActionsForInventory != "none" && !ignoreLogin)
                str = str + player.IgnoreActionsForInventory + ". ";
            foreach (Item obj in player.TPlayer.armor)
            {
                if (TShock.Itembans.ItemIsBanned(obj.name, player))
                    str = str + "Remove armor/accessory " + obj.name + ". ";
            }
            foreach (Item obj in player.TPlayer.bank.item)
            {
                if (!player.Group.HasPermission(TShockAPI.Permissions.usebanneditem) && obj.type != null && TShock.Itembans.ItemIsBanned(obj.name, player))
                {
                    if (obj.stack > 1 || obj.maxStack > 1)
                        str = str + (object)obj.name + " (" + obj.stack + ") in your piggy bank is banned. ";
                    else
                        str = str + obj.name + " in your piggy bank is banned. ";
                }
            }
            foreach (Item obj in player.TPlayer.bank2.item)
            {
                if (!player.Group.HasPermission(TShockAPI.Permissions.usebanneditem) && obj.type != null && TShock.Itembans.ItemIsBanned(obj.name, player))
                {
                    if (obj.stack > 1 || obj.maxStack > 1)
                        str = str + (object)obj.name + " (" + obj.stack + ") in your safe is banned. ";
                    else
                        str = str + obj.name + " in your safe is banned. ";
                }
            }
            if (string.IsNullOrWhiteSpace(str))
                return "You are clear to go!";
            if (ignoreLogin)
                TShock.Log.ConsoleInfo(player.Name + " is frozen for: " + str);
            else
                TShock.Log.Info(player.Name + " is frozen for: " + str);
            return str;
        }

        private static void CleanUp(TSPlayer player)
        {
            foreach (Item obj in player.TPlayer.armor)
            {
                if (TShock.Itembans.ItemIsBanned(obj.name, player))
                    obj.stack = 0;
                else if (!player.Group.HasPermission(TShockAPI.Permissions.ignorestackhackdetection) && obj.stack > obj.maxStack && obj.type != null)
                    obj.stack = obj.maxStack;
            }
            foreach (Item obj in player.TPlayer.inventory)
            {
                if (TShock.Itembans.ItemIsBanned(obj.name, player))
                    obj.stack = 0;
                else if (!player.Group.HasPermission(TShockAPI.Permissions.ignorestackhackdetection) && obj.stack > obj.maxStack && obj.type != null)
                    obj.stack = obj.maxStack;
            }
            foreach (Item obj in player.TPlayer.bank.item)
            {
                if (TShock.Itembans.ItemIsBanned(obj.name, player))
                    obj.stack = 0;
                else if (!player.Group.HasPermission(TShockAPI.Permissions.ignorestackhackdetection) && obj.stack > obj.maxStack && obj.type != null)
                    obj.stack = obj.maxStack;
            }
            foreach (Item obj in player.TPlayer.bank2.item)
            {
                if (TShock.Itembans.ItemIsBanned(obj.name, player))
                    obj.stack = 0;
                else if (!player.Group.HasPermission(TShockAPI.Permissions.ignorestackhackdetection) && obj.stack > obj.maxStack && obj.type != null)
                    obj.stack = obj.maxStack;
            }
        }

        private static void OnFreezeCheck(CommandArgs args)
        {
            if (args.Parameters.Count == 0)
            {
                if (!args.Player.RealPlayer)
                {
                    args.Player.SendErrorMessage("Impossible to check yourself. You don't exist!");
                    SendHelp(args.Player);
                    return;
                }
                args.Player.SendInfoMessage(GetChecks(args.Player, false));
                return;
            }
            else if (args.Parameters.Count == 1)
            {
                if (!args.Player.Group.HasPermission(Permissions.cancheckothers) && !args.Player.Group.HasPermission(Permissions.canforce))
                {
                    SendHelp(args.Player);
                    return;
                }
                if (args.Parameters[0] == "help")
                {
                    SendHelp(args.Player);
                    return;
                }
                List<TSPlayer> list = TShock.Utils.FindPlayer(args.Parameters[0]);
                if (list.Count == 0)
                {
                    if (args.Player.Group.HasPermission(Permissions.canforce) && args.Parameters[0] == "-force")
                    {
                        FreezeCheck.CleanUp(args.Player);
                        return;
                    }
                    args.Player.SendErrorMessage("Invalid player!");
                    return;
                }
                else
                {
                    if (list.Count > 1)
                    {
                        TShock.Utils.SendMultipleMatchError(args.Player, from p in list
                                                                         select p.Name);
                        return;
                    }
                    args.Player.SendInfoMessage(GetChecks(list[0], false));
                    return;
                }
            }
            else
            {
                if (args.Parameters.Count != 2)
                {
                    if (args.Parameters.Count > 2)
                    {
                        SendHelp(args.Player);
                    }
                    return;
                }
                if (!args.Player.Group.HasPermission(Permissions.cancheckothers) || !args.Player.Group.HasPermission(Permissions.canforce))
                {
                    SendHelp(args.Player);
                    return;
                }
                List<TSPlayer> list2 = TShock.Utils.FindPlayer(args.Parameters[0]);
                if (list2.Count == 0)
                {
                    args.Player.SendErrorMessage("Invalid player!");
                    return;
                }
                if (list2.Count > 1)
                {
                    TShock.Utils.SendMultipleMatchError(args.Player, from p in list2
                                                                     select p.Name);
                    return;
                }
                FreezeCheck.CleanUp(list2[0]);
                return;
            }
        }

        private void OnLogInv(CommandArgs args)
        {
            StringBuilder stringBuilder = new StringBuilder();
            TShock.Log.Info(args.Player.Name + " requested inventory dump.");
            stringBuilder.Clear().Append("Armor:");
            Item[] armor = args.Player.TPlayer.armor;
            for (int i = 0; i < armor.Length; i++)
            {
                Item item = armor[i];
                stringBuilder.Append(" ").Append(item.name).Append("(").Append(item.stack).Append(")");
            }
            TShock.Log.Info(stringBuilder.ToString());
            stringBuilder.Clear().Append("Inventory:");
            Item[] inventory = args.Player.TPlayer.inventory;
            for (int j = 0; j < inventory.Length; j++)
            {
                Item item2 = inventory[j];
                stringBuilder.Append(" ").Append(item2.name).Append("(").Append(item2.stack).Append(")");
            }
            TShock.Log.Info(stringBuilder.ToString());
            stringBuilder.Clear().Append("Piggy bank:");
            Item[] item3 = args.Player.TPlayer.bank.item;
            for (int k = 0; k < item3.Length; k++)
            {
                Item item4 = item3[k];
                stringBuilder.Append(" ").Append(item4.name).Append("(").Append(item4.stack).Append(")");
            }
            TShock.Log.Info(stringBuilder.ToString());
            stringBuilder.Clear().Append("Safe:");
            Item[] item5 = args.Player.TPlayer.bank2.item;
            for (int l = 0; l < item5.Length; l++)
            {
                Item item6 = item5[l];
                stringBuilder.Append(" ").Append(item6.name).Append("(").Append(item6.stack).Append(")");
            }
            TShock.Log.Info(stringBuilder.ToString());
        }

        private void OnNetGreetPlayer(GreetPlayerEventArgs args)
        {
            if (TShock.Players[args.Who] == null)
            {
                TShock.Log.ConsoleInfo("[FreezeCheck]Check failed.");
                return;
            }
            GetChecks(TShock.Players[args.Who], true);
        }

        private void OnPlayerUpdate(object sender, GetDataHandlers.PlayerUpdateEventArgs args)
        {
            if (args.Position.X < 0f || args.Position.Y < 0f || args.Position.X >= Main.maxTilesX * 16 - 16 || args.Position.Y >= Main.maxTilesY * 16 - 16)
            {
                return;
            }
            if (args.Item < 0 || args.Item >= TShock.Players[args.PlayerId].TPlayer.inventory.Length)
            {
                return;
            }
            if (TShock.Players[args.PlayerId].LastNetPosition == Vector2.Zero)
            {
                return;
            }
            if (!args.Position.Equals(TShock.Players[args.PlayerId].LastNetPosition))
            {
                return;
            }
            if ((args.Control & 32) == 32 && TShock.Itembans.ItemIsBanned(TShock.Players[args.PlayerId].TPlayer.inventory[args.Item].name, TShock.Players[args.PlayerId]) && !string.IsNullOrWhiteSpace(Config.BannedGroup))
            {
                if (TShock.Players[args.PlayerId].Group.Name != Config.BannedGroup)
                {
                    TShock.Players[args.PlayerId].tempGroup = TShock.Utils.GetGroup(Config.BannedGroup);
                    TShock.Log.ConsoleInfo(string.Format("[FreezeCheck]Player {0} has been moved to group {1}.", TShock.Players[args.PlayerId].Name, TShock.Players[args.PlayerId].Group.Name));
                }
                if (BannedTimers[args.PlayerId] != null)
                {
                    BannedTimers[args.PlayerId].Stop();
                    BannedTimers[args.PlayerId].Close();
                    BannedTimers[args.PlayerId] = null;
                }
                BannedTimers[args.PlayerId] = new Timer(Config.BannedTime * 1000);
                BannedTimers[args.PlayerId].AutoReset = false;
                BannedTimers[args.PlayerId].Elapsed += delegate
            {
                OnBannedTimerElapsed(args.PlayerId);
            };
                BannedTimers[args.PlayerId].Start();
            }
        }

        private void OnNetGetData(GetDataEventArgs e)
        {
            PacketTypes msgId = e.MsgID;
            TSPlayer tsPlayer = TShock.Players[e.Msg.whoAmI];
            if (tsPlayer == null || !tsPlayer.ConnectionAlive || tsPlayer.RequiresPassword && msgId != 38 || (tsPlayer.State < 10 || tsPlayer.Dead) && (msgId > 12 && msgId != 16) && (msgId != 42 && msgId != 50 && (msgId != 38 && msgId != 21)) || msgId != 13)
                return;
            using (MemoryStream memoryStream = new MemoryStream(e.Msg.readBuffer, e.Index, e.Length))
            {
                GetDataHandlers.PlayerUpdateEventArgs args = new GetDataHandlers.PlayerUpdateEventArgs();
                args.PlayerId = StreamExt.ReadInt8(memoryStream);
                args.Control = StreamExt.ReadInt8(memoryStream);
                args.Item = StreamExt.ReadInt8(memoryStream);
                args.set_Position(new Vector2(StreamExt.ReadSingle(memoryStream), StreamExt.ReadSingle(memoryStream)));
                args.set_Velocity(new Vector2(StreamExt.ReadSingle(memoryStream), StreamExt.ReadSingle(memoryStream)));
                args.Pulley = StreamExt.ReadInt8(memoryStream);
                this.OnPlayerUpdate(null, args);
            }
        }

        private void OnServerLeave(LeaveEventArgs args)
        {
            if (BannedTimers[args.Who] != null)
            {
                BannedTimers[args.Who].Stop();
                BannedTimers[args.Who].Close();
                BannedTimers[args.Who] = null;
            }
        }

        private void OnBannedTimerElapsed(int Who)
        {
            if (BannedTimers[Who] != null)
            {
                if (TShock.Players[Who] != null)
                {
                    TShock.Players[Who].tempGroup = null;
                    TShock.Players[Who].SendSuccessMessage("Your actions are being accepted now.");
                    TShock.Log.ConsoleInfo(string.Format("[FreezeCheck]Player {0} has had his group restored.", TShock.Players[Who].Name));
                }
                BannedTimers[Who].Stop();
                BannedTimers[Who].Close();
                BannedTimers[Who] = null;
            }
        }

        private void OnReload(ReloadEventArgs args)
        {
            FreezeCheckConfig.Reload(args);
        }
    }
}
