// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.DataStorage.ClientReader;

namespace Forged.MapServer.DataStorage.Structs.C;

public sealed record ChrCustomizationOptionRecord
{
    public int AddedInPatch;
    public float BarberShopCostModifier;
    public int ChrCustomizationCategoryID;
    public int ChrCustomizationID;
    public int ChrCustomizationReqID;
    public uint ChrModelID;
    public int Flags;
    public uint Id;
    public LocalizedString Name;
    public int OptionType;
    public ushort SecondaryID;
    public int SortIndex;
    public int UiOrderIndex;
    public float QuestXpMultiplier;
}