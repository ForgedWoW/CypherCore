// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;

namespace Forged.MapServer.Networking.Packets.Spell;

internal class ContentTuningParams
{
    public enum ContentTuningFlags
    {
        NoLevelScaling = 0x1,
        NoItemLevelScaling = 0x2
    }

    public enum ContentTuningType
    {
        CreatureToPlayerDamage = 1,
        PlayerToCreatureDamage = 2,
        CreatureToCreatureDamage = 4,
        PlayerToSandboxScaling = 7, // NYI
        PlayerToPlayerExpectedStat = 8
    }

    public byte Expansion;

    public ContentTuningFlags Flags = ContentTuningFlags.NoLevelScaling | ContentTuningFlags.NoItemLevelScaling;

    public uint PlayerContentTuningID;

    public float PlayerItemLevel;

    public short PlayerLevelDelta;

    public uint ScalingHealthItemLevelCurveID;

    public uint TargetContentTuningID;

    public float TargetItemLevel;

    public byte TargetLevel;

    public sbyte TargetScalingLevelDelta;

    public ContentTuningType TuningType;

    public int Unused927;

    public bool GenerateDataForUnits(Unit attacker, Unit target)
    {
        var playerAttacker = attacker.AsPlayer;
        var creatureAttacker = attacker.AsCreature;

        if (playerAttacker != null)
        {
            var playerTarget = target.AsPlayer;
            var creatureTarget = target.AsCreature;

            if (playerTarget != null)
                return false;

            if (creatureTarget == null)
                return false;

            if (creatureTarget.HasScalableLevels)
                return GenerateDataPlayerToCreature(playerAttacker, creatureTarget);
        }
        else if (creatureAttacker != null)
        {
            var playerTarget = target.AsPlayer;
            var creatureTarget = target.AsCreature;

            if (playerTarget != null)
            {
                if (creatureAttacker.HasScalableLevels)
                    return GenerateDataCreatureToPlayer(creatureAttacker, playerTarget);
            }
            else if (creatureTarget != null)
                if (creatureAttacker.HasScalableLevels || creatureTarget.HasScalableLevels)
                    return GenerateDataCreatureToCreature(creatureAttacker, creatureTarget);
        }

        return false;
    }

    public void Write(WorldPacket data)
    {
        data.WriteFloat(PlayerItemLevel);
        data.WriteFloat(TargetItemLevel);
        data.WriteInt16(PlayerLevelDelta);
        data.WriteUInt32(ScalingHealthItemLevelCurveID);
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

    private bool GenerateDataCreatureToCreature(Creature attacker, Creature target)
    {
        var accessor = target.HasScalableLevels ? target : attacker;
        var creatureTemplate = accessor.Template;
        var creatureScaling = creatureTemplate.GetLevelScaling(accessor.Location.Map.DifficultyID);

        TuningType = ContentTuningType.CreatureToCreatureDamage;
        PlayerLevelDelta = 0;
        PlayerItemLevel = 0;
        TargetLevel = (byte)target.Level;
        Expansion = (byte)creatureTemplate.HealthScalingExpansion;
        TargetScalingLevelDelta = (sbyte)accessor.UnitData.ScalingLevelDelta;
        TargetContentTuningID = creatureScaling.ContentTuningId;

        return true;
    }

    private bool GenerateDataCreatureToPlayer(Creature attacker, Player target)
    {
        var creatureTemplate = attacker.Template;
        var creatureScaling = creatureTemplate.GetLevelScaling(attacker.Location.Map.DifficultyID);

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

    private bool GenerateDataPlayerToCreature(Player attacker, Creature target)
    {
        var creatureTemplate = target.Template;
        var creatureScaling = creatureTemplate.GetLevelScaling(target.Location.Map.DifficultyID);

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
}