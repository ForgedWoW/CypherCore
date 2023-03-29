// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.LFG;

public struct LFGProposalUpdatePlayer
{
    public void Write(WorldPacket data)
    {
        data.WriteUInt32(Roles);
        data.WriteBit(Me);
        data.WriteBit(SameParty);
        data.WriteBit(MyParty);
        data.WriteBit(Responded);
        data.WriteBit(Accepted);
        data.FlushBits();
    }

    public uint Roles;
    public bool Me;
    public bool SameParty;
    public bool MyParty;
    public bool Responded;
    public bool Accepted;
}