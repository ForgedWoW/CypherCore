// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Common.Entities.Objects;
using Game.Common.Networking;
using Game.Common.Networking.Packets.CombatLog;

namespace Game.Common.Networking.Packets.CombatLog;

public class SpellDispellLog : ServerPacket
{
	public List<SpellDispellData> DispellData = new();
	public ObjectGuid CasterGUID;
	public ObjectGuid TargetGUID;
	public uint DispelledBySpellID;
	public bool IsBreak;
	public bool IsSteal;
	public SpellDispellLog() : base(ServerOpcodes.SpellDispellLog, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteBit(IsSteal);
		_worldPacket.WriteBit(IsBreak);
		_worldPacket.WritePackedGuid(TargetGUID);
		_worldPacket.WritePackedGuid(CasterGUID);
		_worldPacket.WriteUInt32(DispelledBySpellID);

		_worldPacket.WriteInt32(DispellData.Count);

		foreach (var data in DispellData)
		{
			_worldPacket.WriteUInt32(data.SpellID);
			_worldPacket.WriteBit(data.Harmful);
			_worldPacket.WriteBit(data.Rolled.HasValue);
			_worldPacket.WriteBit(data.Needed.HasValue);

			if (data.Rolled.HasValue)
				_worldPacket.WriteInt32(data.Rolled.Value);

			if (data.Needed.HasValue)
				_worldPacket.WriteInt32(data.Needed.Value);

			_worldPacket.FlushBits();
		}
	}
}
