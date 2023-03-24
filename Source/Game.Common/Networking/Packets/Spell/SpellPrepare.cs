// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Common.Entities.Objects;

namespace Game.Common.Networking.Packets.Spell;

public class SpellPrepare : ServerPacket
{
	public ObjectGuid ClientCastID;
	public ObjectGuid ServerCastID;
	public SpellPrepare() : base(ServerOpcodes.SpellPrepare) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(ClientCastID);
		_worldPacket.WritePackedGuid(ServerCastID);
	}
}
