﻿using System;
using System.Collections.Generic;
using System.IO.Streams;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Protocol;
using Rests;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.DB;
using TShockAPI.Hooks;

namespace CaiBotPlugin
{
    public static class Login
    {
        public async static void OnGetData(GetDataEventArgs args)
        {
            if (!Config.config.WhiteList)
                return;
            
            var type = args.MsgID;

            var player = TShock.Players[args.Msg.whoAmI];
            if (player == null || !player.ConnectionAlive)
            {
                args.Handled = true;
                return;
            }

            if (player.RequiresPassword && type != PacketTypes.PasswordSend)
            {
                args.Handled = true;
                return;
            }

            if ((player.State < 10 || player.Dead) && (int)type > 12 && (int)type != 16 && (int)type != 42 && (int)type != 50 &&
                (int)type != 38 && (int)type != 21 && (int)type != 22)
            {
                args.Handled = true;
                return;
            }
            try
            {
                using (var data = new MemoryStream(args.Msg.readBuffer, args.Index, args.Length - 1))
                {
                    // ReSharper disable once ConvertIfStatementToSwitchStatement
                    if (type == PacketTypes.ContinueConnecting2)
                    {
                        var account = TShock.UserAccounts.GetUserAccountByName(player.Name);
                        player.DataWhenJoined = new PlayerData(player);
                        player.DataWhenJoined.CopyCharacter(player);
                        //NetMessage.TrySendData(68, -1, -1, null, args.Index);
                        args.Handled = true;
                    }
                    else if (type == PacketTypes.PlayerInfo)
                    {
                        if (player.IsLoggedIn)
                        {
                            return;
                        }
                        data.ReadByte();
                        data.ReadByte();
                        data.ReadByte();
                        var name = data.ReadString().Trim().Trim();
                        var re = new RestObject
                        {
                            { "type","whitelist" },
                            { "name",name }
                        };
                        if (!MessageHandle.isWebsocketConnected)
                        {
                            TShock.Log.ConsoleError("[CaiBot]机器人处于未连接状态, 玩家无法加入。\n" +
                                "如果你不想使用Cai白名单，可以在tshock/CaiBot.json中将其关闭。");
                            return;
                        }
                        await MessageHandle.SendDateAsync(re.ToJson());
                        //CheckWhite(name, player);
                        //{
                        //    HandleLogin(player, Guid.NewGuid().ToString());
                        //}
                    }
                }

            }
            catch (Exception e)
            {
                TShock.Log.ConsoleError(e.ToString());
            }
        }
        public static async Task<bool> CheckWhiteAsync(string name, int code,List<string> uuids)
        {
            var playerList = TSPlayer.FindByNameOrID("tsn:" + name);
            if (playerList.Count == 0)
            {
                return false;
            }
            TSPlayer plr = playerList[0];
            //NetMessage.SendData(9, args.Who, -1, Terraria.Localization.NetworkText.FromLiteral($"[白名单]正在校验白名单..."), 1);
            if (string.IsNullOrEmpty(name))
            {
                TShock.Log.Info($"[白名单]玩家[{name}](IP: {plr.IP};UUID: {plr.UUID})版本可能过低...");
                Console.WriteLine($"[白名单]玩家[{name}](IP: {plr.IP})版本可能过低...");
                plr.Disconnect("你的游戏版本可能过低,请使用Terraria1.4.4+游玩");
                return false;
            }
            try
            {
                switch (code)
                {
                    case 200:
                        {
                            TShock.Log.Info($"[白名单]玩家[{name}](IP: {plr.IP};UUID: {plr.UUID})已通过白名单验证...");
                            Console.WriteLine($"[白名单]玩家[{name}](IP: {plr.IP})已通过白名单验证...");
                            //NetMessage.SendData(9, args.Who, -1, Terraria.Localization.NetworkText.FromLiteral($"[白名单]白名单校验成功!\n"), 1);
                            break;
                        }
                    case 404:
                        {
                            TShock.Log.Info($"[白名单]玩家[{name}](IP: {plr.IP};UUID: {plr.UUID})没有添加白名单...");
                            Console.WriteLine($"[白名单]玩家[{name}](IP: {plr.IP})没有添加白名单...");
                            plr.SilentKickInProgress = true;
                            plr.Disconnect("没有添加白名单\n请在群内发送'添加白名单'");
                            return false;
                        }
                    case 403:
                        {
                            TShock.Log.Info($"[白名单]玩家[{name}](IP: {plr.IP};UUID: {plr.UUID})白名单被冻结...");
                            Console.WriteLine($"[白名单]玩家[{name}](IP: {plr.IP})白名单被冻结...");
                            plr.SilentKickInProgress = true;
                            plr.Disconnect("[白名单]你已被服务器屏蔽\n你在云黑名单内!");
                            return false;
                        }
                    case 401:
                        {
                            TShock.Log.Info($"[白名单]玩家[{name}](IP: {plr.IP};UUID: {plr.UUID})不在本群内...");
                            Console.WriteLine($"[白名单]玩家[{name}](IP: {plr.IP})不在本群内...");
                            plr.SilentKickInProgress = true;
                            plr.Disconnect("[白名单]不在本服务器群内!\n请加入服务器群");
                            return false;
                        }
                    default:
                        {
                            TShock.Log.Info($"[白名单]玩家[{name}](IP: {plr.IP};UUID: {plr.UUID})触发了百年一遇的白名单bug...");
                            Console.WriteLine($"[白名单]玩家[{name}](IP: {plr.IP})触发了百年一遇的白名单bug...");
                            plr.SilentKickInProgress = true;
                            plr.Disconnect("[白名单]这个情况可能只是一个摆设\n但是你触发了它？");
                            return false;
                        }
                }
                if (!uuids.Contains(plr.UUID))
                {
                    if (string.IsNullOrEmpty(plr.UUID))
                            
                    {
                        plr.SilentKickInProgress = true;
                        plr.Disconnect("[白名单]UUID为空\n请尝试重新加入游戏或者联系服务器管理员");
                        return false;
                    }
                    TShock.Log.Info($"[白名单]玩家[{name}](IP: {plr.IP};UUID: {plr.UUID})没有批准登录...");
                    Console.WriteLine($"[白名单]玩家[{name}](IP: {plr.IP})没有批准登录...");
                    plr.SilentKickInProgress = true;
                    plr.Disconnect("[白名单]在群内发送'登录'，以批准此设备登录");

                    var re = new RestObject
                        {
                            { "type","device" },
                            { "uuid", plr.UUID },
                            { "ip", plr.IP },
                            { "name", name }
                        };
                    await MessageHandle.SendDateAsync(re.ToJson());

                    return false;
                }

            }
            catch (Exception ex)
            {
                TShock.Log.Info($"[白名单]玩家[{name}](IP: {plr.IP};UUID: {plr.UUID})验证白名单时出现错误...");
                Console.WriteLine($"[白名单]玩家[{name}](IP: {plr.IP})验证白名单时出现错误...");
                TShock.Log.Info("[XSB适配插件]:\n" + ex);
                Console.WriteLine("[XSB适配插件]:\n" + ex);
                plr.SilentKickInProgress = true;
                plr.Disconnect("[白名单]服务器发生错误无法处理该请求!请尝试重新加入游戏或者联系服务器管理员");
                return false;
            }
            

           
            return true;
            //NetMessage.SendData(9, plr.Index, -1, NetworkText.FromLiteral("正在检查白名单..."), 1);

        }
        public static bool HandleLogin(TSPlayer player, string password)
        {

            var account = TShock.UserAccounts.GetUserAccountByName(player.Name);
            if (account != null)
            {
                player.RequiresPassword = false;
                player.PlayerData = TShock.CharacterDB.GetPlayerData(player, account.ID);

                if (player.State == 1)
                    player.State = 2;
                NetMessage.SendData((int)PacketTypes.WorldInfo, player.Index);

                var group = TShock.Groups.GetGroupByName(account.Group);

                player.Group = group;
                player.tempGroup = null;
                player.Account = account;
                player.IsLoggedIn = true;
                player.IsDisabledForSSC = false;

                if (Main.ServerSideCharacter)
                {
                    if (player.HasPermission(Permissions.bypassssc))
                    {
                        player.PlayerData.CopyCharacter(player);
                        TShock.CharacterDB.InsertPlayerData(player);
                    }
                    player.PlayerData.RestoreCharacter(player);
                }
                player.LoginFailsBySsi = false;

                if (player.HasPermission(Permissions.ignorestackhackdetection))
                    player.IsDisabledForStackDetection = false;

                if (player.HasPermission(Permissions.usebanneditem))
                    player.IsDisabledForBannedWearable = false;


                player.SendSuccessMessage($"[账号管理]已经验证{account.Name}登录完毕。");
                TShock.Log.ConsoleInfo(player.Name + "成功验证登录。");
                TShock.UserAccounts.SetUserAccountUUID(account, player.UUID);
                PlayerHooks.OnPlayerPostLogin(player);
                return true;
            }
            if (player.Name != TSServerPlayer.AccountName)
            {
                account = new UserAccount
                {
                    Name = player.Name,
                    Group = TShock.Config.Settings.DefaultRegistrationGroupName,
                    UUID = player.UUID
                };
                try
                {
                    account.CreateBCryptHash(password);
                }
                catch (ArgumentOutOfRangeException)
                {
                    //Kick(player, "注册失败!\n密码位数不能少于" + TShock.Config.Settings.MinimumPasswordLength + "个字符.");
                    return true;
                }
                player.SendSuccessMessage("[i:50]账户{0}注册成功。", account.Name);
                TShock.UserAccounts.AddUserAccount(account);
                TShock.Log.ConsoleInfo("玩家{0}注册了新账户：{1}", player.Name, account.Name);

                //player.RequiresPassword = false;
                //player.SetData(WaitPwd4Reg, false);
                player.PlayerData = TShock.CharacterDB.GetPlayerData(player, account.ID);

                if (player.State == 1)
                    player.State = 2;
                NetMessage.SendData((int)PacketTypes.WorldInfo, player.Index);

                var group = TShock.Groups.GetGroupByName(account.Group);

                player.Group = group;
                player.tempGroup = null;
                player.Account = account;
                player.IsLoggedIn = true;
                player.IsDisabledForSSC = false;

                if (Main.ServerSideCharacter)
                {
                    if (player.HasPermission(Permissions.bypassssc))
                    {
                        player.PlayerData.CopyCharacter(player);
                        TShock.CharacterDB.InsertPlayerData(player);
                    }
                    player.PlayerData.RestoreCharacter(player);
                }
                player.LoginFailsBySsi = false;

                if (player.HasPermission(Permissions.ignorestackhackdetection))
                    player.IsDisabledForStackDetection = false;

                if (player.HasPermission(Permissions.usebanneditem))
                    player.IsDisabledForBannedWearable = false;


                player.SendSuccessMessage($"[账号管理]已经验证{account.Name}登录完毕.");
                TShock.Log.ConsoleInfo(player.Name + "成功验证登录.");
                TShock.UserAccounts.SetUserAccountUUID(account, player.UUID);
                PlayerHooks.OnPlayerPostLogin(player);
                return true;
            }

            player.SilentKickInProgress = true;
            player.Disconnect($"[账号管理]该用户名已被占用.\n请更换人物名");
            return true;
        }
    }
}
