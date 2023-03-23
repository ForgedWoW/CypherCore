// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Common.Networking;

namespace Game.Common.Networking.Packets.Party;

public class GroupNewLeader : ServerPacket
{
	public sbyte PartyIndex;
	public string Name;
	public GroupNewLeader() : base(ServerOpcodes.GroupNewLeader) { }

	public override void Write()
	{
		_worldPacket.WriteInt8(PartyIndex);
		_worldPacket.WriteBits(Name.GetByteCount(), 9);
		_worldPacket.WriteString(Name);
	}
}
