// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Common.Entities.Objects;
using Game.Common.Networking;
using Game.Common.Networking.Packets.CombatLog;
using Game.Common.Networking.Packets.Spell;

namespace Game.Common.Networking.Packets.CombatLog;

public class SpellPeriodicAuraLog : CombatLogServerPacket
{
	public ObjectGuid TargetGUID;
	public ObjectGuid CasterGUID;
	public uint SpellID;
	public List<SpellLogEffect> Effects = new();
	public SpellPeriodicAuraLog() : base(ServerOpcodes.SpellPeriodicAuraLog, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(TargetGUID);
		_worldPacket.WritePackedGuid(CasterGUID);
		_worldPacket.WriteUInt32(SpellID);
		_worldPacket.WriteInt32(Effects.Count);
		WriteLogDataBit();
		FlushBits();

		Effects.ForEach(p => p.Write(_worldPacket));

		WriteLogData();
	}

	public struct PeriodicalAuraLogEffectDebugInfo
	{
		public float CritRollMade;
		public float CritRollNeeded;
	}

	public class SpellLogEffect
	{
		public uint Effect;
		public uint Amount;
		public int OriginalDamage;
		public uint OverHealOrKill;
		public uint SchoolMaskOrPower;
		public uint AbsorbedOrAmplitude;
		public uint Resisted;
		public bool Crit;
		public PeriodicalAuraLogEffectDebugInfo? DebugInfo;
		public ContentTuningParams ContentTuning;

		public void Write(WorldPacket data)
		{
			data.WriteUInt32(Effect);
			data.WriteUInt32(Amount);
			data.WriteInt32(OriginalDamage);
			data.WriteUInt32(OverHealOrKill);
			data.WriteUInt32(SchoolMaskOrPower);
			data.WriteUInt32(AbsorbedOrAmplitude);
			data.WriteUInt32(Resisted);

			data.WriteBit(Crit);
			data.WriteBit(DebugInfo.HasValue);
			data.WriteBit(ContentTuning != null);
			data.FlushBits();

			if (ContentTuning != null)
				ContentTuning.Write(data);

			if (DebugInfo.HasValue)
			{
				data.WriteFloat(DebugInfo.Value.CritRollMade);
				data.WriteFloat(DebugInfo.Value.CritRollNeeded);
			}
		}
	}
}
