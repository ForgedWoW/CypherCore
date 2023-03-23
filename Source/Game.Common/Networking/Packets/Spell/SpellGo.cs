// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking.Packets.CombatLog;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking.Packets.Spell;

namespace Game.Common.Networking.Packets.Spell;

public class SpellGo : CombatLogServerPacket
{
	public SpellCastData Cast = new();
	public SpellGo() : base(ServerOpcodes.SpellGo, ConnectionType.Instance) { }

	public override void Write()
	{
		Cast.Write(_worldPacket);

		WriteLogDataBit();
		FlushBits();

		WriteLogData();
	}
}
