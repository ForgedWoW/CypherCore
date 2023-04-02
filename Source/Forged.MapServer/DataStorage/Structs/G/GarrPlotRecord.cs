// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.G;

public sealed class GarrPlotRecord
{
    public uint AllianceConstructObjID;
    public byte Flags;
    public uint HordeConstructObjID;
    public uint Id;
    public string Name;
    public byte PlotType;
    public byte UiCategoryID;
    public uint[] UpgradeRequirement = new uint[2];
}