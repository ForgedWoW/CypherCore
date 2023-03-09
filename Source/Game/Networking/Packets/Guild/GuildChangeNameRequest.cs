// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Networking.Packets;

public class GuildChangeNameRequest : ClientPacket
{
	public string NewName;
	public GuildChangeNameRequest(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		var nameLen = _worldPacket.ReadBits<uint>(7);
		NewName = _worldPacket.ReadString(nameLen);
	}
}