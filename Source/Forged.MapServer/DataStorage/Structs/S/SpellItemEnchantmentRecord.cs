// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.S;

public sealed class SpellItemEnchantmentRecord
{
	public uint Id;
	public string Name;
	public string HordeName;
	public int Duration;
	public uint[] EffectArg = new uint[ItemConst.MaxItemEnchantmentEffects];
	public float[] EffectScalingPoints = new float[ItemConst.MaxItemEnchantmentEffects];
	public uint IconFileDataID;
	public int MinItemLevel;
	public int MaxItemLevel;
	public uint TransmogUseConditionID;
	public uint TransmogCost;
	public ushort[] EffectPointsMin = new ushort[ItemConst.MaxItemEnchantmentEffects];
	public ushort ItemVisual;
	public ushort Flags;
	public ushort RequiredSkillID;
	public ushort RequiredSkillRank;
	public ushort ItemLevel;
	public byte Charges;
	public ItemEnchantmentType[] Effect = new ItemEnchantmentType[ItemConst.MaxItemEnchantmentEffects];
	public sbyte ScalingClass;
	public sbyte ScalingClassRestricted;
	public byte ConditionID;
	public byte MinLevel;
	public byte MaxLevel;

	public SpellItemEnchantmentFlags GetFlags()
	{
		return (SpellItemEnchantmentFlags)Flags;
	}
}