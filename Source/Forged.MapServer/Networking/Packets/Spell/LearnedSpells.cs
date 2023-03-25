// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Spell;

public class LearnedSpells : ServerPacket
{
	public List<LearnedSpellInfo> ClientLearnedSpellData = new();
	public uint SpecializationID;
	public bool SuppressMessaging;
	public LearnedSpells() : base(ServerOpcodes.LearnedSpells, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(ClientLearnedSpellData.Count);
		_worldPacket.WriteUInt32(SpecializationID);
		_worldPacket.WriteBit(SuppressMessaging);
		_worldPacket.FlushBits();

		foreach (var spell in ClientLearnedSpellData)
			spell.Write(_worldPacket);
	}
}