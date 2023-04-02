// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Guild;

public class GuildSetRankPermissions : ClientPacket
{
    public uint Flags;
    public uint OldFlags;
    public byte RankID;
    public string RankName;
    public int RankOrder;
    public uint[] TabFlags = new uint[GuildConst.MaxBankTabs];
    public uint[] TabWithdrawItemLimit = new uint[GuildConst.MaxBankTabs];
    public uint WithdrawGoldLimit;
    public GuildSetRankPermissions(WorldPacket packet) : base(packet) { }

    public override void Read()
    {
        RankID = WorldPacket.ReadUInt8();
        RankOrder = WorldPacket.ReadInt32();
        Flags = WorldPacket.ReadUInt32();
        WithdrawGoldLimit = WorldPacket.ReadUInt32();

        for (byte i = 0; i < GuildConst.MaxBankTabs; i++)
        {
            TabFlags[i] = WorldPacket.ReadUInt32();
            TabWithdrawItemLimit[i] = WorldPacket.ReadUInt32();
        }

        WorldPacket.ResetBitPos();
        var rankNameLen = WorldPacket.ReadBits<uint>(7);

        RankName = WorldPacket.ReadString(rankNameLen);

        OldFlags = WorldPacket.ReadUInt32();
    }
}