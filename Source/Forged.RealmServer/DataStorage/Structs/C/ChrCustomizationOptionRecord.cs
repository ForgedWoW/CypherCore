// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.RealmServer.DataStorage;

public sealed class ChrCustomizationOptionRecord
{
	public LocalizedString Name;
	public uint Id;
	public ushort SecondaryID;
	public int Flags;
	public uint ChrModelID;
	public int SortIndex;
	public int ChrCustomizationCategoryID;
	public int OptionType;
	public float BarberShopCostModifier;
	public int ChrCustomizationID;
	public int ChrCustomizationReqID;
	public int UiOrderIndex;
	public int AddedInPatch;
}