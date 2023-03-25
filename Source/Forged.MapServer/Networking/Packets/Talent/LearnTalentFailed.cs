// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Talent;

class LearnTalentFailed : ServerPacket
{
	public uint Reason;
	public int SpellID;
	public List<ushort> Talents = new();
	public LearnTalentFailed() : base(ServerOpcodes.LearnTalentFailed) { }

	public override void Write()
	{
		_worldPacket.WriteBits(Reason, 4);
		_worldPacket.WriteInt32(SpellID);
		_worldPacket.WriteInt32(Talents.Count);

		foreach (var talent in Talents)
			_worldPacket.WriteUInt16(talent);
	}
}