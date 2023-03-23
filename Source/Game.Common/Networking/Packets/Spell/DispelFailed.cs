// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Common.Entities.Objects;
using Game.Common.Networking;

namespace Game.Common.Networking.Packets.Spell;

public class DispelFailed : ServerPacket
{
	public ObjectGuid CasterGUID;
	public ObjectGuid VictimGUID;
	public uint SpellID;
	public List<uint> FailedSpells = new();
	public DispelFailed() : base(ServerOpcodes.DispelFailed) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(CasterGUID);
		_worldPacket.WritePackedGuid(VictimGUID);
		_worldPacket.WriteUInt32(SpellID);
		_worldPacket.WriteInt32(FailedSpells.Count);

		FailedSpells.ForEach(FailedSpellID => _worldPacket.WriteUInt32(FailedSpellID));
	}
}
