// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Chat;

internal class ChatRestricted : ServerPacket
{
    private readonly ChatRestrictionType Reason;

	public ChatRestricted(ChatRestrictionType reason) : base(ServerOpcodes.ChatRestricted)
	{
		Reason = reason;
	}

	public override void Write()
	{
		_worldPacket.WriteUInt8((byte)Reason);
	}
}