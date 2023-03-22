﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Networking.Packets;

public class GuildPartyState : ServerPacket
{
	public float GuildXPEarnedMult = 0.0f;
	public int NumMembers;
	public int NumRequired;
	public bool InGuildParty;
	public GuildPartyState() : base(ServerOpcodes.GuildPartyState, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteBit(InGuildParty);
		_worldPacket.FlushBits();

		_worldPacket.WriteInt32(NumMembers);
		_worldPacket.WriteInt32(NumRequired);
		_worldPacket.WriteFloat(GuildXPEarnedMult);
	}
}