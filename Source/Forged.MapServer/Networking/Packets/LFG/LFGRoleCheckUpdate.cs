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
        _worldPacket.WriteUInt8(PartyIndex);
        _worldPacket.WriteUInt8(RoleCheckStatus);
        _worldPacket.WriteInt32(JoinSlots.Count);
        _worldPacket.WriteInt32(BgQueueIDs.Count);
        _worldPacket.WriteInt32(GroupFinderActivityID);
        _worldPacket.WriteInt32(Members.Count);

        foreach (var slot in JoinSlots)
            _worldPacket.WriteUInt32(slot);

        foreach (var bgQueueID in BgQueueIDs)
            _worldPacket.WriteUInt64(bgQueueID);

        _worldPacket.WriteBit(IsBeginning);
        _worldPacket.WriteBit(IsRequeue);
        _worldPacket.FlushBits();

        foreach (var member in Members)
            member.Write(_worldPacket);
    }
}