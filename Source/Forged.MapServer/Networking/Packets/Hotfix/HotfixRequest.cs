// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Game.Networking.Packets;

class HotfixRequest : ClientPacket
{
	public uint ClientBuild;
	public uint DataBuild;
	public List<int> Hotfixes = new();
	public HotfixRequest(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		ClientBuild = _worldPacket.ReadUInt32();
		DataBuild = _worldPacket.ReadUInt32();

		var hotfixCount = _worldPacket.ReadUInt32();

		for (var i = 0; i < hotfixCount; ++i)
			Hotfixes.Add(_worldPacket.ReadInt32());
	}
}