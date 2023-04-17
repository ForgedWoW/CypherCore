// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.LFG;

public class LfgBootInfo
{
    public uint BootVotes;
    public bool MyVote;
    public bool MyVoteCompleted;
    public string Reason = "";
    public ObjectGuid Target;
    public uint TimeLeft;
    public uint TotalVotes;
    public bool VoteInProgress;
    public bool VotePassed;
    public uint VotesNeeded;

    public void Write(WorldPacket data)
    {
        data.WriteBit(VoteInProgress);
        data.WriteBit(VotePassed);
        data.WriteBit(MyVoteCompleted);
        data.WriteBit(MyVote);
        data.WriteBits(Reason.GetByteCount(), 8);
        data.WritePackedGuid(Target);
        data.WriteUInt32(TotalVotes);
        data.WriteUInt32(BootVotes);
        data.WriteUInt32(TimeLeft);
        data.WriteUInt32(VotesNeeded);
        data.WriteString(Reason);
    }
}