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
        _worldPacket.WriteUInt32(DifficultyID);
        _worldPacket.WriteBit(IsTournamentRealm);
        _worldPacket.WriteBit(XRealmPvpAlert);
        _worldPacket.WriteBit(BlockExitingLoadingScreen);
        _worldPacket.WriteBit(RestrictedAccountMaxLevel.HasValue);
        _worldPacket.WriteBit(RestrictedAccountMaxMoney.HasValue);
        _worldPacket.WriteBit(InstanceGroupSize.HasValue);
        _worldPacket.FlushBits();

        if (RestrictedAccountMaxLevel.HasValue)
            _worldPacket.WriteUInt32(RestrictedAccountMaxLevel.Value);

        if (RestrictedAccountMaxMoney.HasValue)
            _worldPacket.WriteUInt64(RestrictedAccountMaxMoney.Value);

        if (InstanceGroupSize.HasValue)
            _worldPacket.WriteUInt32(InstanceGroupSize.Value);
    }
}