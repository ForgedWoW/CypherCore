// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Game.Entities;

namespace Game.Networking.Packets;

public class LfgBootInfo
{
	public bool VoteInProgress;
	public bool VotePassed;
	public bool MyVoteCompleted;
	public bool MyVote;
	public ObjectGuid Target;
	public uint TotalVotes;
	public uint BootVotes;
	public uint TimeLeft;
	public uint VotesNeeded;
	public string Reason = "";

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