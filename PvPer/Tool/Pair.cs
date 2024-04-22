using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using TShockAPI;

namespace PvPer
{
    public class Pair
    {
        public int Player1, Player2;
        public static Configuration Config = new Configuration();

        public Pair(int player1, int player2)
        {
            Player1 = player1;
            Player2 = player2;
        }

        public override bool Equals(object? obj)
        {
            if (obj == null)
            {
                return false;
            }

            Pair other = (Pair)obj;

            return Player1 == other.Player1 && Player2 == other.Player2 || Player1 == other.Player2 && Player2 == other.Player1;
        }

        public override int GetHashCode()
        {
            return Player1 << 16 | Player2;
        }

        public void StartDuel()
        {
            TSPlayer plr1 = TShock.Players[Player1];
            TSPlayer plr2 = TShock.Players[Player2];

            if (plr1 != null && plr2 != null)
            {
                if (!plr1.Active || !plr2.Active)
                {
                    plr1.SendErrorMessage("�����ѱ�ȡ������Ϊ����һ�������߲����ߡ�");
                    plr2.SendErrorMessage("�����ѱ�ȡ������Ϊ����һ�������߲����ߡ�");
                    PvPer.Invitations.Remove(this);
                    return;
                }

                if (plr1.Dead || plr2.Dead)
                {
                    plr1.SendErrorMessage("�����ѱ�ȡ������Ϊ����һ���������Ѿ�������");
                    plr2.SendErrorMessage("�����ѱ�ȡ������Ϊ����һ���������Ѿ�������");
                    PvPer.Invitations.Remove(this);
                    return;
                }

                if (Utils.IsPlayerInADuel(plr1.Index) || Utils.IsPlayerInADuel(plr2.Index))
                {
                    plr1.SendErrorMessage("�����ѱ�ȡ������Ϊ����һ���������Ѵ�����һ�������С�");
                    plr2.SendErrorMessage("�����ѱ�ȡ������Ϊ����һ���������Ѵ�����һ�������С�");
                    PvPer.Invitations.Remove(this);
                    return;
                }
            }
            else
            {
                PvPer.Invitations.Remove(this);
                return;
            }

            plr1.SendSuccessMessage($"������ʼ��");
            plr2.SendSuccessMessage($"������ʼ��");

            // ������Һ�����BUFF
            plr1.Teleport(PvPer.Config.Player1PositionX * 16, PvPer.Config.Player1PositionY * 16);
            plr2.Teleport(PvPer.Config.Player2PositionX * 16, PvPer.Config.Player2PositionY * 16);

            plr1.SetBuff(BuffID.Webbed, 60 * 6);
            plr2.SetBuff(BuffID.Webbed, 60 * 6);

            plr1.SetPvP(false);
            plr2.SetPvP(false);
            plr1.SetTeam(0);
            plr2.SetTeam(0);
            plr1.Heal();
            plr2.Heal();
            plr1.SendData(PacketTypes.PlayerDodge, number: plr1.Index, number2: 6);
            plr2.SendData(PacketTypes.PlayerDodge, number: plr1.Index, number2: 6);

            // ����Ծ����������Ծ�����б�
            PvPer.Invitations.Remove(this);
            PvPer.ActiveDuels.Add(this);

            // ��ʱ������Ϊÿλ�������PvPģʽ
            Task.Run(async () =>
            {
                NetMessage.SendData((int)PacketTypes.CreateCombatTextExtended, Player1, -1,
                    Terraria.Localization.NetworkText.FromLiteral("����������ʼ..."), (int)new Color(0, 255, 0).PackedValue,
                    plr1.X + 16, plr1.Y - 16);

                NetMessage.SendData((int)PacketTypes.CreateCombatTextExtended, Player2, -1,
                    Terraria.Localization.NetworkText.FromLiteral("����������ʼ..."), (int)new Color(0, 255, 0).PackedValue,
                    plr2.X + 16, plr2.Y - 16);

                for (int i = 5; i > 0; i--)
                {
                    await Task.Delay(1000);
                    NetMessage.SendData((int)PacketTypes.CreateCombatTextExtended, Player1, -1,
                        Terraria.Localization.NetworkText.FromLiteral(i.ToString()), (int)new Color(255 - i * 50, i * 50, 0).PackedValue,
                        plr1.X + 16, plr1.Y - 16);

                    NetMessage.SendData((int)PacketTypes.CreateCombatTextExtended, Player2, -1,
                        Terraria.Localization.NetworkText.FromLiteral(i.ToString()), (int)new Color(255 - i * 50, i * 50, 0).PackedValue,
                        plr2.X + 16, plr2.Y - 16);
                }

                await Task.Delay(1000);

                NetMessage.SendData((int)PacketTypes.CreateCombatTextExtended, Player1, -1,
                    Terraria.Localization.NetworkText.FromLiteral("��ս!!"), (int)new Color(255, 0, 0).PackedValue,
                    plr1.X + 16, plr1.Y - 16);

                NetMessage.SendData((int)PacketTypes.CreateCombatTextExtended, Player2, -1,
                    Terraria.Localization.NetworkText.FromLiteral("��ս!!"), (int)new Color(255, 0, 0).PackedValue,
                    plr2.X + 16, plr2.Y - 16);

                plr1.TPlayer.hostile = true;
                plr2.TPlayer.hostile = true;
                NetMessage.SendData((int)PacketTypes.TogglePvp, Player1, -1, Terraria.Localization.NetworkText.Empty, Player1);
                NetMessage.SendData((int)PacketTypes.TogglePvp, Player1, -1, Terraria.Localization.NetworkText.Empty, Player2);
                NetMessage.SendData((int)PacketTypes.TogglePvp, Player2, -1, Terraria.Localization.NetworkText.Empty, Player1);
                NetMessage.SendData((int)PacketTypes.TogglePvp, Player2, -1, Terraria.Localization.NetworkText.Empty, Player2);
            });
        }

        // ����������ķ���
        public void EndDuel(int winner)
        {
            int loser = winner == Player1 ? Player2 : Player1;
            string msg = DeathMessages.GetMessage(TShock.Players[winner].Name, TShock.Players[loser].Name);
            TSPlayer.All.SendMessage(msg, 255, 204, 255);

            PvPer.ActiveDuels.Remove(this);
            TShock.Players[winner].SetPvP(false);
            TShock.Players[loser].SetPvP(false);

            // ����Ӯ�����ݲ�������ʤ����
            SavePlayersData(winner);
            // ������ҵ���ʤ����Ϊ0
            ResetLoserWinStreak(loser);
            // ����Ӯ����ʤ����
            DPlayer winnerData = PvPer.DbManager.GetDPlayer(TShock.Players[winner].Account.ID);
            winnerData.WinStreak++; // ����Ӯ����ʤ����
            PvPer.DbManager.SavePlayer(winnerData); // ������º��Ӯ������

            int winStreak = winnerData.WinStreak;// ֱ��ʹ�ø��º��Ӯ����ʤ����
            TSPlayer.All.SendMessage($"{TShock.Players[winner].Name} �Ѿ���ʤ {winStreak} ������!", 255, 255, 90);

            int p = Projectile.NewProjectile(Projectile.GetNoneSource(), TShock.Players[winner].TPlayer.position.X + 16,
            TShock.Players[winner].TPlayer.position.Y - 64f, 0f, -8f, ProjectileID.RocketFireworkGreen, 0, 0);
            Main.projectile[p].Kill();

        }

        // ������ҵ���ʤ����Ϊ0
        private void ResetLoserWinStreak(int loser)
        {
            DPlayer playerData = PvPer.DbManager.GetDPlayer(TShock.Players[loser].Account.ID);
            playerData.WinStreak = 0; // WinStreak�����Դ洢�����ʤ����
            PvPer.DbManager.SavePlayer(playerData); // ������º���������
        }

        //�洢���ʤ��ֵ����
        public void SavePlayersData(int winnerIndex)
        {
            DPlayer plr1, plr2;
            try
            {
                plr1 = PvPer.DbManager.GetDPlayer(TShock.Players[Player1].Account.ID);
            }
            catch (NullReferenceException)
            {
                PvPer.DbManager.InsertPlayer(TShock.Players[Player1].Account.ID, 0, 0);
                plr1 = PvPer.DbManager.GetDPlayer(TShock.Players[Player1].Account.ID);
            }
            try
            {
                plr2 = PvPer.DbManager.GetDPlayer(TShock.Players[Player2].Account.ID);
            }
            catch (NullReferenceException)
            {
                PvPer.DbManager.InsertPlayer(TShock.Players[Player2].Account.ID, 0, 0);
                plr2 = PvPer.DbManager.GetDPlayer(TShock.Players[Player2].Account.ID);
            }

            if (winnerIndex == Player1)
            {
                plr1.Kills++;
                plr2.Deaths++;
                PvPer.DbManager.SavePlayer(plr1);
                PvPer.DbManager.SavePlayer(plr2);
            }
            else
            {
                plr2.Kills++;
                plr1.Deaths++;
                PvPer.DbManager.SavePlayer(plr1);
                PvPer.DbManager.SavePlayer(plr2);
            }
        }
    }
}