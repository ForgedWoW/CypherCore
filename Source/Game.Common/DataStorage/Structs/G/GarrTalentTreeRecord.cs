using Game.DataStorage;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.DataStorage.Structs.G;

public sealed class GarrTalentTreeRecord
{
	public uint Id;
	public string Name;
	public byte GarrTypeID;
	public int ClassID;
	public sbyte MaxTiers;
	public sbyte UiOrder;
	public int Flags;
	public ushort UiTextureKitID;
	public int GarrTalentTreeType;
	public int PlayerConditionID;
	public byte FeatureTypeIndex;
	public sbyte FeatureSubtypeIndex;
	public int CurrencyID;
}
