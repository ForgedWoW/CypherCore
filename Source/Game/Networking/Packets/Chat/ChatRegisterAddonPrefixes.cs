// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Networking.Packets;

class ChatRegisterAddonPrefixes : ClientPacket
{
	public string[] Prefixes = new string[64];
	public ChatRegisterAddonPrefixes(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		var count = _worldPacket.ReadInt32();

		for (var i = 0; i < count && i < 64; ++i)
			Prefixes[i] = _worldPacket.ReadString(_worldPacket.ReadBits<uint>(5));
	}
}