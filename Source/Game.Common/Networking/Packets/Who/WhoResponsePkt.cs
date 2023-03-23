// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Common.Networking;
using Game.Common.Networking.Packets.Who;

namespace Game.Common.Networking.Packets.Who;

public class WhoResponsePkt : ServerPacket
{
	public uint RequestID;
	public List<WhoEntry> Response = new();
	public WhoResponsePkt() : base(ServerOpcodes.Who) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(RequestID);
		_worldPacket.WriteBits(Response.Count, 6);
		_worldPacket.FlushBits();

		Response.ForEach(p => p.Write(_worldPacket));
	}
}
