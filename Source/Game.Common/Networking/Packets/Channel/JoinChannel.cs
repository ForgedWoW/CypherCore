// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Networking.Packets.Channel;

public class JoinChannel : ClientPacket
{
	public string Password;
	public string ChannelName;
	public bool CreateVoiceSession;
	public int ChatChannelId;
	public bool Internal;
	public JoinChannel(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		ChatChannelId = _worldPacket.ReadInt32();
		CreateVoiceSession = _worldPacket.HasBit();
		Internal = _worldPacket.HasBit();
		var channelLength = _worldPacket.ReadBits<uint>(7);
		var passwordLength = _worldPacket.ReadBits<uint>(7);
		ChannelName = _worldPacket.ReadString(channelLength);
		Password = _worldPacket.ReadString(passwordLength);
	}
}
