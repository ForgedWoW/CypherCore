// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Networking.Packets;

public class GuildEventDisbanded : ServerPacket
{
	public GuildEventDisbanded() : base(ServerOpcodes.GuildEventDisbanded) { }

	public override void Write() { }
}