// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Game.Networking.Packets;

public class SetSpellModifier : ServerPacket
{
	public List<SpellModifierInfo> Modifiers = new();
	public SetSpellModifier(ServerOpcodes opcode) : base(opcode, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(Modifiers.Count);

		foreach (var spellMod in Modifiers)
			spellMod.Write(_worldPacket);
	}
}