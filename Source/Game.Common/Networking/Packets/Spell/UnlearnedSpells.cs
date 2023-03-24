// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Game.Common.Networking.Packets.Spell;

public class UnlearnedSpells : ServerPacket
{
	public List<uint> SpellID = new();
	public bool SuppressMessaging;
	public UnlearnedSpells() : base(ServerOpcodes.UnlearnedSpells, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(SpellID.Count);

		foreach (var spellId in SpellID)
			_worldPacket.WriteUInt32(spellId);

		_worldPacket.WriteBit(SuppressMessaging);
		_worldPacket.FlushBits();
	}
}
