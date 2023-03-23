// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Networking.Packets.Social;

public class AddFriend : ClientPacket
{
	public string Notes;
	public string Name;
	public AddFriend(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		var nameLength = _worldPacket.ReadBits<uint>(9);
		var noteslength = _worldPacket.ReadBits<uint>(10);
		Name = _worldPacket.ReadString(nameLength);
		Notes = _worldPacket.ReadString(noteslength);
	}
}
