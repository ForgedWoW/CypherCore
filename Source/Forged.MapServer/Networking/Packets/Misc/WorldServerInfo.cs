// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Misc;

public class WorldServerInfo : ServerPacket
{
    public bool BlockExitingLoadingScreen;
    public uint DifficultyID;
    public uint? InstanceGroupSize;

    public bool IsTournamentRealm;

    // instead it will be done after this packet is sent again with false in this bit and SMSG_UPDATE_OBJECT Values for player
    public uint? RestrictedAccountMaxLevel;

    // when set to true, sending SMSG_UPDATE_OBJECT with CreateObject Self bit = true will not hide loading screen
    public ulong? RestrictedAccountMaxMoney;

    public bool XRealmPvpAlert;

    public WorldServerInfo() : base(ServerOpcodes.WorldServerInfo, ConnectionType.Instance)
    {
        InstanceGroupSize = new uint?();

        RestrictedAccountMaxLevel = new uint?();
        RestrictedAccountMaxMoney = new ulong?();
    }

    public override void Write()
    {
        WorldPacket.WriteUInt32(DifficultyID);
        WorldPacket.WriteBit(IsTournamentRealm);
        WorldPacket.WriteBit(XRealmPvpAlert);
        WorldPacket.WriteBit(BlockExitingLoadingScreen);
        WorldPacket.WriteBit(RestrictedAccountMaxLevel.HasValue);
        WorldPacket.WriteBit(RestrictedAccountMaxMoney.HasValue);
        WorldPacket.WriteBit(InstanceGroupSize.HasValue);
        WorldPacket.FlushBits();

        if (RestrictedAccountMaxLevel.HasValue)
            WorldPacket.WriteUInt32(RestrictedAccountMaxLevel.Value);

        if (RestrictedAccountMaxMoney.HasValue)
            WorldPacket.WriteUInt64(RestrictedAccountMaxMoney.Value);

        if (InstanceGroupSize.HasValue)
            WorldPacket.WriteUInt32(InstanceGroupSize.Value);
    }
}