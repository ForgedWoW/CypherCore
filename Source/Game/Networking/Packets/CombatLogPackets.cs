﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Spells;

namespace Game.Networking.Packets;

public class CombatLogServerPacket : ServerPacket
{
	internal SpellCastLogData LogData;
	bool _includeLogData;

	public CombatLogServerPacket(ServerOpcodes opcode, ConnectionType connection = ConnectionType.Realm) : base(opcode, connection)
	{
		LogData = new SpellCastLogData();
	}

	public override void Write() { }

	public void SetAdvancedCombatLogging(bool value)
	{
		_includeLogData = value;
	}

	public void WriteLogDataBit()
	{
		_worldPacket.WriteBit(_includeLogData);
	}

	public void FlushBits()
	{
		_worldPacket.FlushBits();
	}

	public void WriteLogData()
	{
		if (_includeLogData)
			LogData.Write(_worldPacket);
	}
}

class SpellNonMeleeDamageLog : CombatLogServerPacket
{
	public ObjectGuid Me;
	public ObjectGuid CasterGUID;
	public ObjectGuid CastID;
	public int SpellID;
	public SpellCastVisual Visual;
	public int Damage;
	public int OriginalDamage;
	public int Overkill = -1;
	public byte SchoolMask;
	public int ShieldBlock;
	public int Resisted;
	public bool Periodic;
	public int Absorbed;

	public int Flags;

	// Optional<SpellNonMeleeDamageLogDebugInfo> DebugInfo;
	public ContentTuningParams ContentTuning;
	public SpellNonMeleeDamageLog() : base(ServerOpcodes.SpellNonMeleeDamageLog, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Me);
		_worldPacket.WritePackedGuid(CasterGUID);
		_worldPacket.WritePackedGuid(CastID);
		_worldPacket.WriteInt32(SpellID);
		Visual.Write(_worldPacket);
		_worldPacket.WriteInt32(Damage);
		_worldPacket.WriteInt32(OriginalDamage);
		_worldPacket.WriteInt32(Overkill);
		_worldPacket.WriteUInt8(SchoolMask);
		_worldPacket.WriteInt32(Absorbed);
		_worldPacket.WriteInt32(Resisted);
		_worldPacket.WriteInt32(ShieldBlock);

		_worldPacket.WriteBit(Periodic);
		_worldPacket.WriteBits(Flags, 7);
		_worldPacket.WriteBit(false); // Debug info
		WriteLogDataBit();
		_worldPacket.WriteBit(ContentTuning != null);
		FlushBits();
		WriteLogData();

		if (ContentTuning != null)
			ContentTuning.Write(_worldPacket);
	}
}

class EnvironmentalDamageLog : CombatLogServerPacket
{
	public ObjectGuid Victim;
	public EnviromentalDamage Type;
	public int Amount;
	public int Resisted;
	public int Absorbed;
	public EnvironmentalDamageLog() : base(ServerOpcodes.EnvironmentalDamageLog) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Victim);
		_worldPacket.WriteUInt8((byte)Type);
		_worldPacket.WriteInt32(Amount);
		_worldPacket.WriteInt32(Resisted);
		_worldPacket.WriteInt32(Absorbed);

		WriteLogDataBit();
		FlushBits();
		WriteLogData();
	}
}

class SpellExecuteLog : CombatLogServerPacket
{
	public ObjectGuid Caster;
	public uint SpellID;
	public List<SpellLogEffect> Effects = new();
	public SpellExecuteLog() : base(ServerOpcodes.SpellExecuteLog, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Caster);
		_worldPacket.WriteUInt32(SpellID);
		_worldPacket.WriteInt32(Effects.Count);

		foreach (var effect in Effects)
		{
			_worldPacket.WriteInt32(effect.Effect);

			_worldPacket.WriteInt32(effect.PowerDrainTargets.Count);
			_worldPacket.WriteInt32(effect.ExtraAttacksTargets.Count);
			_worldPacket.WriteInt32(effect.DurabilityDamageTargets.Count);
			_worldPacket.WriteInt32(effect.GenericVictimTargets.Count);
			_worldPacket.WriteInt32(effect.TradeSkillTargets.Count);
			_worldPacket.WriteInt32(effect.FeedPetTargets.Count);

			foreach (var powerDrainTarget in effect.PowerDrainTargets)
			{
				_worldPacket.WritePackedGuid(powerDrainTarget.Victim);
				_worldPacket.WriteUInt32(powerDrainTarget.Points);
				_worldPacket.WriteUInt32(powerDrainTarget.PowerType);
				_worldPacket.WriteFloat(powerDrainTarget.Amplitude);
			}

			foreach (var extraAttacksTarget in effect.ExtraAttacksTargets)
			{
				_worldPacket.WritePackedGuid(extraAttacksTarget.Victim);
				_worldPacket.WriteUInt32(extraAttacksTarget.NumAttacks);
			}

			foreach (var durabilityDamageTarget in effect.DurabilityDamageTargets)
			{
				_worldPacket.WritePackedGuid(durabilityDamageTarget.Victim);
				_worldPacket.WriteInt32(durabilityDamageTarget.ItemID);
				_worldPacket.WriteInt32(durabilityDamageTarget.Amount);
			}

			foreach (var genericVictimTarget in effect.GenericVictimTargets)
				_worldPacket.WritePackedGuid(genericVictimTarget.Victim);

			foreach (var tradeSkillTarget in effect.TradeSkillTargets)
				_worldPacket.WriteInt32(tradeSkillTarget.ItemID);


			foreach (var feedPetTarget in effect.FeedPetTargets)
				_worldPacket.WriteInt32(feedPetTarget.ItemID);
		}

		WriteLogDataBit();
		FlushBits();
		WriteLogData();
	}
}

class SpellHealLog : CombatLogServerPacket
{
	public ObjectGuid CasterGUID;
	public ObjectGuid TargetGUID;
	public uint SpellID;
	public uint Health;
	public int OriginalHeal;
	public uint OverHeal;
	public uint Absorbed;
	public bool Crit;
	public float? CritRollMade;
	public float? CritRollNeeded;
	public ContentTuningParams ContentTuning;
	public SpellHealLog() : base(ServerOpcodes.SpellHealLog, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(TargetGUID);
		_worldPacket.WritePackedGuid(CasterGUID);

		_worldPacket.WriteUInt32(SpellID);
		_worldPacket.WriteUInt32(Health);
		_worldPacket.WriteInt32(OriginalHeal);
		_worldPacket.WriteUInt32(OverHeal);
		_worldPacket.WriteUInt32(Absorbed);

		_worldPacket.WriteBit(Crit);

		_worldPacket.WriteBit(CritRollMade.HasValue);
		_worldPacket.WriteBit(CritRollNeeded.HasValue);
		WriteLogDataBit();
		_worldPacket.WriteBit(ContentTuning != null);
		FlushBits();

		WriteLogData();

		if (CritRollMade.HasValue)
			_worldPacket.WriteFloat(CritRollMade.Value);

		if (CritRollNeeded.HasValue)
			_worldPacket.WriteFloat(CritRollNeeded.Value);

		if (ContentTuning != null)
			ContentTuning.Write(_worldPacket);
	}
}

class SpellPeriodicAuraLog : CombatLogServerPacket
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

class SpellInterruptLog : ServerPacket
{
	public ObjectGuid Caster;
	public ObjectGuid Victim;
	public uint InterruptedSpellID;
	public uint SpellID;
	public SpellInterruptLog() : base(ServerOpcodes.SpellInterruptLog, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Caster);
		_worldPacket.WritePackedGuid(Victim);
		_worldPacket.WriteUInt32(InterruptedSpellID);
		_worldPacket.WriteUInt32(SpellID);
	}
}

class SpellDispellLog : ServerPacket
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

class SpellEnergizeLog : CombatLogServerPacket
{
	public ObjectGuid TargetGUID;
	public ObjectGuid CasterGUID;
	public uint SpellID;
	public PowerType Type;
	public int Amount;
	public int OverEnergize;
	public SpellEnergizeLog() : base(ServerOpcodes.SpellEnergizeLog, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(TargetGUID);
		_worldPacket.WritePackedGuid(CasterGUID);

		_worldPacket.WriteUInt32(SpellID);
		_worldPacket.WriteUInt32((uint)Type);
		_worldPacket.WriteInt32(Amount);
		_worldPacket.WriteInt32(OverEnergize);

		WriteLogDataBit();
		FlushBits();
		WriteLogData();
	}
}

public class SpellInstakillLog : ServerPacket
{
	public ObjectGuid Target;
	public ObjectGuid Caster;
	public uint SpellID;
	public SpellInstakillLog() : base(ServerOpcodes.SpellInstakillLog, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Target);
		_worldPacket.WritePackedGuid(Caster);
		_worldPacket.WriteUInt32(SpellID);
	}
}

class SpellMissLog : ServerPacket
{
	public uint SpellID;
	public ObjectGuid Caster;
	public List<SpellLogMissEntry> Entries = new();
	public SpellMissLog() : base(ServerOpcodes.SpellMissLog, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WriteUInt32(SpellID);
		_worldPacket.WritePackedGuid(Caster);
		_worldPacket.WriteInt32(Entries.Count);

		foreach (var missEntry in Entries)
			missEntry.Write(_worldPacket);
	}
}

class ProcResist : ServerPacket
{
	public ObjectGuid Caster;
	public ObjectGuid Target;
	public uint SpellID;
	public float? Rolled;
	public float? Needed;
	public ProcResist() : base(ServerOpcodes.ProcResist) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Caster);
		_worldPacket.WritePackedGuid(Target);
		_worldPacket.WriteUInt32(SpellID);
		_worldPacket.WriteBit(Rolled.HasValue);
		_worldPacket.WriteBit(Needed.HasValue);
		_worldPacket.FlushBits();

		if (Rolled.HasValue)
			_worldPacket.WriteFloat(Rolled.Value);

		if (Needed.HasValue)
			_worldPacket.WriteFloat(Needed.Value);
	}
}

class SpellOrDamageImmune : ServerPacket
{
	public ObjectGuid CasterGUID;
	public ObjectGuid VictimGUID;
	public uint SpellID;
	public bool IsPeriodic;
	public SpellOrDamageImmune() : base(ServerOpcodes.SpellOrDamageImmune, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(CasterGUID);
		_worldPacket.WritePackedGuid(VictimGUID);
		_worldPacket.WriteUInt32(SpellID);
		_worldPacket.WriteBit(IsPeriodic);
		_worldPacket.FlushBits();
	}
}

class SpellDamageShield : CombatLogServerPacket
{
	public ObjectGuid Attacker;
	public ObjectGuid Defender;
	public uint SpellID;
	public uint TotalDamage;
	public int OriginalDamage;
	public uint OverKill;
	public uint SchoolMask;
	public uint LogAbsorbed;
	public SpellDamageShield() : base(ServerOpcodes.SpellDamageShield, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Attacker);
		_worldPacket.WritePackedGuid(Defender);
		_worldPacket.WriteUInt32(SpellID);
		_worldPacket.WriteUInt32(TotalDamage);
		_worldPacket.WriteInt32(OriginalDamage);
		_worldPacket.WriteUInt32(OverKill);
		_worldPacket.WriteUInt32(SchoolMask);
		_worldPacket.WriteUInt32(LogAbsorbed);

		WriteLogDataBit();
		FlushBits();
		WriteLogData();
	}
}

class AttackerStateUpdate : CombatLogServerPacket
{
	public HitInfo hitInfo; // Flags
	public ObjectGuid AttackerGUID;
	public ObjectGuid VictimGUID;
	public int Damage;
	public int OriginalDamage;
	public int OverDamage = -1; // (damage - health) or -1 if unit is still alive
	public SubDamage? SubDmg;
	public byte VictimState;
	public uint AttackerState;
	public uint MeleeSpellID;
	public int BlockAmount;
	public int RageGained;
	public UnkAttackerState UnkState;
	public float Unk;
	public ContentTuningParams ContentTuning = new();
	public AttackerStateUpdate() : base(ServerOpcodes.AttackerStateUpdate, ConnectionType.Instance) { }

	public override void Write()
	{
		WorldPacket attackRoundInfo = new();
		attackRoundInfo.WriteUInt32((uint)hitInfo);
		attackRoundInfo.WritePackedGuid(AttackerGUID);
		attackRoundInfo.WritePackedGuid(VictimGUID);
		attackRoundInfo.WriteInt32(Damage);
		attackRoundInfo.WriteInt32(OriginalDamage);
		attackRoundInfo.WriteInt32(OverDamage);
		attackRoundInfo.WriteUInt8((byte)(SubDmg.HasValue ? 1 : 0));

		if (SubDmg.HasValue)
		{
			attackRoundInfo.WriteInt32(SubDmg.Value.SchoolMask);
			attackRoundInfo.WriteFloat(SubDmg.Value.FDamage);
			attackRoundInfo.WriteInt32(SubDmg.Value.Damage);

			if (hitInfo.HasAnyFlag(HitInfo.FullAbsorb | HitInfo.PartialAbsorb))
				attackRoundInfo.WriteInt32(SubDmg.Value.Absorbed);

			if (hitInfo.HasAnyFlag(HitInfo.FullResist | HitInfo.PartialResist))
				attackRoundInfo.WriteInt32(SubDmg.Value.Resisted);
		}

		attackRoundInfo.WriteUInt8(VictimState);
		attackRoundInfo.WriteUInt32(AttackerState);
		attackRoundInfo.WriteUInt32(MeleeSpellID);

		if (hitInfo.HasAnyFlag(HitInfo.Block))
			attackRoundInfo.WriteInt32(BlockAmount);

		if (hitInfo.HasAnyFlag(HitInfo.RageGain))
			attackRoundInfo.WriteInt32(RageGained);

		if (hitInfo.HasAnyFlag(HitInfo.Unk1))
		{
			attackRoundInfo.WriteUInt32(UnkState.State1);
			attackRoundInfo.WriteFloat(UnkState.State2);
			attackRoundInfo.WriteFloat(UnkState.State3);
			attackRoundInfo.WriteFloat(UnkState.State4);
			attackRoundInfo.WriteFloat(UnkState.State5);
			attackRoundInfo.WriteFloat(UnkState.State6);
			attackRoundInfo.WriteFloat(UnkState.State7);
			attackRoundInfo.WriteFloat(UnkState.State8);
			attackRoundInfo.WriteFloat(UnkState.State9);
			attackRoundInfo.WriteFloat(UnkState.State10);
			attackRoundInfo.WriteFloat(UnkState.State11);
			attackRoundInfo.WriteUInt32(UnkState.State12);
		}

		if (hitInfo.HasAnyFlag(HitInfo.Block | HitInfo.Unk12))
			attackRoundInfo.WriteFloat(Unk);

		attackRoundInfo.WriteUInt8((byte)ContentTuning.TuningType);
		attackRoundInfo.WriteUInt8(ContentTuning.TargetLevel);
		attackRoundInfo.WriteUInt8(ContentTuning.Expansion);
		attackRoundInfo.WriteInt16(ContentTuning.PlayerLevelDelta);
		attackRoundInfo.WriteInt8(ContentTuning.TargetScalingLevelDelta);
		attackRoundInfo.WriteFloat(ContentTuning.PlayerItemLevel);
		attackRoundInfo.WriteFloat(ContentTuning.TargetItemLevel);
		attackRoundInfo.WriteUInt16(ContentTuning.ScalingHealthItemLevelCurveID);
		attackRoundInfo.WriteUInt32((uint)ContentTuning.Flags);
		attackRoundInfo.WriteUInt32(ContentTuning.PlayerContentTuningID);
		attackRoundInfo.WriteUInt32(ContentTuning.TargetContentTuningID);

		WriteLogDataBit();
		FlushBits();
		WriteLogData();

		_worldPacket.WriteUInt32(attackRoundInfo.GetSize());
		_worldPacket.WriteBytes(attackRoundInfo);
	}
}

class SpellAbsorbLog : CombatLogServerPacket
{
	public ObjectGuid Attacker;
	public ObjectGuid Victim;
	public ObjectGuid Caster;
	public uint AbsorbedSpellID;
	public uint AbsorbSpellID;
	public int Absorbed;
	public uint OriginalDamage;
	public bool Unk;
	public SpellAbsorbLog() : base(ServerOpcodes.SpellAbsorbLog, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Attacker);
		_worldPacket.WritePackedGuid(Victim);
		_worldPacket.WriteUInt32(AbsorbedSpellID);
		_worldPacket.WriteUInt32(AbsorbSpellID);
		_worldPacket.WritePackedGuid(Caster);
		_worldPacket.WriteInt32(Absorbed);
		_worldPacket.WriteUInt32(OriginalDamage);

		_worldPacket.WriteBit(Unk);
		WriteLogDataBit();
		FlushBits();

		WriteLogData();
	}
}

class SpellHealAbsorbLog : ServerPacket
{
	public ObjectGuid Healer;
	public ObjectGuid Target;
	public ObjectGuid AbsorbCaster;
	public int AbsorbSpellID;
	public int AbsorbedSpellID;
	public int Absorbed;
	public int OriginalHeal;
	public ContentTuningParams ContentTuning;
	public SpellHealAbsorbLog() : base(ServerOpcodes.SpellHealAbsorbLog, ConnectionType.Instance) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Target);
		_worldPacket.WritePackedGuid(AbsorbCaster);
		_worldPacket.WritePackedGuid(Healer);
		_worldPacket.WriteInt32(AbsorbSpellID);
		_worldPacket.WriteInt32(AbsorbedSpellID);
		_worldPacket.WriteInt32(Absorbed);
		_worldPacket.WriteInt32(OriginalHeal);
		_worldPacket.WriteBit(ContentTuning != null);
		_worldPacket.FlushBits();

		if (ContentTuning != null)
			ContentTuning.Write(_worldPacket);
	}
}

//Structs
public struct SpellLogEffectPowerDrainParams
{
	public ObjectGuid Victim;
	public uint Points;
	public uint PowerType;
	public float Amplitude;
}

public struct SpellLogEffectExtraAttacksParams
{
	public ObjectGuid Victim;
	public uint NumAttacks;
}

public struct SpellLogEffectDurabilityDamageParams
{
	public ObjectGuid Victim;
	public int ItemID;
	public int Amount;
}

public struct SpellLogEffectGenericVictimParams
{
	public ObjectGuid Victim;
}

public struct SpellLogEffectTradeSkillItemParams
{
	public int ItemID;
}

public struct SpellLogEffectFeedPetParams
{
	public int ItemID;
}

struct SpellLogMissDebug
{
	public void Write(WorldPacket data)
	{
		data.WriteFloat(HitRoll);
		data.WriteFloat(HitRollNeeded);
	}

	public float HitRoll;
	public float HitRollNeeded;
}

public class SpellLogMissEntry
{
	public ObjectGuid Victim;
	public byte MissReason;
	SpellLogMissDebug? Debug;

	public SpellLogMissEntry(ObjectGuid victim, byte missReason)
	{
		Victim = victim;
		MissReason = missReason;
	}

	public void Write(WorldPacket data)
	{
		data.WritePackedGuid(Victim);
		data.WriteUInt8(MissReason);

		if (data.WriteBit(Debug.HasValue))
			Debug.Value.Write(data);

		data.FlushBits();
	}
}

struct SpellDispellData
{
	public uint SpellID;
	public bool Harmful;
	public int? Rolled;
	public int? Needed;
}

public struct SubDamage
{
	public int SchoolMask;
	public float FDamage; // Float damage (Most of the time equals to Damage)
	public int Damage;
	public int Absorbed;
	public int Resisted;
}

public struct UnkAttackerState
{
	public uint State1;
	public float State2;
	public float State3;
	public float State4;
	public float State5;
	public float State6;
	public float State7;
	public float State8;
	public float State9;
	public float State10;
	public float State11;
	public uint State12;
}