// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.LFG;

internal class LFGRoleCheckUpdate : ServerPacket
{
    public List<ulong> BgQueueIDs = new();
    public int GroupFinderActivityID = 0;
    public bool IsBeginning;
    public bool IsRequeue;
    public List<uint> JoinSlots = new();
    public List<LFGRoleCheckUpdateMember> Members = new();
    public byte PartyIndex;
    public byte RoleCheckStatus;
    public LFGRoleCheckUpdate() : base(ServerOpcodes.LfgRoleCheckUpdate) { }

    public override void Write()
    {
        WorldPacket.WriteUInt8(PartyIndex);
        WorldPacket.WriteUInt8(RoleCheckStatus);
        WorldPacket.WriteInt32(JoinSlots.Count);
        WorldPacket.WriteInt32(BgQueueIDs.Count);
        WorldPacket.WriteInt32(GroupFinderActivityID);
        WorldPacket.WriteInt32(Members.Count);

        foreach (var slot in JoinSlots)
            WorldPacket.WriteUInt32(slot);

        foreach (var bgQueueID in BgQueueIDs)
            WorldPacket.WriteUInt64(bgQueueID);

        WorldPacket.WriteBit(IsBeginning);
        WorldPacket.WriteBit(IsRequeue);
        WorldPacket.FlushBits();

        foreach (var member in Members)
            member.Write(WorldPacket);
    }
}