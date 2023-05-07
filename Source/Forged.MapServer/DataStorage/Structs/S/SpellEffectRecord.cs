// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Framework.Dynamic;

namespace Forged.MapServer.DataStorage.Structs.S;

public sealed record SpellEffectRecord
{
    public float BonusCoefficientFromAP;
    public float Coefficient;
    public uint DifficultyID;
    public uint Effect;
    public float EffectAmplitude;
    public SpellEffectAttributes EffectAttributes;
    public short EffectAura;
    public uint EffectAuraPeriod;
    public float EffectBasePoints;
    public float EffectBonusCoefficient;
    public float EffectChainAmplitude;
    public int EffectChainTargets;
    public int EffectIndex;
    public uint EffectItemType;
    public int EffectMechanic;
    public int[] EffectMiscValue = new int[2];
    public float EffectPointsPerResource;
    public float EffectPosFacing;
    public uint[] EffectRadiusIndex = new uint[2];
    public float EffectRealPointsPerLevel;
    public FlagArray128 EffectSpellClassMask;
    public uint EffectTriggerSpell;
    public float GroupSizeBasePointsCoefficient;
    public uint Id;
    public short[] ImplicitTarget = new short[2];
    public float PvpMultiplier;
    public float ResourceCoefficient;
    public int ScalingClass;
    public uint SpellID;
    public float Variance;
}