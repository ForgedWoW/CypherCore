// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.DataStorage.ClientReader;

namespace Game.Common.DataStorage.Structs.A;

public sealed class AreaPOIRecord
{
	public LocalizedString Name;
	public LocalizedString Description;
	public uint ID;
	public Vector3 Pos;
	public int PortLocID;
	public uint PlayerConditionID;
	public uint UiTextureAtlasMemberID;
	public uint Flags;
	public int WmoGroupID;
	public int PoiDataType;
	public int PoiData;
	public uint Field_9_1_0;
	public int Field_10_0_0_45141_012;
	public ushort ContinentID;
	public short AreaID;
	public ushort WorldStateID;
	public ushort UiWidgetSetID;
	public ushort UiTextureKitID;
	public ushort Field_9_1_0_38783;
	public byte Importance;
	public byte Icon;
}
