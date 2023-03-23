// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Entities.Creatures;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Entities.Players;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Entities.Units;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking.Packets.Spell;

namespace Game.Common.Networking.Packets.Spell;

public class ContentTuningParams
{
	public enum ContentTuningType
	{
		CreatureToPlayerDamage = 1,
		PlayerToCreatureDamage = 2,
		CreatureToCreatureDamage = 4,
		PlayerToSandboxScaling = 7, // NYI
		PlayerToPlayerExpectedStat = 8
	}

	public enum ContentTuningFlags
	{
		NoLevelScaling = 0x1,
		NoItemLevelScaling = 0x2
	}

	public ContentTuningType TuningType;
	public short PlayerLevelDelta;
	public float PlayerItemLevel;
	public float TargetItemLevel;
	public ushort ScalingHealthItemLevelCurveID;
	public byte TargetLevel;
	public byte Expansion;
	public sbyte TargetScalingLevelDelta;
	public ContentTuningFlags Flags = ContentTuningFlags.NoLevelScaling | ContentTuningFlags.NoItemLevelScaling;
	public uint PlayerContentTuningID;
	public uint TargetContentTuningID;
	public int Unused927;

	public bool GenerateDataForUnits(Unit attacker, Unit target)
	{
		var playerAttacker = attacker.AsPlayer;
		var creatureAttacker = attacker.AsCreature;

		if (playerAttacker)
		{
			var playerTarget = target.AsPlayer;
			var creatureTarget = target.AsCreature;

			if (playerTarget)
				return GenerateDataPlayerToPlayer(playerAttacker, playerTarget);
			else if (creatureTarget)
				if (creatureTarget.HasScalableLevels)
					return GenerateDataPlayerToCreature(playerAttacker, creatureTarget);
		}
		else if (creatureAttacker)
		{
			var playerTarget = target.AsPlayer;
			var creatureTarget = target.AsCreature;

			if (playerTarget)
			{
				if (creatureAttacker.HasScalableLevels)
					return GenerateDataCreatureToPlayer(creatureAttacker, playerTarget);
			}
			else if (creatureTarget)
			{
				if (creatureAttacker.HasScalableLevels || creatureTarget.HasScalableLevels)
					return GenerateDataCreatureToCreature(creatureAttacker, creatureTarget);
			}
		}

		return false;
	}

	public void Write(WorldPacket data)
	{
		data.WriteFloat(PlayerItemLevel);
		data.WriteFloat(TargetItemLevel);
		data.WriteInt16(PlayerLevelDelta);
		data.WriteUInt16(ScalingHealthItemLevelCurveID);
		data.WriteUInt8(TargetLevel);
		data.WriteUInt8(Expansion);
		data.WriteInt8(TargetScalingLevelDelta);
		data.WriteUInt32((uint)Flags);
		data.WriteUInt32(PlayerContentTuningID);
		data.WriteUInt32(TargetContentTuningID);
		data.WriteInt32(Unused927);
		data.WriteBits(TuningType, 4);
		data.FlushBits();
	}

	bool GenerateDataPlayerToPlayer(Player attacker, Player target)
	{
		return false;
	}

	bool GenerateDataCreatureToPlayer(Creature attacker, Player target)
	{
		var creatureTemplate = attacker.Template;
		var creatureScaling = creatureTemplate.GetLevelScaling(attacker.Map.DifficultyID);

		TuningType = ContentTuningType.CreatureToPlayerDamage;
		PlayerLevelDelta = (short)target.ActivePlayerData.ScalingPlayerLevelDelta;
		PlayerItemLevel = (ushort)target.GetAverageItemLevel();
		ScalingHealthItemLevelCurveID = (ushort)target.UnitData.ScalingHealthItemLevelCurveID;
		TargetLevel = (byte)target.Level;
		Expansion = (byte)creatureTemplate.HealthScalingExpansion;
		TargetScalingLevelDelta = (sbyte)attacker.UnitData.ScalingLevelDelta;
		TargetContentTuningID = creatureScaling.ContentTuningId;

		return true;
	}

	bool GenerateDataPlayerToCreature(Player attacker, Creature target)
	{
		var creatureTemplate = target.Template;
		var creatureScaling = creatureTemplate.GetLevelScaling(target.Map.DifficultyID);

		TuningType = ContentTuningType.PlayerToCreatureDamage;
		PlayerLevelDelta = (short)attacker.ActivePlayerData.ScalingPlayerLevelDelta;
		PlayerItemLevel = (ushort)attacker.GetAverageItemLevel();
		ScalingHealthItemLevelCurveID = (ushort)target.UnitData.ScalingHealthItemLevelCurveID;
		TargetLevel = (byte)target.Level;
		Expansion = (byte)creatureTemplate.HealthScalingExpansion;
		TargetScalingLevelDelta = (sbyte)target.UnitData.ScalingLevelDelta;
		TargetContentTuningID = creatureScaling.ContentTuningId;

		return true;
	}

	bool GenerateDataCreatureToCreature(Creature attacker, Creature target)
	{
		var accessor = target.HasScalableLevels ? target : attacker;
		var creatureTemplate = accessor.Template;
		var creatureScaling = creatureTemplate.GetLevelScaling(accessor.Map.DifficultyID);

		TuningType = ContentTuningType.CreatureToCreatureDamage;
		PlayerLevelDelta = 0;
		PlayerItemLevel = 0;
		TargetLevel = (byte)target.Level;
		Expansion = (byte)creatureTemplate.HealthScalingExpansion;
		TargetScalingLevelDelta = (sbyte)accessor.UnitData.ScalingLevelDelta;
		TargetContentTuningID = creatureScaling.ContentTuningId;

		return true;
	}
}
