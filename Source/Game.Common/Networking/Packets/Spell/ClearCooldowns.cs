// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Common.Networking;

namespace Game.Common.Networking.Packets.Spell;

public class ClearCooldowns : ServerPacket
{
	public List<uint> SpellID = new();
	public bool IsPet;
	public ClearCooldowns() : base(ServerOpcodes.ClearCooldowns, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(SpellID.Count);

		if (!SpellID.Empty())
			SpellID.ForEach(p => _worldPacket.WriteUInt32(p));

		_worldPacket.WriteBit(IsPet);
		_worldPacket.FlushBits();
	}
}
