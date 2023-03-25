// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Spell;

public class SendSpellHistory : ServerPacket
{
	public List<SpellHistoryEntry> Entries = new();
	public SendSpellHistory() : base(ServerOpcodes.SendSpellHistory, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(Entries.Count);
		Entries.ForEach(p => p.Write(_worldPacket));
	}
}