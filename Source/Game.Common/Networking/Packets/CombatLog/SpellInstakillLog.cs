// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Common.Entities.Objects;

namespace Game.Common.Networking.Packets.CombatLog;

public class SpellInstakillLog : ServerPacket
{
	public ObjectGuid Target;
	public ObjectGuid Caster;
	public uint SpellID;
	public SpellInstakillLog() : base(ServerOpcodes.SpellInstakillLog, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Target);
		_worldPacket.WritePackedGuid(Caster);
		_worldPacket.WriteUInt32(SpellID);
	}
}
