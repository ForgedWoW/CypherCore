// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Movement;

internal class SummonRequest : ServerPacket
{
    public int AreaID;

    public SummonReason Reason;

    public bool SkipStartingArea;

    public ObjectGuid SummonerGUID;

    public uint SummonerVirtualRealmAddress;

    public SummonRequest() : base(ServerOpcodes.SummonRequest, ConnectionType.Instance) { }

    public enum SummonReason
    {
        Spell = 0,
        Scenario = 1
    }
    public override void Write()
    {
        WorldPacket.WritePackedGuid(SummonerGUID);
        WorldPacket.WriteUInt32(SummonerVirtualRealmAddress);
        WorldPacket.WriteInt32(AreaID);
        WorldPacket.WriteUInt8((byte)Reason);
        WorldPacket.WriteBit(SkipStartingArea);
        WorldPacket.FlushBits();
    }
}