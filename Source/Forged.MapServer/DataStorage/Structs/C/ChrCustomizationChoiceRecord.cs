// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.DataStorage.ClientReader;

namespace Forged.MapServer.DataStorage.Structs.C;

public sealed class ChrCustomizationChoiceRecord
{
    public int AddedInPatch;
    public uint ChrCustomizationOptionID;
    public uint ChrCustomizationReqID;
    public int ChrCustomizationVisReqID;
    public int Flags;
    public uint Id;
    public LocalizedString Name;
    public ushort SortOrder;
    public int[] SwatchColor = new int[2];
    public ushort UiOrderIndex;
}