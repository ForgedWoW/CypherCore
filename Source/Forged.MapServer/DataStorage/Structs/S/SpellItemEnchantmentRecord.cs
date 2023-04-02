// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.S;

public sealed class SpellItemEnchantmentRecord
{
    public byte Charges;
    public byte ConditionID;
    public int Duration;
    public ItemEnchantmentType[] Effect = new ItemEnchantmentType[ItemConst.MaxItemEnchantmentEffects];
    public uint[] EffectArg = new uint[ItemConst.MaxItemEnchantmentEffects];
    public ushort[] EffectPointsMin = new ushort[ItemConst.MaxItemEnchantmentEffects];
    public float[] EffectScalingPoints = new float[ItemConst.MaxItemEnchantmentEffects];
    public ushort Flags;
    public string HordeName;
    public uint IconFileDataID;
    public uint Id;
    public ushort ItemLevel;
    public ushort ItemVisual;
    public int MaxItemLevel;
    public byte MaxLevel;
    public int MinItemLevel;
    public byte MinLevel;
    public string Name;
    public ushort RequiredSkillID;
    public ushort RequiredSkillRank;
    public sbyte ScalingClass;
    public sbyte ScalingClassRestricted;
    public uint TransmogCost;
    public uint TransmogUseConditionID;
    public SpellItemEnchantmentFlags GetFlags()
    {
        return (SpellItemEnchantmentFlags)Flags;
    }
}