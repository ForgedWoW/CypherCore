// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Networking.Packets.Chat;

class ChatAddonMessageTargeted : ClientPacket
{
	public string Target;
	public ChatAddonMessageParams Params = new();
	public ObjectGuid? ChannelGUID; // not optional in the packet. Optional for api reasons
	public ChatAddonMessageTargeted(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		var targetLen = _worldPacket.ReadBits<uint>(9);
		Params.Read(_worldPacket);
		ChannelGUID = _worldPacket.ReadPackedGuid();
		Target = _worldPacket.ReadString(targetLen);
	}
}