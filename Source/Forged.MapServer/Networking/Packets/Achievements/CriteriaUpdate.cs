// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Achievements;

public class CriteriaUpdate : ServerPacket
{
    public long CreationTime;
    public uint CriteriaID;
    public long CurrentTime;
    public long ElapsedTime;
    public uint Flags;
    public ObjectGuid PlayerGUID;
    public ulong Quantity;
    public ulong? RafAcceptanceID;
    public uint Unused_10_1_5;
    public CriteriaUpdate() : base(ServerOpcodes.CriteriaUpdate, ConnectionType.Instance) { }

    public override void Write()
    {
        WorldPacket.WriteUInt32(CriteriaID);
        WorldPacket.WriteUInt64(Quantity);
        WorldPacket.WritePackedGuid(PlayerGUID);
        WorldPacket.WriteUInt32(Unused_10_1_5);
        WorldPacket.WriteUInt32(Flags);
        WorldPacket.WritePackedTime(CurrentTime);
        WorldPacket.WriteInt64(ElapsedTime);
        WorldPacket.WriteInt64(CreationTime);
        WorldPacket.WriteBit(RafAcceptanceID.HasValue);
        WorldPacket.FlushBits();

        if (RafAcceptanceID.HasValue)
            WorldPacket.WriteUInt64(RafAcceptanceID.Value);
    }
}