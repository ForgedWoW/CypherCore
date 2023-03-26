// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Networking.Packets.CombatLog;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Spell;

internal class SpellGo : CombatLogServerPacket
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