// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Networking.Packets;

public class GroupDestroyed : ServerPacket
{
	public GroupDestroyed() : base(ServerOpcodes.GroupDestroyed) { }

	public override void Write() { }
}