// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Networking.Packets;

public struct AuthWaitInfo
{
	public void Write(WorldPacket data)
	{
		data.WriteUInt32(WaitCount);
		data.WriteUInt32(WaitTime);
		data.WriteBit(HasFCM);
		data.FlushBits();
	}

	public uint WaitCount; // position of the account in the login queue
	public uint WaitTime;  // Wait time in login queue in minutes, if sent queued and this value is 0 client displays "unknown time"
	public bool HasFCM;    // true if the account has a forced character migration pending. @todo implement
}