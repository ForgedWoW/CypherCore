// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.RealmServer.DataStorage;

public sealed class ChrCustomizationChoiceRecord
{
	public LocalizedString Name;
	public uint Id;
	public uint ChrCustomizationOptionID;
	public uint ChrCustomizationReqID;
	public int ChrCustomizationVisReqID;
	public ushort SortOrder;
	public ushort UiOrderIndex;
	public int Flags;
	public int AddedInPatch;
	public int[] SwatchColor = new int[2];
}