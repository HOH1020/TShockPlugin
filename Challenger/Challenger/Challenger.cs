﻿using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System.Text;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.Localization;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;
using static OTAPI.Hooks.NPC;
using static TShockAPI.GetDataHandlers;
using static TShockAPI.Hooks.GeneralHooks;
using OTAPI;
using System.IO;
using ChalleAnger;
using System.Configuration;
using Google.Protobuf.WellKnownTypes;

namespace Challenger
{
    [ApiVersion(2, 1)]
    public class Challenger : TerrariaPlugin
    {
        public string configPath = Path.Combine(TShock.SavePath, "ChallengerConfig.json");

        internal static Config config = new Config();

        public static long Timer = 0L;

        public static Dictionary<int, int> honey = new Dictionary<int, int>();

        public override string Author => "z枳 & 星夜神花 修改：羽学";

        public override string Description => "增强游戏难度，更好的游戏体验";

        public override string Name => "Challenger";

        public override Version Version => new Version(1, 0, 0, 2);

        public Challenger(Main game)
            : base(game)
        {
        }

        public override void Initialize()
        {
            LoadConfig();
            GeneralHooks.ReloadEvent += new GeneralHooks.ReloadEventD(LoadConfig);
            ServerApi.Hooks.GameUpdate.Register((TerrariaPlugin)(object)this, (HookHandler<EventArgs>)OnGameUpdate);
            GetDataHandlers.PlayerDamage.Register((EventHandler<PlayerDamageEventArgs>)PlayerSufferDamage, (HandlerPriority)3, false);
            GetDataHandlers.NewProjectile.Register((EventHandler<NewProjectileEventArgs>)OnProjSpawn, (HandlerPriority)3, false);
            ServerApi.Hooks.ProjectileAIUpdate.Register((TerrariaPlugin)(object)this, (HookHandler<ProjectileAiUpdateEventArgs>)OnProjAIUpdate);
            GetDataHandlers.ProjectileKill.Register((EventHandler<ProjectileKillEventArgs>)OnProjKilled, (HandlerPriority)3, false);
            ServerApi.Hooks.NpcAIUpdate.Register((TerrariaPlugin)(object)this, (HookHandler<NpcAiUpdateEventArgs>)OnNPCAI);
            Hooks.NPC.Killed += OnNPCKilled;
            ServerApi.Hooks.NpcStrike.Register((TerrariaPlugin)(object)this, (HookHandler<NpcStrikeEventArgs>)OnNpcStrike);
            GetDataHandlers.PlayerSlot.Register((EventHandler<PlayerSlotEventArgs>)OnHoldItem, (HandlerPriority)3, false);
            ServerApi.Hooks.NetGreetPlayer.Register((TerrariaPlugin)(object)this, (HookHandler<GreetPlayerEventArgs>)OnGreetPlayer);
            ServerApi.Hooks.ServerLeave.Register((TerrariaPlugin)(object)this, (HookHandler<LeaveEventArgs>)OnServerLeave);
            Commands.ChatCommands.Add(new Command("challenger.enable", new CommandDelegate(EnableModel), new string[1] { "cenable" })
            {
                HelpText = "输入 /cenable  来启用挑战模式，再次使用取消"
            });
            Commands.ChatCommands.Add(new Command("challenger.tip", new CommandDelegate(EnableTips), new string[1] { "ctip" })
            {
                HelpText = "输入 /ctip  来启用内容提示，如各种物品的强化文字提示，再次使用取消"
            });
            Commands.ChatCommands.Add(new Command("challenger.fun", new CommandDelegate(Function), new string[1] { "cf" })
            {
                HelpText = "输入 /cf  来实现某些技能的或状态的切换"
            });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                GeneralHooks.ReloadEvent -= new ReloadEventD(LoadConfig);
                ServerApi.Hooks.GameUpdate.Deregister((TerrariaPlugin)(object)this, (HookHandler<EventArgs>)OnGameUpdate);
                GetDataHandlers.PlayerDamage.UnRegister((EventHandler<PlayerDamageEventArgs>)PlayerSufferDamage);
                ServerApi.Hooks.ProjectileAIUpdate.Deregister((TerrariaPlugin)(object)this, (HookHandler<ProjectileAiUpdateEventArgs>)OnProjAIUpdate);
                GetDataHandlers.ProjectileKill.UnRegister((EventHandler<ProjectileKillEventArgs>)OnProjKilled);
                ServerApi.Hooks.NpcAIUpdate.Deregister((TerrariaPlugin)(object)this, (HookHandler<NpcAiUpdateEventArgs>)OnNPCAI);
                Hooks.NPC.Killed -= OnNPCKilled;
                ServerApi.Hooks.NpcStrike.Deregister((TerrariaPlugin)(object)this, (HookHandler<NpcStrikeEventArgs>)OnNpcStrike);
                GetDataHandlers.PlayerSlot.UnRegister((EventHandler<PlayerSlotEventArgs>)OnHoldItem);
                ServerApi.Hooks.NetGreetPlayer.Deregister((TerrariaPlugin)(object)this, (HookHandler<GreetPlayerEventArgs>)OnGreetPlayer);
                ServerApi.Hooks.ServerLeave.Deregister((TerrariaPlugin)(object)this, (HookHandler<LeaveEventArgs>)OnServerLeave);
            }
            base.Dispose(disposing);
        }

        #region 配置文件创建与重读加载方法
        private static void LoadConfig(ReloadEventArgs args = null!)
        {
            //调用Configuration.cs文件Read和Write方法
            config = Config.Read(Config.FilePath);
            config.Write(Config.FilePath);
            if (args != null && args.Player != null)
            {
                args.Player.SendSuccessMessage("[挑战者模式]重新加载配置完毕。");
            }
        }
        #endregion



        public void TouchedAndBeSucked(PlayerDamageEventArgs e)
        {
            NPC val = Main.npc[e.PlayerDeathReason._sourceNPCIndex];
            int num = ((e.Damage > 1500) ? 1500 : e.Damage);
            NPC val2 = NearestWeakestNPC(val.Center, 4000000f);
            int num2 = e.PlayerDeathReason._sourceNPCIndex;
            float num3;
            if ((val2 != null && !val2.boss) || val2 == null)
            {
                num3 = ((float)num * 0.8f + (float)val.lifeMax * 0.05f) * config.BloodAbsorptionRatio;
            }
            else
            {
                num3 = ((float)num * 0.5f + (float)val.lifeMax * 0.008f) * config.BloodAbsorptionRatioForBoss;
                num2 = ((Entity)val2).whoAmI;
            }
            Vector2 center = ((Entity)Main.player[((GetDataHandledEventArgs)e).Player.Index]).Center;
            float num4 = ((num - Main.player[((GetDataHandledEventArgs)e).Player.Index].statDefense >= 0) ? (num - Main.player[((GetDataHandledEventArgs)e).Player.Index].statDefense) : 0);
            num4 = (1f - Main.player[((GetDataHandledEventArgs)e).Player.Index].endurance) * num4 * 0.6f;
            if (num4 > 400f)
            {
                num4 = 400f;
            }
            else if (num4 < 0f)
            {
                num4 = 0f;
            }
            BloodBagProj.NewCProjectile(center, Vector2.Zero, ((GetDataHandledEventArgs)e).Player.Index, 0, new float[8] { num4, 0f, 0f, num2, num3, 0f, 0f, 0f });
        }

        public void ProjAndBeSucked(PlayerDamageEventArgs e)
        {
            Vector2 center = ((Entity)Main.player[((GetDataHandledEventArgs)e).Player.Index]).Center;
            NPC val = NearestWeakestNPC(center, 9000000f);
            int num = ((e.Damage > 1500) ? 1500 : e.Damage);
            float num2 = ((val == null || !val.boss) ? ((float)num * config.BloodAbsorptionRatio) : (((float)num + (float)val.lifeMax * 0.008f) * config.BloodAbsorptionRatioForBoss));
            float num3 = ((num - Main.player[((GetDataHandledEventArgs)e).Player.Index].statDefense >= 0) ? (num - Main.player[((GetDataHandledEventArgs)e).Player.Index].statDefense) : 0);
            num3 = (1f - Main.player[((GetDataHandledEventArgs)e).Player.Index].endurance) * num3 * 0.6f;
            if (num3 > 400f)
            {
                num3 = 400f;
            }
            else if (num3 < 0f)
            {
                num3 = 0f;
            }
            BloodBagProj.NewCProjectile(center, Vector2.Zero, ((GetDataHandledEventArgs)e).Player.Index, 0, new float[8] { num3, 0f, 0f, -1f, num2, 0f, 0f, 0f });
        }

        public void AnglerArmorEffect(Player player)
        {
            Item[] armor = player.armor;
            if (armor[0].type == 2367 && armor[1].type == 2368 && armor[2].type == 2369 && Timer % 120 == 0)
            {
                TShock.Players[((Entity)player).whoAmI].SetBuff(106, 180, false);
                TShock.Players[((Entity)player).whoAmI].SetBuff(123, 180, false);
                TShock.Players[((Entity)player).whoAmI].SetBuff(121, 180, false);
                TShock.Players[((Entity)player).whoAmI].SetBuff(122, 180, false);
            }
        }

        public void NinjaArmorEffect(PlayerDamageEventArgs e)
        {
            if (config.NinjaArmorEffect)
            {
                Item[] armor = Main.player[((GetDataHandledEventArgs)e).Player.Index].armor;
                if (armor[0].type == 256 && armor[1].type == 257 && armor[2].type == 258 && Main.rand.Next(4) == 0)
                {
                    int index = Collect.MyNewProjectile(null, ((Entity)Main.player[((GetDataHandledEventArgs)e).Player.Index]).Center, -Vector2.UnitY, 196, 0, 0f, ((GetDataHandledEventArgs)e).Player.Index);
                    CProjectile.Update(index);
                    NetMessage.SendData(62, -1, -1, (NetworkText)null, ((GetDataHandledEventArgs)e).Player.Index, 1f, 0f, 0f, 0, 0, 0);
                    NetMessage.SendData(16, -1, -1, NetworkText.Empty, ((GetDataHandledEventArgs)e).Player.Index, 0f, 0f, 0f, 0, 0, 0);
                    SendPlayerText(e.Damage, Color.Green, ((Entity)Main.player[((GetDataHandledEventArgs)e).Player.Index]).Center);
                    if (config.EnableConsumptionMode)
                    {
                        SendPlayerText("闪避锁血成功！", Color.White, ((Entity)Main.player[((GetDataHandledEventArgs)e).Player.Index]).Center + new Vector2((float)Main.rand.Next(-60, 61), (float)Main.rand.Next(61)));
                    }
                }
            }
        }

        public void FossilArmorEffect(Player player)
        {
            if (config.FossilArmorEffect)
            {

                Item[] armor = player.armor;
                bool flag = armor[0].type == 3374 && armor[1].type == 3375 && armor[2].type == 3376;
                if (flag && !Collect.cplayers[((Entity)player).whoAmI].FossilArmorEffectProj)
                {
                    FossiArmorProj fossiArmorProj = FossiArmorProj.NewCProjectile(((Entity)player).Center, Vector2.Zero, ((Entity)player).whoAmI, new float[0], 0);
                    Collect.cplayers[((Entity)player).whoAmI].FossilArmorEffectProjIndex = ((Entity)fossiArmorProj.proj).whoAmI;
                    Collect.cplayers[((Entity)player).whoAmI].FossilArmorEffectProj = true;
                }
                else if (flag && Collect.cplayers[((Entity)player).whoAmI].FossilArmorEffectProj && !Collect.cprojs[Collect.cplayers[((Entity)player).whoAmI].FossilArmorEffectProjIndex].isActive)
                {
                    FossiArmorProj fossiArmorProj2 = FossiArmorProj.NewCProjectile(player.Center, Vector2.Zero, player.whoAmI, new float[0], 0);
                    Collect.cplayers[((Entity)player).whoAmI].FossilArmorEffectProjIndex = ((Entity)fossiArmorProj2.proj).whoAmI;
                }
                else if (!flag && Collect.cplayers[((Entity)player).whoAmI].FossilArmorEffectProj)
                {
                    CProjectile.CKill(Collect.cplayers[((Entity)player).whoAmI].FossilArmorEffectProjIndex);
                    Collect.cplayers[((Entity)player).whoAmI].FossilArmorEffectProj = false;
                }
            }
        }

        public void CrimsonArmorEffect(NpcStrikeEventArgs args)
        {
            CPlayer cPlayer = Collect.cplayers[((Entity)args.Player).whoAmI];
            if (args.Player.armor[0].type != 792 || args.Player.armor[1].type != 793 || args.Player.armor[2].type != 794 || !args.Critical || Timer - cPlayer.CrimsonArmorEffectTimer < 300 || !args.Npc.CanBeChasedBy((object)null, false))
            {
                return;
            }
            NPC[] array = NearAllHostileNPCs(((Entity)args.Player).Center, 102400f);
            if (!array.Any())
            {
                return;
            }
            int num = 0;
            NPC[] array2 = array;
            foreach (NPC val in array2)
            {
                int num2 = ((10 - num > 0) ? (10 - num) : 0);
                num++;
                if (num2 == 0)
                {
                    break;
                }
                Projectile.NewProjectile((IEntitySource)null, ((Entity)val).Center, Vector2.Zero, 305, 0, 0f, -1, (float)((Entity)args.Player).whoAmI, (float)num2, 0f);
            }
            cPlayer.CrimsonArmorEffectTimer = (int)Timer;
        }

        public void ShadowArmorEffect(NpcStrikeEventArgs args)
        {
            Item[] armor = args.Player.armor;
            if ((armor[0].netID == 102 || armor[0].netID == 956) && (armor[1].netID == 101 || armor[1].netID == 957) && (armor[2].netID == 100 || armor[2].netID == 958) && args.Critical && Timer - Collect.cplayers[((Entity)args.Player).whoAmI].ShadowArmorEffectTimer >= 60)
            {
                int num = Main.rand.Next(2, 6);
                for (int i = 0; i < num; i++)
                {
                    int num2 = Collect.MyNewProjectile(null, ((Entity)args.Player).Center, new Vector2((float)Math.Cos(Main.rand.NextDouble() * 6.2831854820251465), (float)Math.Sin(Main.rand.NextDouble() * 6.2831854820251465)), 307, 20, 2f, ((Entity)args.Player).whoAmI);
                    Projectile obj = Main.projectile[num2];
                    obj.scale *= 0.5f;
                    CProjectile.Update(num2);
                }
                Collect.cplayers[((Entity)args.Player).whoAmI].ShadowArmorEffectTimer = (int)Timer;
            }
        }

        public void MeteorArmorEffect(NpcStrikeEventArgs? args, Player? player)
        {
            if (args != null)
            {
                Item[] armor = args.Player.armor;
                if (armor[0].netID == 123 && armor[1].netID == 124 && armor[2].netID == 125 && args.Critical && args.Npc.CanBeChasedBy((object)null, false))
                {
                    Player player2 = args.Player;
                    player2.statMana += 3;
                    if (config.EnableConsumptionMode)
                    {
                        HealPlayerMana(args.Player, 5, visible: false);
                        SendPlayerText(TShock.Players[((Entity)args.Player).whoAmI], "陨石回魔 + 3", new Color(6, 0, 255), ((Entity)args.Player).Center);
                    }
                    else
                    {
                        HealPlayerMana(args.Player, 5);
                    }
                }
            }
            else if (player != null)
            {
                if (config.MeteorArmorEffect)
                {
                    Item[] armor2 = player.armor;
                    if (armor2[0].netID == 123 && armor2[1].netID == 124 && armor2[2].netID == 125 && Timer % 120 == 0)
                    {
                        int index = Collect.MyNewProjectile(null, ((Entity)player).Center + new Vector2((float)Main.rand.Next(-860, 861), -600f), Terraria.Utils.RotateRandom(Vector2.UnitY, 0.3) * 16f, 725, 1000, 0f, ((Entity)player).whoAmI);
                        CProjectile.Update(index);
                    }
                }
            }
        }

        public void JungleArmorEffect(Player player)
        {
            if (config.JungleArmorEffect)
            {
                Item[] armor = player.armor;
                if ((armor[0].type == 228 || armor[0].type == 960) && (armor[1].type == 229 || armor[1].type == 961) && (armor[2].type == 230 || armor[2].type == 962) && Main.rand.Next(15) == 0)
                {
                    int index = Collect.MyNewProjectile(player.GetProjectileSource_Accessory(armor[0]), ((Entity)player).Center, Terraria.Utils.RotatedByRandom(Vector2.One, 6.2831854820251465) * 1.5f, Main.rand.Next(569, 572), 15, 8f, ((Entity)player).whoAmI);
                    CProjectile.Update(index);
                }
            }
        }

        public void BeeArmorEffect(Player player)
        {
            if (config.BeeArmorEffect)
            {
                Item[] armor = player.armor;
                if (armor[0].type == 2361 && armor[1].type == 2362 && armor[2].type == 2363 && Timer % 120 == 0)
                {
                    TShock.Players[((Entity)player).whoAmI].SetBuff(48, 150, false);
                }
                if (armor[0].type == 2361 && armor[1].type == 2362 && armor[2].type == 2363 && Main.rand.Next(70) == 0)
                {
                    Honey.NewCProjectile(((Entity)player).Center, -Terraria.Utils.RotatedByRandom(Vector2.UnitY, 0.7853981852531433) * 5.5f, 1, ((Entity)player).whoAmI, new float[1]);
                }
            }
        }

        public void NecroArmor(GetDataHandlers.PlayerDamageEventArgs? e, NpcStrikeEventArgs? args)
        {
            if (config.NecroArmor)
            {
                if (e != null)
                {
                    Item[] armor = Main.player[((GetDataHandledEventArgs)e).Player.Index].armor;
                    if ((armor[0].type == 151 || armor[0].type == 959) && armor[1].type == 152 && armor[2].type == 153)
                    {
                        for (int i = 0; i < 8; i++)
                        {
                            Vector2 velocity = Terraria.Utils.RotatedBy(Vector2.UnitY, (double)((float)Math.PI / 4f * (float)i + (float)Math.PI / 8f), default(Vector2)) * 4f;
                            int index = Collect.MyNewProjectile(null, ((Entity)Main.player[((GetDataHandledEventArgs)e).Player.Index]).Center, velocity, 532, 20, 5f, ((GetDataHandledEventArgs)e).Player.Index);
                            CProjectile.Update(index);
                        }
                    }
                }
                else if (args != null)
                {
                    Item[] armor2 = args.Player.armor;
                    if ((armor2[0].type == 151 || armor2[0].type == 959) && armor2[1].type == 152 && armor2[2].type == 153 && Main.rand.Next(3) == 0)
                    {
                        Vector2 val = ((Entity)args.Player).Center + Terraria.Utils.RotatedByRandom(Vector2.One, 3.1415927410125732) * 0.1f * (float)Main.rand.Next(0, 500);
                        int index2 = Collect.MyNewProjectile(null, val, (((Entity)args.Npc).Center + new Vector2(0f, -10f) - val) * 0.02f, 117, 20, 2f, ((Entity)args.Player).whoAmI);
                        CProjectile.Update(index2);
                    }
                }
            }
        }

        public void ObsidianArmorEffect(NpcStrikeEventArgs args)
        {
            if (config.ObsidianArmorEffect)
            {
                Item[] armor = args.Player.armor;
                if (armor[0].type != 3266 || armor[1].type != 3267 || armor[2].type != 3268 || !args.Npc.CanBeChasedBy((object)null, false) || args.Npc.SpawnedFromStatue)
                {
                    return;
                }
                try
                {
                    if (!args.Npc.boss && args.Npc.rarity <= 1 && args.Npc.lifeMax <= 7000)
                    {
                        Collect.cnpcs[((Entity)args.Npc).whoAmI].AccOfObsidian.Add(args.Player.name);
                    }
                }
                catch
                {
                }
            }
        }

        public void MoltenArmor(Player player)
        {
            var any = config.MoltenArmor;

            Item[] armor = player.armor;
            if (armor[0].type == 231 && armor[1].type == 232 && armor[2].type == 233 && Timer % 120 == 0)
            {
                TShock.Players[((Entity)player).whoAmI].SetBuff(1, 180, false);
                TShock.Players[((Entity)player).whoAmI].SetBuff(any, 180, false);
            }
        }

        public void SpiderArmorEffect(NpcStrikeEventArgs? args, Player? player)
        {
            if (args != null)
            {
                Item[] armor = args.Player.armor;
                if (armor[0].type == 2370 && armor[1].type == 2371 && armor[2].type == 2372)
                {
                    args.Npc.AddBuff(70, 180, false);
                    args.Npc.AddBuff(20, 360, false);
                }
                return;
            }
            Item[] armor2 = player.armor;
            if (armor2[0].type == 2370 && armor2[1].type == 2371 && armor2[2].type == 2372 && Timer - Collect.cplayers[((Entity)player).whoAmI].SpiderArmorEffectTimer >= 60 && player.controlUp)
            {
                NPC val = NearestHostileNPC(((Entity)player).Center, 360000f);
                if (val != null)
                {
                    SpiderArmorProj.NewCProjectile(((Entity)player).Center, Terraria.Utils.SafeNormalize(((Entity)val).Center - ((Entity)player).Center, Vector2.Zero) * 17f, 0, ((Entity)player).whoAmI, new float[0]);
                }
                else
                {
                    SpiderArmorProj.NewCProjectile(((Entity)player).Center, Vector2.Zero, 0, ((Entity)player).whoAmI, new float[0]);
                }
                Collect.cplayers[((Entity)player).whoAmI].SpiderArmorEffectTimer = (int)Timer;
            }
        }

        public void CrystalAssassinArmorEffect(Player? player, GetDataHandlers.PlayerDamageEventArgs? e)
        {
            if (config.CrystalAssassinArmorEffect)
            {
                Item[] armor;
                Vector2 center;
                int num;
                int num2;
                if (player != null)
                {
                    armor = player.armor;
                    center = ((Entity)player).Center;
                    num = 90;
                    num2 = ((Entity)player).whoAmI;
                }
                else
                {
                    armor = Main.player[((GetDataHandledEventArgs)e).Player.Index].armor;
                    center = ((Entity)Main.player[((GetDataHandledEventArgs)e).Player.Index]).Center;
                    num = 94;
                    num2 = ((GetDataHandledEventArgs)e).Player.Index;
                }
                if (armor[0].type == 4982 && armor[1].type == 4983 && armor[2].type == 4984 && (Timer % 50 == 0L || e != null) && (NearestHostileNPC(((Entity)Main.player[num2]).Center, 360000f) != null || e != null))
                {
                    for (int i = 0; i < 20; i++)
                    {
                        Vector2 velocity = Terraria.Utils.RotatedBy(Vector2.UnitY, (double)((float)Math.PI / 10f * (float)i + (float)Math.PI / 20f), default(Vector2)) * (float)((num == 94) ? 4 : 5);
                        int index = Collect.MyNewProjectile(Main.player[num2].GetProjectileSource_Item(armor[0]), center, velocity, num, (num == 94) ? 70 : 40, 5f, num2);
                        CProjectile.Update(index);
                    }
                }
            }
        }

        public void ForbiddenArmorEffect(Player player)
        {
            if (config.ForbiddenArmorEffect)
            {
                Item[] armor = player.armor;
                if (armor[0].type == 3776 && armor[1].type == 3777 && armor[2].type == 3778 && Main.rand.Next(20) == 0)
                {
                    NPC val = NearestHostileNPC(((Entity)player).Center, 1000000f);
                    Vector2 postion = ((Entity)player).Center + Terraria.Utils.RotatedByRandom(Vector2.UnitX, 6.2831854820251465) * (float)Main.rand.Next(100);
                    int index = ((val == null) ? Collect.MyNewProjectile(null, postion, Vector2.Zero, 659, 45, 0f, ((Entity)player).whoAmI, -2f) : Collect.MyNewProjectile(null, postion, Vector2.Zero, 659, 45, 0f, ((Entity)player).whoAmI, ((Entity)val).whoAmI));
                    CProjectile.Update(index);
                }
            }
        }

        public void FrostArmorEffect(Player player)
        {
            if (config.FrostArmorEffect)
            {
                Item[] armor = player.armor;
                if (armor[0].type == 684 && armor[1].type == 685 && armor[2].type == 686 && Timer % 7 == 0)
                {
                    Vector2 postion = ((Entity)player).Center + new Vector2((float)Main.rand.Next(-860, 861), -600f);
                    int index = Collect.MyNewProjectile(null, postion, Vector2.UnitY, 344, 50, 0f, ((Entity)player).whoAmI, 0f, Main.rand.Next(3));
                    CProjectile.Update(index);
                }
            }
        }

        public void HallowedArmorEffect(NpcStrikeEventArgs args)
        {
            var any = config.HallowedArmorEffect;

            if (args.KnockBack == 1.14514f || Main.player[((Entity)args.Player).whoAmI].ownedProjectileCounts[156] + Main.player[((Entity)args.Player).whoAmI].ownedProjectileCounts[157] >= 75)
            {
                return;
            }
            Item[] armor = args.Player.armor;
            if ((armor[1].type == 551 || armor[1].type == 4900) && (armor[2].type == 552 || armor[2].type == 4901) && (armor[0].type == 559 || armor[0].type == 553 || armor[0].type == 558 || armor[0].type == 4873 || armor[0].type == 4896 || armor[0].type == 4897 || armor[0].type == 4898 || armor[0].type == 4899))
            {
                if (Collect.cplayers[((Entity)args.Player).whoAmI].HallowedArmorState)
                {
                    int damage = (int)((double)args.Damage * any);
                    double num = Main.rand.NextDouble();
                    Vector2 val = ((Entity)args.Npc).Center + new Vector2((float)Math.Cos(num * 6.2831854820251465), (float)Math.Sin(num * 6.2831854820251465)) * 300f;
                    Vector2 velocity = Terraria.Utils.SafeNormalize(((Entity)args.Npc).Center - val, Vector2.Zero) * 20f;
                    int index = Collect.MyNewProjectile(null, val, velocity, 156, damage, 1.14514f, ((Entity)args.Player).whoAmI);
                    CProjectile.Update(index);
                }
                else
                {
                    int damage = (int)((double)args.Damage * any);
                    double num2 = Main.rand.NextDouble();
                    Vector2 val2 = ((Entity)args.Npc).Center + new Vector2((float)Math.Cos(num2 * 6.2831854820251465), (float)Math.Sin(num2 * 6.2831854820251465)) * 300f;
                    Vector2 velocity2 = Terraria.Utils.SafeNormalize(((Entity)args.Npc).Center - val2, Vector2.Zero) * 18f;
                    int index2 = Collect.MyNewProjectile(null, val2, velocity2, 157, damage, 1.14514f, ((Entity)args.Player).whoAmI);
                    CProjectile.Update(index2);
                }
            }
        }

        public void ChlorophyteArmorEffect(Player player)
        {
            var Any = config.ChlorophyteArmorEffect;

            Item[] armor = player.armor;
            bool flag = (armor[0].type == 1001 || armor[0].type == 1002 || armor[0].type == 1003) && armor[1].type == 1004 && armor[2].type == 1005;
            if (flag && !Collect.cplayers[((Entity)player).whoAmI].ChlorophyteArmorEffectLife)
            {
                player.statLifeMax += Any;
                Collect.cplayers[((Entity)player).whoAmI].ExtraLife += Any;
                NetMessage.SendData(16, -1, -1, NetworkText.Empty, ((Entity)player).whoAmI, 0f, 0f, 0f, 0, 0, 0);
                Collect.cplayers[((Entity)player).whoAmI].ChlorophyteArmorEffectLife = true;
                if (config.EnableConsumptionMode)
                {
                    SendPlayerText($"生命值上限 + {Any}", new Color(0, 255, 255), ((Entity)player).Center);
                }
            }
            if (!flag && Collect.cplayers[((Entity)player).whoAmI].ChlorophyteArmorEffectLife)
            {
                player.statLifeMax -= Any;
                Collect.cplayers[((Entity)player).whoAmI].ExtraLife -= Any;
                NetMessage.SendData(16, -1, -1, NetworkText.Empty, ((Entity)player).whoAmI, 0f, 0f, 0f, 0, 0, 0);
                Collect.cplayers[((Entity)player).whoAmI].ChlorophyteArmorEffectLife = false;
                if (config.EnableConsumptionMode)
                {
                    SendPlayerText($"生命值上限 - {Any}", new Color(255, 0, 156), player.Center);
                }
            }
        }

        public void TurtleArmorEffect(Player player)
        {
            var Any = config.TurtleArmorEffect;

            Item[] armor = player.armor;
            bool flag = armor[0].type == 1316 && armor[1].type == 1317 && armor[2].type == 1318;
            if (flag && Timer % 180 == 0)
            {
                int num = Main.rand.Next(15, 25);
                for (int i = 0; i < num; i++)
                {
                    Vector2 val = Terraria.Utils.RotatedBy(Vector2.UnitY, (double)((float)Math.PI * 2f / (float)num * (float)i + (float)Math.PI * 2f / (float)(num * 2)), default(Vector2)) * (Terraria.Utils.NextFloat(Main.rand) * 3f + 9f);
                    if (val.Y > 0f)
                    {
                        val.Y *= 0.5f;
                    }
                    int index = Collect.MyNewProjectile(Projectile.GetNoneSource(), ((Entity)player).Center, val, 249, 60, 5f, ((Entity)player).whoAmI);
                    CProjectile.Update(index);
                }
            }
            if (flag && !Collect.cplayers[((Entity)player).whoAmI].TurtleArmorEffectLife)
            {
                player.statLifeMax += Any;
                Collect.cplayers[((Entity)player).whoAmI].ExtraLife += Any;
                NetMessage.SendData(16, -1, -1, NetworkText.Empty, ((Entity)player).whoAmI, 0f, 0f, 0f, 0, 0, 0);
                Collect.cplayers[((Entity)player).whoAmI].TurtleArmorEffectLife = true;
                if (config.EnableConsumptionMode)
                {
                    SendPlayerText($"生命值上限 + {Any}", new Color(0, 255, 255), ((Entity)player).Center);
                }
            }
            if (!flag && Collect.cplayers[((Entity)player).whoAmI].TurtleArmorEffectLife)
            {
                player.statLifeMax -= Any;
                Collect.cplayers[((Entity)player).whoAmI].ExtraLife -= Any;
                NetMessage.SendData(16, -1, -1, NetworkText.Empty, ((Entity)player).whoAmI, 0f, 0f, 0f, 0, 0, 0);
                Collect.cplayers[((Entity)player).whoAmI].TurtleArmorEffectLife = false;
                if (config.EnableConsumptionMode)
                {
                    SendPlayerText($"生命值上限 - {Any}", new Color(255, 0, 156), ((Entity)player).Center);
                }
            }
        }

        public void TikiArmorEffect(Player? pl, ProjectileAiUpdateEventArgs? args, int mode = 0)
        {
            var Any = config.TikiArmorEffect;

            if (args != null)
            {
                Item[] armor = Main.player[args.Projectile.owner].armor;
                Player val = Main.player[args.Projectile.owner];
                bool flag = armor[0].type == 1159 && armor[1].type == 1160 && armor[2].type == 1161;
                int num;
                int num2;
                if (mode == 0)
                {
                    num = 13;
                    num2 = 48;
                }
                else
                {
                    num = 20;
                    num2 = 70;
                }
                if (flag && args.Projectile.ai[0] >= (float)num && args.Projectile.ai[0] <= (float)num2 && args.Projectile.ai[0] % 3f == 0f)
                {
                    List<Vector2> list = new List<Vector2>();
                    Projectile.FillWhipControlPoints(args.Projectile, list);
                    Vector2 val2 = list[list.Count - 2];
                    int index = Collect.MyNewProjectile(null, val2, (val2 - ((Entity)val).Center) * 0.004f, 228, (int)((float)args.Projectile.damage * 0.4f), 0f, ((Entity)val).whoAmI);
                    CProjectile.Update(index);
                }
                return;
            }
            Item[] armor2 = pl.armor;
            bool flag2 = armor2[0].type == 1159 && armor2[1].type == 1160 && armor2[2].type == 1161;
            if (flag2 && !Collect.cplayers[((Entity)pl).whoAmI].TikiArmorEffectLife)
            {
                pl.statLifeMax += Any;
                Collect.cplayers[((Entity)pl).whoAmI].ExtraLife += Any;
                NetMessage.SendData(16, -1, -1, NetworkText.Empty, ((Entity)pl).whoAmI, 0f, 0f, 0f, 0, 0, 0);
                Collect.cplayers[((Entity)pl).whoAmI].TikiArmorEffectLife = true;
                if (config.EnableConsumptionMode)
                {
                    Challenger.SendPlayerText($"生命值上限 + {Any}", new Color(0, 255, 255), ((Entity)pl).Center);
                }
            }
            if (!flag2 && Collect.cplayers[((Entity)pl).whoAmI].TikiArmorEffectLife)
            {
                pl.statLifeMax -= Any;
                Collect.cplayers[((Entity)pl).whoAmI].ExtraLife -= Any;
                NetMessage.SendData(16, -1, -1, NetworkText.Empty, ((Entity)pl).whoAmI, 0f, 0f, 0f, 0, 0, 0);
                Collect.cplayers[((Entity)pl).whoAmI].TikiArmorEffectLife = false;
                if (config.EnableConsumptionMode)
                {
                    SendPlayerText($"生命值上限 - {Any}", new Color(255, 0, 156), ((Entity)pl).Center);
                }
            }
        }

        public void BeetleArmorEffect(Player? player, GetDataHandlers.PlayerDamageEventArgs? e, NpcStrikeEventArgs? args)
        {
            var any1 = config.BeetleArmorEffect_1;
            var any2 = config.BeetleArmorEffect_2;

            if (player != null)
            {
                Item[] armor = player.armor;
                bool flag = armor[0].type == 2199 && (armor[1].type == 2200 || armor[1].type == 2201) && armor[2].type == 2202;
                if (flag && !Collect.cplayers[((Entity)player).whoAmI].BeetleArmorEffectLife)
                {
                    player.statLifeMax += any1;
                    Collect.cplayers[((Entity)player).whoAmI].ExtraLife += any1;
                    NetMessage.SendData(16, -1, -1, NetworkText.Empty, ((Entity)player).whoAmI, 0f, 0f, 0f, 0, 0, 0);
                    Collect.cplayers[((Entity)player).whoAmI].BeetleArmorEffectLife = true;
                    if (config.EnableConsumptionMode)
                    {
                        SendPlayerText($"生命值上限 + {any1}", new Color(0, 255, 255), ((Entity)player).Center);
                    }
                }
                if (!flag && Collect.cplayers[((Entity)player).whoAmI].BeetleArmorEffectLife)
                {
                    player.statLifeMax -= any1;
                    Collect.cplayers[((Entity)player).whoAmI].ExtraLife -= any1;
                    NetMessage.SendData(16, -1, -1, NetworkText.Empty, ((Entity)player).whoAmI, 0f, 0f, 0f, 0, 0, 0);
                    Collect.cplayers[((Entity)player).whoAmI].BeetleArmorEffectLife = false;
                    if (config.EnableConsumptionMode)
                    {
                        SendPlayerText($"生命值上限 - {any1}", new Color(255, 0, 156), ((Entity)player).Center);
                    }
                }
                return;
            }
            if (e != null)
            {
                Item[] armor2 = Main.player[((GetDataHandledEventArgs)e).Player.Index].armor;
                if (armor2[0].type == 2199 && (armor2[1].type == 2200 || armor2[1].type == 2201) && armor2[2].type == 2202)
                {
                    Vector2 center = ((Entity)Main.player[((Entity)((GetDataHandledEventArgs)e).Player.TPlayer).whoAmI]).Center;
                    int num = (int)((double)e.Damage * 0.3);
                    BeetleHeal.NewCProjectile(center, Vector2.Zero, ((Entity)((GetDataHandledEventArgs)e).Player.TPlayer).whoAmI, new float[2] { num, 0f }, 0);
                }
                return;
            }
            Item[] armor3 = args.Player.armor;
            bool flag2 = armor3[0].type == 2199 && (armor3[1].type == 2200 || armor3[1].type == 2201) && armor3[2].type == 2202;
            bool flag3 = false;
            for (int i = 3; i < 10; i++)
            {
                if (armor3[i].type == 938 || armor3[i].type == 3998 || armor3[i].type == 3997)
                {
                    flag3 = true;
                    break;
                }
            }
            if (flag2 && args.KnockBack != 20.114f)
            {
                double num2 = Main.rand.NextDouble();
                Vector2 val = ((Entity)args.Npc).Center + new Vector2((float)Math.Cos(num2 * 6.2831854820251465), (float)Math.Sin(num2 * 6.2831854820251465)) * 250f;
                Vector2 velocity = Terraria.Utils.SafeNormalize(((Entity)args.Npc).Center - val, Vector2.Zero) * 20f;
                int index = Collect.MyNewProjectile(Projectile.GetNoneSource(), val, velocity, 301, flag3 ? ((int)((float)args.Damage * any2)) : ((int)((float)args.Damage * 0.45f)), 20.114f, ((Entity)args.Player).whoAmI);
                CProjectile.Update(index);
            }
        }

        public void ShroomiteArmorEffect(Projectile? projectile, NpcStrikeEventArgs? args)
        {
            if (config.ShroomiteArmorEffect)
            {
                Vector2 val;
                if (projectile != null)
                {
                    if (Main.player[projectile.owner].ownedProjectileCounts[131] >= 100 || projectile.knockBack == 1.14514f)
                    {
                        return;
                    }
                    Item[] armor = Main.player[projectile.owner].armor;
                    bool flag = (armor[0].type == 1546 || armor[0].type == 1547 || armor[0].type == 1548) && armor[1].type == 1549 && armor[2].type == 1550;
                    bool flag2 = projectile.type == 90 || projectile.type == 92 || projectile.type == 640 || projectile.type == 631;
                    if (!(!flag2 && flag) || !projectile.ranged)
                    {
                        return;
                    }
                    val = ((Entity)projectile).Center - ((Entity)Main.player[projectile.owner]).Center;
                    if (((Vector2)(val)).LengthSquared() <= 921600f)
                    {
                        int num = Main.rand.Next(1, 5);
                        for (int i = 0; i < num; i++)
                        {
                            Vector2 unitY = Vector2.UnitY;
                            double num2 = (double)((float)Math.PI * 2f / (float)num * (float)i) + Main.time % 3.0;
                            val = default(Vector2);
                            Vector2 velocity = Terraria.Utils.RotatedBy(unitY, num2, val) * 20f;
                            int index = Collect.MyNewProjectile(Projectile.GetNoneSource(), ((Entity)projectile).Center, velocity, 131, (int)((float)projectile.damage * 0.32f), 1.14514f, projectile.owner);
                            CProjectile.Update(index);
                        }
                    }

                }
                else
                {
                    if (args.Player.ownedProjectileCounts[131] >= 100 || args.KnockBack == 1.14514f)
                    {
                        return;
                    }
                    Item[] armor2 = args.Player.armor;
                    if ((armor2[0].type == 1546 || armor2[0].type == 1547 || armor2[0].type == 1548) && armor2[1].type == 1549 && armor2[2].type == 1550)
                    {
                        int num3 = Main.rand.Next(1, 4);
                        for (int j = 0; j < num3; j++)
                        {
                            Vector2 unitY2 = Vector2.UnitY;
                            double num4 = (double)((float)Math.PI * 2f / (float)num3 * (float)j) + Main.time % 3.14;
                            val = default(Vector2);
                            Vector2 val2 = Terraria.Utils.RotatedBy(unitY2, num4, val) * 70f;
                            int index2 = Collect.MyNewProjectile(null, ((Entity)args.Npc).Center + val2, Vector2.Zero, 131, (int)((float)args.Damage * 0.25f), 1.14514f, ((Entity)args.Player).whoAmI);
                            CProjectile.Update(index2);
                        }
                    }
                }
            }
        }

        public void SpectreArmorEffect(Player player)
        {
            var any = config.SpectreArmorEffect;

            Item[] armor = player.armor;
            bool flag = armor[0].type == 1503 && armor[1].type == 1504 && armor[2].type == 1505;
            if (flag && !Collect.cplayers[((Entity)player).whoAmI].SpectreArmorEffectLife)
            {
                player.statLifeMax += any;
                Collect.cplayers[((Entity)player).whoAmI].ExtraLife += any;
                NetMessage.SendData(16, -1, -1, NetworkText.Empty, ((Entity)player).whoAmI, 0f, 0f, 0f, 0, 0, 0);
                Collect.cplayers[((Entity)player).whoAmI].SpectreArmorEffectLife = true;
                if (config.EnableConsumptionMode)
                {
                    SendPlayerText($"生命值上限 + {any}", new Color(0, 255, 255), ((Entity)player).Center);
                }
                Collect.cplayers[((Entity)player).whoAmI].SpectreArmorEffectProjIndex = ((Entity)SpectreArmorProj.NewCProjectile(((Entity)player).Center + Vector2.UnitY * 100f, Vector2.Zero, ((Entity)player).whoAmI, new float[0], 1).proj).whoAmI;
            }
            else if (flag && Collect.cplayers[((Entity)player).whoAmI].SpectreArmorEffectLife && !Collect.cprojs[Collect.cplayers[((Entity)player).whoAmI].SpectreArmorEffectProjIndex].isActive)
            {
                Collect.cplayers[((Entity)player).whoAmI].SpectreArmorEffectProjIndex = ((Entity)SpectreArmorProj.NewCProjectile(((Entity)player).Center + Vector2.UnitY * 100f, Vector2.Zero, ((Entity)player).whoAmI, new float[0], 1).proj).whoAmI;
            }
            else if (!flag && Collect.cplayers[((Entity)player).whoAmI].SpectreArmorEffectLife)
            {
                player.statLifeMax -= any;
                Collect.cplayers[((Entity)player).whoAmI].ExtraLife -= any;
                NetMessage.SendData(16, -1, -1, NetworkText.Empty, ((Entity)player).whoAmI, 0f, 0f, 0f, 0, 0, 0);
                Collect.cplayers[((Entity)player).whoAmI].SpectreArmorEffectLife = false;
                if (config.EnableConsumptionMode)
                {
                    SendPlayerText($"生命值上限 - {any}", new Color(255, 0, 156), ((Entity)player).Center);
                }
                CProjectile.CKill(Collect.cplayers[((Entity)player).whoAmI].SpectreArmorEffectProjIndex);
            }
            flag = armor[0].type == 2189 && armor[1].type == 1504 && armor[2].type == 1505;
            if (flag && !Collect.cplayers[((Entity)player).whoAmI].SpectreArmorEffectMana)
            {
                player.statManaMax += any;
                Collect.cplayers[((Entity)player).whoAmI].ExtraMana += any;
                NetMessage.SendData(42, -1, -1, NetworkText.Empty, ((Entity)player).whoAmI, 0f, 0f, 0f, 0, 0, 0);
                Collect.cplayers[((Entity)player).whoAmI].SpectreArmorEffectMana = true;
                if (config.EnableConsumptionMode)
                {
                    SendPlayerText($"魔力值上限 + {any}", new Color(0, 255, 255), ((Entity)player).Center + new Vector2(0f, 32f));
                }
                Collect.cplayers[((Entity)player).whoAmI].SpectreArmorEffectProjIndex = ((Entity)SpectreArmorProj.NewCProjectile(((Entity)player).Center + Vector2.UnitY * 100f, Vector2.Zero, ((Entity)player).whoAmI, new float[0], 1).proj).whoAmI;
            }
            else if (flag && Collect.cplayers[((Entity)player).whoAmI].SpectreArmorEffectMana && !Collect.cprojs[Collect.cplayers[((Entity)player).whoAmI].SpectreArmorEffectProjIndex].isActive)
            {
                Collect.cplayers[((Entity)player).whoAmI].SpectreArmorEffectProjIndex = SpectreArmorProj.NewCProjectile(((Entity)player).Center + Vector2.UnitY * 100f, Vector2.Zero, ((Entity)player).whoAmI, new float[0], 1).proj.whoAmI;
            }
            if (!flag && Collect.cplayers[((Entity)player).whoAmI].SpectreArmorEffectMana)
            {
                player.statManaMax -= any;
                Collect.cplayers[((Entity)player).whoAmI].ExtraMana -= any;
                NetMessage.SendData(42, -1, -1, NetworkText.Empty, ((Entity)player).whoAmI, 0f, 0f, 0f, 0, 0, 0);
                Collect.cplayers[((Entity)player).whoAmI].SpectreArmorEffectMana = false;
                if (config.EnableConsumptionMode)
                {
                    SendPlayerText($"魔力值上限 - {any}", new Color(255, 0, 156), ((Entity)player).Center + new Vector2(0f, 32f));
                }
                CProjectile.CKill(Collect.cplayers[((Entity)player).whoAmI].SpectreArmorEffectProjIndex);
            }
        }

        public void SpookyArmorEffect(ProjectileAiUpdateEventArgs args, int mode = 0)
        {
            if (config.SpookyArmorEffect)
            {
                Item[] armor = Main.player[args.Projectile.owner].armor;
                Player val = Main.player[args.Projectile.owner];
                bool flag = armor[0].type == 1832 && armor[1].type == 1833 && armor[2].type == 1834;
                int num = ((mode == 0) ? 13 : 35);
                int num2 = ((mode == 0) ? 48 : 70);
                int type = (Main.dayTime ? 316 : 321);
                if (flag && args.Projectile.ai[0] >= (float)num && args.Projectile.ai[0] <= (float)num2 && args.Projectile.ai[0] % 4f == 0f)
                {
                    List<Vector2> list = new List<Vector2>();
                    Projectile.FillWhipControlPoints(args.Projectile, list);
                    Vector2 val2 = list[list.Count - 2];
                    int index = Collect.MyNewProjectile(null, val2, (val2 - ((Entity)val).Center) * 0.008f, type, (int)((double)args.Projectile.damage * 0.2), 0f, ((Entity)val).whoAmI);
                    CProjectile.Update(index);
                }
            }
        }

        public void HivePack(Player player)
        {
            if (config.HivePack)
            {
                if (Main.rand.Next(30) == 0)
                {
                    Honey.NewCProjectile(((Entity)player).Center, -Terraria.Utils.RotatedByRandom(Vector2.UnitY, 1.5707963705062866) * 7f, 2, ((Entity)player).whoAmI, new float[1]);
                }
            }
        }

        public void RoyalGel(Player player)
        {
            if (config.RoyalGel)
            {
                if (Timer % 120 == 0)
                {
                    int num = Item.NewItem((IEntitySource)null, ((Entity)player).Center + new Vector2((float)Main.rand.Next(-860, 861), -600f), new Vector2(36f, 36f), 23, 1, false, 0, false, false);
                    Main.item[num].color = new Color(Main.rand.Next(256), Main.rand.Next(256), Main.rand.Next(256));
                    TSPlayer.All.SendData((PacketTypes)88, (string)null, num, 1f, 0f, 0f, 0);
                }
            }
        }

        public void CthulhuShield(Player player)
        {
            if (player.dashDelay == -1 && Timer - Collect.cplayers[((Entity)player).whoAmI].CthulhuShieldTime >= 720)
            {
                NetMessage.SendData(62, -1, -1, (NetworkText)null, ((Entity)player).whoAmI, 2f, 0f, 0f, 0, 0, 0);
                Collect.cplayers[((Entity)player).whoAmI].CthulhuShieldTime = (int)Timer;
            }
            if (Timer - Collect.cplayers[((Entity)player).whoAmI].CthulhuShieldTime == 720)
            {
                SendPlayerText(TShock.Players[((Entity)player).whoAmI], "克苏鲁之盾冷却完成", new Color(255, 183, 183), ((Entity)player).Center);
            }
        }

        public void WormScarf(Player player)
        {
            var Any = config.WormScarf;

            bool flag = false;
            for (int i = 0; i < 22; i++)
            {
                if (player.buffType[i] == 39 || player.buffType[i] == 69 || player.buffType[i] == 44 || player.buffType[i] == 46 || player.buffType[i] == Any)
                {
                    flag = true;
                    player.buffType[i] = 0;
                }
            }
            if (flag)
            {
                TShock.Players[((Entity)player).whoAmI].SendData((PacketTypes)50, "", ((Entity)player).whoAmI, 0f, 0f, 0f, 0);
            }
        }

        public void VolatileGelatin(NpcStrikeEventArgs args)
        {
            if (args.Npc.CanBeChasedBy((object)null, false) && !args.Npc.SpawnedFromStatue)
            {
                int num = Main.rand.Next(700);
                if (num >= 90 && num < 100)
                {
                    Item.NewItem((IEntitySource)null, ((Entity)args.Npc).Center, new Vector2(20f, 20f), 502, 1, false, 0, false, false);
                }
                else if (num >= 80 && num < 90 && Main.BestiaryTracker.Kills.GetKillCount(ContentSamples.NpcBestiaryCreditIdsByNpcNetIds[-4]) > 0)
                {
                    Item.NewItem((IEntitySource)null, ((Entity)args.Npc).Center, new Vector2(20f, 20f), 3111, 1, false, 0, false, false);
                }
                else if (num >= 60 && num < 80)
                {
                    Item.NewItem((IEntitySource)null, ((Entity)args.Npc).Center, new Vector2(20f, 20f), 409, 1, false, 0, false, false);
                }
                else if (num >= 40 && num < 60)
                {
                    Item.NewItem((IEntitySource)null, ((Entity)args.Npc).Center, new Vector2(20f, 20f), 23, 1, false, 0, false, false);
                }
            }
        }

        public void DisplayTips(TSPlayer tsplayer, short type)
        {
            switch (type)
            {
                case 2367:
                case 2368:
                case 2369:
                    SendPlayerText(tsplayer, "【垂钓套装】\n挑战模式奖励：给予永久的声纳、钓鱼、宝匣、镇\n定Buff", new Color(91, 101, 132), ((Entity)Main.player[tsplayer.Index]).Center + new Vector2(0f, -24f));
                    break;
                case 256:
                case 257:
                case 258:
                    SendPlayerText(tsplayer, "【忍者套装】\n挑战模式奖励：有四分之一概率闪避非致命伤害并\n释放烟雾", Color.Black, ((Entity)Main.player[tsplayer.Index]).Center + new Vector2(0f, -24f));
                    break;
                case 3374:
                case 3375:
                case 3376:
                    SendPlayerText(tsplayer, "【化石套装】\n挑战模式奖励：在头上召唤一个琥珀光球，向敌人\n抛出极快的闪电矢", new Color(232, 205, 119), ((Entity)Main.player[tsplayer.Index]).Center + new Vector2(0f, -32f));
                    break;
                case 792:
                case 793:
                case 794:
                    SendPlayerText(tsplayer, "【猩红套装】\n挑战模式奖励：暴击时从周围每个敌怪处吸取一定\n血量随着敌怪数目增多吸血量-1，冷却 5秒", new Color(209, 46, 93), ((Entity)Main.player[tsplayer.Index]).Center + new Vector2(0f, -24f));
                    break;
                case 100:
                case 101:
                case 102:
                case 956:
                case 957:
                case 958:
                    SendPlayerText(tsplayer, "【暗影套装】\n挑战模式奖励：暴击时从玩家周围生成吞噬怪飞弹\n攻击周围敌人，冷却 1秒", new Color(95, 91, 207), ((Entity)Main.player[tsplayer.Index]).Center + new Vector2(0f, -24f));
                    break;
                case 123:
                case 124:
                case 125:
                    SendPlayerText(tsplayer, "【陨石套装】\n挑战模式奖励：暴击时恢复些许魔力，间歇地降下\n高伤害落星攻击敌人", new Color(128, 15, 12), ((Entity)Main.player[tsplayer.Index]).Center + new Vector2(0f, -24f));
                    break;
                case 228:
                case 229:
                case 230:
                case 960:
                case 961:
                case 962:
                    SendPlayerText(tsplayer, "【丛林套装】\n挑战模式奖励：间歇地从玩家周围生成伤害性的孢子", new Color(101, 151, 8), ((Entity)Main.player[tsplayer.Index]).Center + new Vector2(0f, -16f));
                    break;
                case 151:
                case 152:
                case 153:
                case 959:
                    SendPlayerText(tsplayer, "【死灵套装】\n挑战模式奖励：受到伤害时，向四周飞溅骨头；攻\n击时偶尔发射骨箭", new Color(113, 113, 36), ((Entity)Main.player[tsplayer.Index]).Center + new Vector2(0f, -24f));
                    break;
                case 2361:
                case 2362:
                case 2363:
                    SendPlayerText(tsplayer, "【蜜蜂套装】\n挑战模式奖励：给予永久的蜂蜜增益；不间断地向\n四周撒蜂糖罐，玩家接触后回血并给予15秒蜂蜜增\n益；对玩家自身的治疗量略低于对其他玩家", new Color(232, 229, 74), ((Entity)Main.player[tsplayer.Index]).Center + new Vector2(0f, -32f));
                    break;
                case 3266:
                case 3267:
                case 3268:
                    SendPlayerText(tsplayer, "【黑曜石套装】\n挑战模式奖励：因为盗贼的祝福，掉落物会尝试掉落两次\n(仅对非boss生物和非高血量怪物有效)", new Color(90, 83, 160), ((Entity)Main.player[tsplayer.Index]).Center + new Vector2(0f, -24f));
                    break;
                case 231:
                case 232:
                case 233:
                    SendPlayerText(tsplayer, "【狱炎套装】\n挑战模式奖励：免疫岩浆，给予永久的地狱火增益", new Color(255, 27, 0), ((Entity)Main.player[tsplayer.Index]).Center + new Vector2(0f, -16f));
                    break;
                case 2370:
                case 2371:
                case 2372:
                    SendPlayerText(tsplayer, "【蜘蛛套装】\n挑战模式奖励：攻击时，给予敌人中毒和剧毒减益\n，按“up”键生成一个毒牙药水瓶，砸中敌人时爆炸", new Color(184, 79, 29), ((Entity)Main.player[tsplayer.Index]).Center + new Vector2(0f, -32f));
                    break;
                case 4982:
                case 4983:
                case 4984:
                    SendPlayerText(tsplayer, "【水晶刺客套装】\n挑战模式奖励：当有敌人在附近时，自身释放出水\n晶碎片；若玩家被击中，释放出更强大的碎片", new Color(221, 83, 146), ((Entity)Main.player[tsplayer.Index]).Center + new Vector2(0f, -24f));
                    break;
                case 3776:
                case 3777:
                case 3778:
                    SendPlayerText(tsplayer, "【禁戒套装】\n挑战模式奖励：释放自动寻的灵焰魂火攻击附近的\n敌人", new Color(222, 171, 26), ((Entity)Main.player[tsplayer.Index]).Center + new Vector2(0f, -24f));
                    break;
                case 684:
                case 685:
                case 686:
                    SendPlayerText(tsplayer, "【寒霜套装】\n挑战模式奖励：你周围开始下雪", new Color(31, 193, 229), ((Entity)Main.player[tsplayer.Index]).Center + new Vector2(0f, -16f));
                    break;
                case 551:
                case 552:
                case 553:
                case 558:
                case 559:
                case 4873:
                case 4896:
                case 4897:
                case 4898:
                case 4899:
                case 4900:
                case 4901:
                    SendPlayerText(tsplayer, "【神圣套装】\n挑战模式奖励：击中敌人时召唤光与暗剑气，输入\n“/cf”切换剑气类型", new Color(179, 179, 203), Main.player[tsplayer.Index].Center + new Vector2(0f, -16f));
                    break;
                case 1001:
                case 1002:
                case 1003:
                case 1004:
                case 1005:
                    SendPlayerText(tsplayer, "【叶绿套装】\n挑战模式奖励：释放不精确的叶绿水晶矢，丛林之\n力给你更高的生命上限", new Color(103, 209, 0), ((Entity)Main.player[tsplayer.Index]).Center + new Vector2(0f, -24f));
                    break;
                case 1316:
                case 1317:
                case 1318:
                    SendPlayerText(tsplayer, "【海龟套装】\n挑战模式奖励：增加60血上限，自动在附近释放爆\n炸碎片", new Color(169, 104, 69), ((Entity)Main.player[tsplayer.Index]).Center + new Vector2(0f, -24f));
                    break;
                case 1159:
                case 1160:
                case 1161:
                    SendPlayerText(tsplayer, "【提基套装】\n挑战模式奖励：增加20血上限，在鞭子的轨迹上留\n下孢子", Color.Green, ((Entity)Main.player[tsplayer.Index]).Center + new Vector2(0f, -24f));
                    break;
                case 2199:
                case 2200:
                case 2201:
                case 2202:
                    SendPlayerText(tsplayer, "【甲虫套装】\n挑战模式奖励：增加60血上限，敌人的伤害的一部\n分会治疗周围的队友并给予buff；当装备帕拉丁盾\n或其上级合成物时，帕拉丁之锤伤害翻倍", new Color(101, 75, 120), ((Entity)Main.player[tsplayer.Index]).Center + new Vector2(0f, -32f));
                    break;
                case 1546:
                case 1547:
                case 1548:
                case 1549:
                case 1550:
                    SendPlayerText(tsplayer, "【蘑菇套装】\n挑战模式奖励：射弹会不稳定地留下蘑菇", new Color(47, 36, 237), ((Entity)Main.player[tsplayer.Index]).Center + new Vector2(0f, -16f));
                    break;
                case 1503:
                case 1504:
                case 1505:
                case 2189:
                    SendPlayerText(tsplayer, "【幽魂套装】\n挑战模式奖励：根据头饰选择增加40血上限或80魔\n力上限；召唤 2个幽魂诅咒环绕玩家，向附近敌人攻击", new Color(166, 169, 218), ((Entity)Main.player[tsplayer.Index]).Center + new Vector2(0f, -24f));
                    break;
                case 1832:
                case 1833:
                case 1834:
                    SendPlayerText(tsplayer, "【阴森套装】\n挑战模式奖励：使用鞭子时，甩出蝙蝠或南\n瓜头", new Color(85, 75, 126), ((Entity)Main.player[tsplayer.Index]).Center + new Vector2(0f, -24f));
                    break;
                case 3090:
                    SendPlayerText(tsplayer, "【皇家凝胶】\n挑战模式奖励：天空开始下凝胶小雨", new Color(0, 189, 238), ((Entity)Main.player[tsplayer.Index]).Center + new Vector2(0f, 0f));
                    break;
                case 3097:
                    SendPlayerText(tsplayer, "【克苏鲁之盾】\n挑战模式奖励：冲刺时获得一小段无敌时间，冷却\n12秒", new Color(255, 199, 199), ((Entity)Main.player[tsplayer.Index]).Center + new Vector2(0f, 0f));
                    break;
                case 3223:
                    SendPlayerText(tsplayer, "【混乱之脑】\n挑战模式奖励：输入“/cf”混乱周围所有敌怪", new Color(241, 108, 108), ((Entity)Main.player[tsplayer.Index]).Center + new Vector2(0f, 0f));
                    break;
                case 3224:
                    SendPlayerText(tsplayer, "【蠕虫围巾】\n挑战模式奖励：免疫寒冷，霜火，灵液和咒火", new Color(166, 127, 231), ((Entity)Main.player[tsplayer.Index]).Center + new Vector2(0f, 0f));
                    break;
                case 5113:
                    SendPlayerText(tsplayer, "【收音机零件】\n挑战模式奖励：输入“/cf”收听天气预报，在困难\n模式中可以收听世界先知预报", new Color(167, 218, 251), ((Entity)Main.player[tsplayer.Index]).Center + new Vector2(0f, 0f));
                    break;
                case 3333:
                    SendPlayerText(tsplayer, "【蜜蜂背包】\n挑战模式奖励：不间断地向四周扔出毒蜂罐，爆炸\n后释放一只蜜蜂", new Color(232, 229, 74), ((Entity)Main.player[tsplayer.Index]).Center);
                    break;
                case 4987:
                    SendPlayerText(tsplayer, "【挥发明胶】\n挑战模式奖励：击中敌人有概率掉落碎魔晶，珍珠\n石，凝胶等", new Color(232, 229, 74), ((Entity)Main.player[tsplayer.Index]).Center);
                    break;
            }
        }

        private void OnGreetPlayer(GreetPlayerEventArgs args)
        {
            if (Collect.cplayers[args.Who] == null || !Collect.cplayers[args.Who].isActive)
            {
                Collect.cplayers[args.Who] = new CPlayer(TShock.Players[args.Who], tips: true);
            }
            if (config.enableChallenge)
            {
                TShock.Players[args.Who].SendMessage("世界已开启挑战模式，祝您好运！", new Color(255, 82, 165));
            }
            else
            {
                TShock.Players[args.Who].SendMessage("世界已关闭挑战模式，快乐游玩吧", new Color(82, 155, 119));
            }
        }

        private void OnServerLeave(LeaveEventArgs args)
        {
            if (args == null || TShock.Players[args.Who] == null)
            {
                return;
            }
            try
            {
                if (Collect.cplayers[args.Who] != null)
                {
                    if (Collect.cplayers[args.Who].ExtraLife > 0)
                    {
                        Player obj = Main.player[args.Who];
                        obj.statLifeMax -= Collect.cplayers[args.Who].ExtraLife;
                        NetMessage.SendData(16, -1, -1, NetworkText.Empty, args.Who, 0f, 0f, 0f, 0, 0, 0);
                    }
                    if (Collect.cplayers[args.Who].ExtraMana > 0)
                    {
                        Player obj2 = Main.player[args.Who];
                        obj2.statManaMax -= Collect.cplayers[args.Who].ExtraMana;
                        NetMessage.SendData(42, -1, -1, NetworkText.Empty, args.Who, 0f, 0f, 0f, 0, 0, 0);
                    }
                    for (int i = 0; i < 1000; i++)
                    {
                        if (Collect.cprojs[i] != null && Collect.cprojs[i].isActive)
                        {
                            Collect.cprojs[i].CKill();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Challenger.OnServerLeave异常3：" + ex.Message);
                TShock.Log.Error("Challenger.OnServerLeave异常3：" + ex.Message);
            }
            Collect.cplayers[args.Who].isActive = false;
        }

        private void OnGameUpdate(EventArgs args)
        {
            Timer++;
            if (!config.enableChallenge)
            {
                return;
            }
            if (Collect.worldevent != 0)
            {
                switch (Collect.worldevent)
                {
                    case 1:
                        if (Main.time == 1.0 && !Main.dayTime)
                        {
                            TSPlayer.Server.SetFullMoon();
                            Collect.worldevent = 0;
                        }
                        break;
                    case 2:
                        if (Main.time == 1.0 && !Main.dayTime)
                        {
                            TSPlayer.Server.SetBloodMoon(true);
                            Collect.worldevent = 0;
                        }
                        break;
                    case 3:
                        if (Main.time == 1.0 && Main.dayTime)
                        {
                            TSPlayer.Server.SetEclipse(true);
                            Collect.worldevent = 0;
                        }
                        break;
                    case 4:
                        if (Main.time == 1.0 && !Main.dayTime && !LanternNight.LanternsUp)
                        {
                            LanternNight.ToggleManualLanterns();
                            Collect.worldevent = 0;
                        }
                        break;
                    case 5:
                        if (Main.time == 1.0 && !Main.dayTime)
                        {
                            WorldGen.spawnMeteor = false;
                            WorldGen.dropMeteor();
                            Collect.worldevent = 0;
                        }
                        break;
                }
            }
            TSPlayer[] players = TShock.Players;
            foreach (TSPlayer val in players)
            {
                if (val == null || Collect.cplayers[val.Index] == null || !Collect.cplayers[val.Index].isActive)
                {
                    continue;
                }
                Player tPlayer = val.TPlayer;
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append(tPlayer.armor[0].type).Append(tPlayer.armor[1].type).Append(tPlayer.armor[2].type);
                switch (stringBuilder.ToString())
                {
                    case "228229230":
                    case "960961962":
                    case "228961230":
                    case "228229962":
                    case "960229230":
                    case "960961230":
                    case "960229962":
                    case "228961962":
                        JungleArmorEffect(tPlayer);
                        break;
                    case "231232233":
                        MoltenArmor(tPlayer);
                        break;
                    case "236723682369":
                        AnglerArmorEffect(tPlayer);
                        break;
                    case "236123622363":
                        BeeArmorEffect(tPlayer);
                        break;
                    case "123124125":
                        MeteorArmorEffect(null, tPlayer);
                        break;
                    case "237023712372":
                        SpiderArmorEffect(null, tPlayer);
                        break;
                    case "377637773778":
                        ForbiddenArmorEffect(tPlayer);
                        break;
                    case "498249834984":
                        CrystalAssassinArmorEffect(tPlayer, null);
                        break;
                    case "684685686":
                        FrostArmorEffect(tPlayer);
                        break;
                    default:
                        if (Timer % 5 == 0)
                        {
                            FossilArmorEffect(tPlayer);
                            ChlorophyteArmorEffect(tPlayer);
                            TurtleArmorEffect(tPlayer);
                            TikiArmorEffect(tPlayer, null);
                            BeetleArmorEffect(tPlayer, null, null);
                            SpectreArmorEffect(tPlayer);
                        }
                        break;
                }
                if (Timer % 4 != 0)
                {
                    continue;
                }
                Item[] armor = tPlayer.armor;
                for (int j = 3; j < 10; j++)
                {
                    switch (armor[j].type)
                    {
                        case 3333:
                            HivePack(tPlayer);
                            break;
                        case 3090:
                            RoyalGel(tPlayer);
                            break;
                        case 3224:
                            WormScarf(tPlayer);
                            break;
                        case 3097:
                            CthulhuShield(tPlayer);
                            break;
                    }
                }
            }
            if (honey.Count == 0)
            {
                return;
            }
            foreach (KeyValuePair<int, int> item in honey)
            {
                if (item.Value < 4)
                {
                    CProjectile.CKill(item.Key);
                    honey[item.Key]++;
                }
                else
                {
                    honey.Remove(item.Key);
                }
            }
        }

        private void OnHoldItem(object? sender, PlayerSlotEventArgs e)
        {
            if (e.Slot == 58 && e.Stack != 0 && config.enableChallenge && Collect.cplayers[((GetDataHandledEventArgs)e).Player.Index] != null && Collect.cplayers[((GetDataHandledEventArgs)e).Player.Index].isActive && Collect.cplayers[((GetDataHandledEventArgs)e).Player.Index].tips)
            {
                DisplayTips(((GetDataHandledEventArgs)e).Player, e.Type);
            }
        }

        private void OnNpcStrike(NpcStrikeEventArgs args)
        {
            if (!config.enableChallenge)
            {
                return;
            }
            switch (args.Player.armor[2].type)
            {
                case 794:
                    CrimsonArmorEffect(args);
                    break;
                case 100:
                case 958:
                    ShadowArmorEffect(args);
                    break;
                case 125:
                    MeteorArmorEffect(args, null);
                    break;
                case 3268:
                    ObsidianArmorEffect(args);
                    break;
                case 153:
                    NecroArmor(null, args);
                    break;
                case 2372:
                    SpiderArmorEffect(args, null);
                    break;
                case 552:
                case 4901:
                    HallowedArmorEffect(args);
                    break;
                case 2202:
                    BeetleArmorEffect(null, null, args);
                    break;
                case 1550:
                    ShroomiteArmorEffect(null, args);
                    break;
            }
            Item[] armor = args.Player.armor;
            for (int i = 3; i < 10; i++)
            {
                int type = armor[i].type;
                int num = type;
                if (num == 4987)
                {
                    VolatileGelatin(args);
                }
            }
            if (Collect.cnpcs[((Entity)args.Npc).whoAmI] != null && Collect.cnpcs[((Entity)args.Npc).whoAmI].isActive)
            {
                Collect.cnpcs[((Entity)args.Npc).whoAmI].WhenHurtByPlayer(args);
            }
        }

        private void OnNPCKilled(object? sender, KilledEventArgs e)
        {
            if (config.enableChallenge)
            {
                NPC npc = e.Npc;
                if (Collect.cnpcs[((Entity)npc).whoAmI] != null && Collect.cnpcs[((Entity)npc).whoAmI].isActive)
                {
                    Collect.cnpcs[((Entity)npc).whoAmI].OnKilled();
                }
            }
        }

        private void OnNPCAI(NpcAiUpdateEventArgs args)
        {
            if (!config.enableChallenge)
            {
                return;
            }
            if (config.enableBossAI)
            {
                if (Collect.cnpcs[((Entity)args.Npc).whoAmI] == null || !Collect.cnpcs[((Entity)args.Npc).whoAmI].isActive)
                {
                    if (args.Npc.lifeMax > 5 && !Collect.noneedlifeNPC.Contains(args.Npc.netID))
                    {
                        args.Npc.lifeMax = (int)((float)args.Npc.lifeMax * config.lifeXnum);
                        args.Npc.life = args.Npc.lifeMax + 1;
                    }
                    else if (args.Npc.lifeMax == 1)
                    {
                        args.Npc.lifeMax = 100;
                        args.Npc.life = 101;
                    }
                    switch (args.Npc.netID)
                    {
                        case 50:
                            Collect.cnpcs[((Entity)args.Npc).whoAmI] = new SlimeKing(args.Npc);
                            break;
                        case 4:
                            Collect.cnpcs[((Entity)args.Npc).whoAmI] = new EyeofCthulhu(args.Npc);
                            break;
                        case 5:
                            Collect.cnpcs[((Entity)args.Npc).whoAmI] = new EyeofCthulhu_DemonEye(args.Npc);
                            break;
                        case 266:
                            Collect.cnpcs[((Entity)args.Npc).whoAmI] = new BrainofCthulhu(args.Npc);
                            break;
                        case 267:
                            Collect.cnpcs[((Entity)args.Npc).whoAmI] = new Creeper(args.Npc);
                            break;
                        case 13:
                            Collect.cnpcs[((Entity)args.Npc).whoAmI] = new EaterofWorldsHead(args.Npc);
                            break;
                        case 14:
                            Collect.cnpcs[((Entity)args.Npc).whoAmI] = new EaterofWorldsBody(args.Npc);
                            break;
                        case 15:
                            Collect.cnpcs[((Entity)args.Npc).whoAmI] = new EaterofWorldsTail(args.Npc);
                            break;
                        case 668:
                            Collect.cnpcs[((Entity)args.Npc).whoAmI] = new Deerclops(args.Npc);
                            break;
                        case 35:
                            Collect.cnpcs[((Entity)args.Npc).whoAmI] = new Skeletron(args.Npc);
                            break;
                        case 36:
                            Collect.cnpcs[((Entity)args.Npc).whoAmI] = new SkeletronHand(args.Npc);
                            break;
                        case 34:
                            Collect.cnpcs[((Entity)args.Npc).whoAmI] = new Skeletron_Surrand(args.Npc);
                            break;
                        case 222:
                            Collect.cnpcs[((Entity)args.Npc).whoAmI] = new QueenBee(args.Npc);
                            break;
                        case 114:
                            Collect.cnpcs[((Entity)args.Npc).whoAmI] = new WallofFleshEye(args.Npc);
                            break;
                        case 113:
                            Collect.cnpcs[((Entity)args.Npc).whoAmI] = new WallofFlesh(args.Npc);
                            break;
                        default:
                            Collect.cnpcs[((Entity)args.Npc).whoAmI] = new CNPC(args.Npc);
                            break;
                    }
                }
                else
                {
                    Collect.cnpcs[((Entity)args.Npc).whoAmI].NPCAI();
                }
            }
        }
        private void OnProjSpawn(object? sender, NewProjectileEventArgs e)
        {
            if (config.enableChallenge)
            {
                short type = e.Type;
                short num = type;
                if (num == 227)
                {
                    Collect.cprojs[e.Identity] = new CrystalLeafShot(Main.projectile[e.Identity]);
                    Collect.cprojs[e.Identity].MyEffect();
                }
            }
        }

        private void OnProjAIUpdate(ProjectileAiUpdateEventArgs args)
        {
            if (!config.enableChallenge)
            {
                return;
            }
            if (Collect.cprojs[((Entity)args.Projectile).whoAmI] != null && Collect.cprojs[((Entity)args.Projectile).whoAmI].isActive)
            {
                Collect.cprojs[((Entity)args.Projectile).whoAmI].ProjectileAI();
                return;
            }
            switch (args.Projectile.type)
            {
                case 841:
                case 912:
                case 913:
                case 914:
                case 952:
                    TikiArmorEffect(null, args);
                    SpookyArmorEffect(args);
                    break;
                case 847:
                case 848:
                case 849:
                case 915:
                    TikiArmorEffect(null, args, 1);
                    SpookyArmorEffect(args, 1);
                    break;
            }
        }

        private void OnProjKilled(object? sender, ProjectileKillEventArgs e)
        {
            if (e.ProjectileIndex < 0 || e.ProjectileIndex > 999)
            {
                return;
            }
            Projectile projectile = Main.projectile[e.ProjectileIndex];
            if (config.enableChallenge)
            {
                if (Collect.cprojs[e.ProjectileIndex] != null)
                {
                    Collect.cprojs[e.ProjectileIndex].PreProjectileKilled();
                }
                ShroomiteArmorEffect(projectile, null);
            }
        }

        private void PlayerSufferDamage(object? sender, PlayerDamageEventArgs e)
        {
            if (!config.enableChallenge)
            {
                return;
            }
            if (config.enableMonsterSucksBlood)
            {
                if (e.PlayerDeathReason._sourceNPCIndex != -1)
                {
                    TouchedAndBeSucked(e);
                }
                else if (e.PlayerDeathReason._sourceProjectileType != -1)
                {
                    ProjAndBeSucked(e);
                }
            }
            if (e.PlayerDeathReason._sourceNPCIndex != -1)
            {
                int sourceNPCIndex = e.PlayerDeathReason._sourceNPCIndex;
                if (Collect.cnpcs[sourceNPCIndex] != null && Collect.cnpcs[sourceNPCIndex].isActive)
                {
                    Collect.cnpcs[sourceNPCIndex].OnHurtPlayers(e);
                }
            }
            else if (e.PlayerDeathReason._sourceProjectileType == -1)
            {
            }
            if (!e.PVP)
            {
                switch (Main.player[((GetDataHandledEventArgs)e).Player.Index].armor[2].type)
                {
                    case 153:
                        NecroArmor(e, null);
                        break;
                    case 258:
                        NinjaArmorEffect(e);
                        break;
                    case 4984:
                        CrystalAssassinArmorEffect(null, e);
                        break;
                    case 2202:
                        BeetleArmorEffect(null, e, null);
                        break;
                }
            }
        }

        private void EnableTips(CommandArgs args)
        {
            if (args.Parameters.Any())
            {
                args.Player.SendInfoMessage("输入 /ctip 来启用内容提示，如各种物品装备的修改文字提示，再次使用取消");
            }
            else if (!config.enableChallenge)
            {
                args.Player.SendInfoMessage("挑战模式已关闭，无法开启文字提示");
            }
            else if (Collect.cplayers[args.Player.Index] != null && Collect.cplayers[args.Player.Index].isActive && Collect.cplayers[args.Player.Index].tips)
            {
                Collect.cplayers[args.Player.Index].tips = false;
                args.Player.SendMessage("文字提示已取消", new Color(45, 187, 45));
            }
            else if (Collect.cplayers[args.Player.Index] != null && Collect.cplayers[args.Player.Index].isActive && !Collect.cplayers[args.Player.Index].tips)
            {
                Collect.cplayers[args.Player.Index].tips = true;
                args.Player.SendMessage("文字提示已启用", new Color(45, 187, 45));
            }
        }

        private void EnableModel(CommandArgs args)
        {
            if (args.Parameters.Any())
            {
                args.Player.SendInfoMessage("输入 /cenable 来启用挑战模式，再次使用取消");
                return;
            }
            if (config.enableChallenge)
            {
                config.enableChallenge = false;
                LinqExt.ForEach<CProjectile>((IEnumerable<CProjectile>)Collect.cprojs, (Action<CProjectile>)delegate (CProjectile x)
                {
                    if (x != null)
                    {
                        x.CKill();
                        x.isActive = false;
                    }
                });
                LinqExt.ForEach<CNPC>((IEnumerable<CNPC>)Collect.cnpcs, (Action<CNPC>)delegate (CNPC x)
                {
                    if (x != null)
                    {
                        x.isActive = false;
                    }
                });
                LinqExt.ForEach<CPlayer>((IEnumerable<CPlayer>)Collect.cplayers, (Action<CPlayer>)delegate (CPlayer x)
                {
                    if (x != null && x.isActive)
                    {
                        Player tPlayer = x.me.TPlayer;
                        tPlayer.statLifeMax -= x.ExtraLife;
                        Player tPlayer2 = x.me.TPlayer;
                        tPlayer2.statManaMax -= x.ExtraMana;
                        NetMessage.SendData(16, -1, -1, (NetworkText)null, x.me.Index, 0f, 0f, 0f, 0, 0, 0);
                        NetMessage.SendData(42, -1, -1, (NetworkText)null, x.me.Index, 0f, 0f, 0f, 0, 0, 0);
                        x.isActive = false;
                    }
                });
                File.WriteAllText(configPath, JsonConvert.SerializeObject((object)config, (Formatting)1));
                TSPlayer.All.SendMessage("挑战模式已取消，您觉得太难了？[操作来自：" + args.Player.Name + "]", new Color(82, 155, 119));
                return;
            }
            config.enableChallenge = true;
            File.WriteAllText(configPath, JsonConvert.SerializeObject((object)config, (Formatting)1));
            Player[] player = Main.player;
            foreach (Player val in player)
            {
                if (val != null && ((Entity)val).active && TShock.Players[((Entity)val).whoAmI].IsLoggedIn)
                {
                    Collect.cplayers[((Entity)val).whoAmI] = new CPlayer(TShock.Players[((Entity)val).whoAmI], tips: true);
                }
            }
            TSPlayer.All.SendMessage("挑战模式启用，祝您愉快。[操作来自：" + args.Player.Name + "]", new Color(255, 82, 165));
        }

        private void Function(CommandArgs args)
        {
            if (!config.enableChallenge)
            {
                args.Player.SendInfoMessage("未启用挑战模式！");
                return;
            }
            if (!args.Player.Active)
            {
                args.Player.SendInfoMessage("请在游戏里使用该指令");
                return;
            }
            try
            {
                Item[] armor = args.Player.TPlayer.armor;
                if ((armor[1].type == 551 || armor[1].type == 4900) && (armor[2].type == 552 || armor[2].type == 4901) && (armor[0].type == 559 || armor[0].type == 553 || armor[0].type == 558 || armor[0].type == 4873 || armor[0].type == 4896 || armor[0].type == 4897 || armor[0].type == 4898 || armor[0].type == 4899))
                {
                    Collect.cplayers[args.Player.Index].HallowedArmorState = !Collect.cplayers[args.Player.Index].HallowedArmorState;
                    if (Collect.cplayers[args.Player.Index].HallowedArmorState)
                    {
                        SendPlayerText(args.Player, "神圣剑辉已启用", new Color(255, 255, 0), ((Entity)args.Player.TPlayer).Center);
                        args.Player.SendMessage("神圣剑辉已启用", new Color(255, 255, 0));
                    }
                    else
                    {
                        SendPlayerText(args.Player, "永夜剑辉已启用", new Color(255, 0, 255), ((Entity)args.Player.TPlayer).Center);
                        args.Player.SendMessage("永夜剑辉已启用", new Color(255, 0, 255));
                    }
                    return;
                }
                for (int i = 3; i < 10; i++)
                {
                    switch (armor[i].type)
                    {
                        case 3223:
                            {
                                NPC[] array = NearAllHostileNPCs(((Entity)args.Player.TPlayer).Center, 360000f);
                                int num2 = 0;
                                NPC[] array2 = array;
                                foreach (NPC val in array2)
                                {
                                    if (((Entity)val).active)
                                    {
                                        num2++;
                                        NetMessage.SendData(53, -1, -1, (NetworkText)null, ((Entity)val).whoAmI, 31f, 300f, 0f, 0, 0, 0);
                                    }
                                }
                                TSPlayer.All.SendMessage(args.Player.Name + " 发动了混乱之脑迷惑，成功迷惑了附近 " + num2 + "个敌人", new Color(241, 108, 108));
                                return;
                            }
                        case 5113:
                            {
                                int num = Main.rand.Next(Main.hardMode ? 19 : 23);
                                if (num >= 6 && num <= 10 && !Main.hardMode)
                                {
                                    num = 15;
                                }
                                switch (num)
                                {
                                    case 0:
                                    case 11:
                                        Main.StartRain();
                                        TSPlayer.All.SendMessage(args.Player.Name + " 收听了 " + Main.worldName + " 天气预报收音广播：即将下雨", new Color(167, 218, 251));
                                        TSPlayer.All.SendData((PacketTypes)7, "", 0, 0f, 0f, 0f, 0);
                                        break;
                                    case 1:
                                    case 12:
                                        Main.StopRain();
                                        TSPlayer.All.SendMessage(args.Player.Name + " 收听了 " + Main.worldName + " 天气预报收音广播：不会下雨", new Color(167, 218, 251));
                                        TSPlayer.All.SendData((PacketTypes)7, "", 0, 0f, 0f, 0f, 0);
                                        break;
                                    case 2:
                                    case 13:
                                        Main.windSpeedTarget = 0f;
                                        Main.windSpeedCurrent = 0f;
                                        TSPlayer.All.SendMessage(args.Player.Name + " 收听了 " + Main.worldName + " 天气预报收音广播：不会有风", new Color(167, 218, 251));
                                        TSPlayer.All.SendData((PacketTypes)7, "", 0, 0f, 0f, 0f, 0);
                                        break;
                                    case 3:
                                    case 14:
                                        Main.windSpeedTarget = 1f;
                                        Main.windSpeedCurrent = 1f;
                                        TSPlayer.All.SendMessage(args.Player.Name + " 收听了 " + Main.worldName + " 天气预报收音广播：即将挂起风", new Color(167, 218, 251));
                                        TSPlayer.All.SendData((PacketTypes)7, "", 0, 0f, 0f, 0f, 0);
                                        break;
                                    case 4:
                                        Main.windSpeedTarget = 2f;
                                        Main.windSpeedCurrent = 2f;
                                        TSPlayer.All.SendMessage(args.Player.Name + " 收听了 " + Main.worldName + " 天气预报收音广播：即将挂起狂风", new Color(167, 218, 251));
                                        TSPlayer.All.SendData((PacketTypes)7, "", 0, 0f, 0f, 0f, 0);
                                        break;
                                    case 5:
                                        if (Sandstorm.Happening)
                                        {
                                            Sandstorm.StopSandstorm();
                                            TSPlayer.All.SendMessage(args.Player.Name + " 收听了 " + Main.worldName + " 天气预报收音广播：不会有沙尘暴", new Color(167, 218, 251));
                                        }
                                        else
                                        {
                                            Sandstorm.StartSandstorm();
                                            TSPlayer.All.SendMessage(args.Player.Name + " 收听了 " + Main.worldName + " 天气预报收音广播：即将刮起沙尘暴", new Color(167, 218, 251));
                                        }
                                        break;
                                    case 6:
                                        Collect.worldevent = 1;
                                        TSPlayer.All.SendMessage($"{args.Player.Name} 收听了 {Main.worldName} 世界先知广播：{(Main.dayTime ? "今晚" : "明晚")}满月", new Color(72, 182, 252));
                                        break;
                                    case 7:
                                        Collect.worldevent = 2;
                                        TSPlayer.All.SendMessage($"{args.Player.Name} 收听了 {Main.worldName} 世界先知广播：{(Main.dayTime ? "今晚" : "明晚")}血月", new Color(72, 182, 252));
                                        break;
                                    case 8:
                                        Collect.worldevent = 3;
                                        TSPlayer.All.SendMessage(args.Player.Name + " 收听了 " + Main.worldName + " 世界先知广播：明天日食", new Color(72, 182, 252));
                                        break;
                                    case 9:
                                        Collect.worldevent = 4;
                                        TSPlayer.All.SendMessage($"{args.Player.Name} 收听了 {Main.worldName} 世界先知广播：{(Main.dayTime ? "今晚" : "明晚")}灯笼夜", new Color(72, 182, 252));
                                        break;
                                    case 10:
                                        Collect.worldevent = 5;
                                        TSPlayer.All.SendMessage($"{args.Player.Name} 收听了 {Main.worldName} 世界先知广播：{(Main.dayTime ? "今晚" : "明晚")}有流星", new Color(72, 182, 252));
                                        break;
                                    default:
                                        Collect.worldevent = 0;
                                        TSPlayer.All.SendMessage(args.Player.Name + " 收听了 " + Main.worldName + " 天气预报收音广播：顺其自然，不会发生任何事件", new Color(167, 218, 251));
                                        break;
                                }
                                return;
                            }
                    }
                }
                args.Player.SendInfoMessage("没有套装效果启用");
            }
            catch (Exception ex)
            {
                args.Player.SendInfoMessage("状态异常，使用失败: " + ex.ToString());
                Console.WriteLine(ex.ToString());
                TShock.Log.Error(ex.ToString());
            }
        }

        public static NPC? NearestHostileNPC(Vector2 pos, float distanceSquared)
        {
            NPC result = null;
            NPC[] npc = Main.npc;
            foreach (NPC val in npc)
            {
                if (((Entity)val).active && val.CanBeChasedBy((object)null, false))
                {
                    float num = distanceSquared;
                    Vector2 val2 = ((Entity)val).Center - pos;
                    if (num > ((Vector2)(val2)).LengthSquared())
                    {
                        val2 = ((Entity)val).Center - pos;
                        distanceSquared = ((Vector2)(val2)).LengthSquared();
                        result = val;
                    }
                }
            }
            return result;
        }

        public static NPC? NearestWeakestNPC(Vector2 pos, float distanceSquared)
        {
            NPC val = null;
            bool flag = false;
            float num = 0f;
            float num2 = 1000f;
            float num3 = distanceSquared;
            int num4 = -1;
            NPC[] npc = Main.npc;
            foreach (NPC val2 in npc)
            {
                Vector2 val3 = ((Entity)val2).Center - pos;
                float num5 = ((Vector2)(val3)).LengthSquared();
                if (val2.boss && ((Entity)val2).active && distanceSquared > num5 && val2.value >= num)
                {
                    num = val2.value;
                    val = val2;
                    flag = true;
                }
                if (((Entity)val2).active && !flag && val2.CanBeChasedBy((object)null, false) && distanceSquared > num5 && (float)val2.life * 1f / (float)val2.lifeMax <= num2)
                {
                    if (val2.lifeMax - val2.life > 1)
                    {
                        num2 = (float)val2.life * 1f / (float)val2.lifeMax;
                        val = val2;
                    }
                    else if (num3 > num5)
                    {
                        num3 = num5;
                        num4 = ((Entity)val2).whoAmI;
                    }
                }
            }
            if (val == null && num4 != -1)
            {
                val = Main.npc[num4];
            }
            return val;
        }

        public static NPC[] NearAllHostileNPCs(Vector2 pos, float distanceSquared)
        {
            List<NPC> list = new List<NPC>();
            NPC[] npc = Main.npc;
            foreach (NPC val in npc)
            {
                if (((Entity)val).active && val.CanBeChasedBy(null, false))
                {
                    Vector2 val2 = ((Entity)val).Center - pos;
                    if (distanceSquared > ((Vector2)(val2)).LengthSquared())
                    {
                        list.Add(val);
                    }
                }
            }
            return list.ToArray();
        }

        public static Player? NearWeakestPlayer(Vector2 pos, float distanceSquared, Player? dontHealPlayer = null)
        {
            Player result = null;
            int num = 0;
            Player[] player = Main.player;
            foreach (Player val in player)
            {
                if (!val.dead)
                {
                    Vector2 val2 = ((Entity)val).Center - pos;
                    if (((Vector2)(val2)).LengthSquared() < distanceSquared && val.statLifeMax - val.statLife > num && (dontHealPlayer == null || ((Entity)dontHealPlayer).whoAmI != ((Entity)val).whoAmI))
                    {
                        result = val;
                        num = val.statLifeMax - val.statLife;
                    }
                }
            }
            return result;
        }

        public static void HealPlayer(Player player, int num, bool visible = true)
        {
            player.statLife += num;
            if (visible)
            {
                Rectangle val = default(Rectangle);
                Rectangle r = new Rectangle((int)player.position.X, (int)player.position.Y, player.width, player.height);
                CombatText.NewText(val, CombatText.HealLife, num, false, false);
                Color healLife = CombatText.HealLife;
                NetMessage.SendData(81, -1, -1, null, (int)((Color)(healLife)).PackedValue, (float)((Rectangle)(val)).Center.X, (float)((Rectangle)(val)).Center.Y, (float)num, 0, 0, 0);
            }
            NetMessage.SendData(16, -1, -1, NetworkText.Empty, ((Entity)player).whoAmI, 0f, 0f, 0f, 0, 0, 0);
        }

        public static void HealPlayerMana(Player player, int num, bool visible = true)
        {
            player.statMana += num;
            if (visible)
            {
                Rectangle val = default(Rectangle);
                Rectangle r = new Rectangle((int)player.position.X, (int)player.position.Y, player.width, player.height);
                CombatText.NewText(val, CombatText.HealLife, num, false, false);
                Color healMana = CombatText.HealMana;
                NetMessage.SendData(81, -1, -1, (NetworkText)null, (int)((Color)(healMana)).PackedValue, (float)((Rectangle)(val)).Center.X, (float)((Rectangle)(val)).Center.Y, (float)num, 0, 0, 0);
            }
            NetMessage.SendData(42, -1, -1, NetworkText.Empty, ((Entity)player).whoAmI, 0f, 0f, 0f, 0, 0, 0);
        }

        public static void SendPlayerText(TSPlayer player, string text, Color color, Vector2 position)
        {
            player.SendData((PacketTypes)119, text, (int)color.packedValue, position.X, position.Y, 0f, 0);
        }

        public static void SendPlayerText(string text, Color color, Vector2 position)
        {
            TSPlayer.All.SendData((PacketTypes)119, text, (int)color.packedValue, position.X, position.Y, 0f, 0);
        }

        public static void SendPlayerText(TSPlayer player, int text, Color color, Vector2 position)
        {
            player.SendData((PacketTypes)81, (string)null, (int)color.packedValue, position.X, position.Y, (float)text, 0);
        }

        public static void SendPlayerText(int text, Color color, Vector2 position)
        {
            TSPlayer.All.SendData((PacketTypes)81, (string)null, (int)color.packedValue, position.X, position.Y, (float)text, 0);
        }
    }
}