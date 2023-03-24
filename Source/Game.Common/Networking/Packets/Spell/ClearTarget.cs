// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Common.Entities.Objects;

namespace Game.Common.Networking.Packets.Spell;

public class ClearTarget : ServerPacket
{
	public ObjectGuid Guid;
	public ClearTarget() : base(ServerOpcodes.ClearTarget) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Guid);
	}
}
