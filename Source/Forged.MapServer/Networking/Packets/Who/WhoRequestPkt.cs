// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.Networking.Packets.Who;

public class WhoRequestPkt : ClientPacket
{
	public WhoRequest Request = new();
	public uint RequestID;
	public List<int> Areas = new();
	public WhoRequestPkt(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		var areasCount = _worldPacket.ReadBits<uint>(4);

		Request.Read(_worldPacket);
		RequestID = _worldPacket.ReadUInt32();

		for (var i = 0; i < areasCount; ++i)
			Areas.Add(_worldPacket.ReadInt32());
	}
}