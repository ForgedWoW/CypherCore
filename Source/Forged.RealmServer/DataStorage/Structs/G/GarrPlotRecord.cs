// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.RealmServer.DataStorage;

public sealed class GarrPlotRecord
{
	public uint Id;
	public string Name;
	public byte PlotType;
	public uint HordeConstructObjID;
	public uint AllianceConstructObjID;
	public byte Flags;
	public byte UiCategoryID;
	public uint[] UpgradeRequirement = new uint[2];
}