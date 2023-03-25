// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.RealmServer.DataStorage;

public sealed class TransmogSetRecord
{
	public string Name;
	public uint Id;
	public int ClassMask;
	public uint TrackingQuestID;
	public int Flags;
	public uint TransmogSetGroupID;
	public int ItemNameDescriptionID;
	public ushort ParentTransmogSetID;
	public byte Unknown810;
	public byte ExpansionID;
	public int PatchID;
	public short UiOrder;
	public uint PlayerConditionID;
}