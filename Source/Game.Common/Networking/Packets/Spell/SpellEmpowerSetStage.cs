// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Common.Entities.Objects;
using Game.Common.Networking;

namespace Game.Common.Networking.Packets.Spell;

public class SpellEmpowerSetStage : ServerPacket
{
	public ObjectGuid CastID;
	public ObjectGuid Caster;
	public uint Stage;
	public SpellEmpowerSetStage() : base(ServerOpcodes.SpellEmpowerSetStage, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(CastID);
		_worldPacket.WritePackedGuid(Caster);
		_worldPacket.Write(Stage);
	}
}
