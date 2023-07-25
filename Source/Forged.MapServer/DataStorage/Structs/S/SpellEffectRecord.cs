using Framework.Constants;
using Framework.Dynamic;

namespace Forged.MapServer.DataStorage.Structs.S;

public sealed class SpellEffectRecord
{
    public uint Id;
    public short EffectAura;
    public uint DifficultyID;
    public int EffectIndex;
    public uint Effect;
    public float EffectAmplitude;
    public SpellEffectAttributes EffectAttributes;
    public uint EffectAuraPeriod;
    public float EffectBonusCoefficient;
    public float EffectChainAmplitude;
    public int EffectChainTargets;
    public uint EffectItemType;
    public int EffectMechanic;
    public float EffectPointsPerResource;
    public float EffectPosFacing;
    public float EffectRealPointsPerLevel;
    public uint EffectTriggerSpell;
    public float BonusCoefficientFromAP;
    public float PvpMultiplier;
    public float Coefficient;
    public float Variance;
    public float ResourceCoefficient;
    public float GroupSizeBasePointsCoefficient;
    public float EffectBasePoints;
    public int ScalingClass;
    public int[] EffectMiscValue = new int[2];
    public uint[] EffectRadiusIndex = new uint[2];
    public FlagArray128 EffectSpellClassMask;
    public short[] ImplicitTarget = new short[2];
    public uint SpellID;
}