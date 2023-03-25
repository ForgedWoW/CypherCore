// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Channel;

class ChannelPassword : ClientPacket
{
	public string ChannelName;
	public string Password;
	public ChannelPassword(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		var channelNameLength = _worldPacket.ReadBits<uint>(7);
		var passwordLength = _worldPacket.ReadBits<uint>(7);
		ChannelName = _worldPacket.ReadString(channelNameLength);
		Password = _worldPacket.ReadString(passwordLength);
	}
}