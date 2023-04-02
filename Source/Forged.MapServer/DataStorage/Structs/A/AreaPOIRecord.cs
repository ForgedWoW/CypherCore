// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Forged.MapServer.DataStorage.ClientReader;

namespace Forged.MapServer.DataStorage.Structs.A;

public sealed class AreaPOIRecord
{
    public short AreaID;
    public ushort ContinentID;
    public LocalizedString Description;
    public int Field_10_0_0_45141_012;
    public uint Field_9_1_0;
    public ushort Field_9_1_0_38783;
    public uint Flags;
    public byte Icon;
    public uint ID;
    public byte Importance;
    public LocalizedString Name;
    public uint PlayerConditionID;
    public int PoiData;
    public int PoiDataType;
    public int PortLocID;
    public Vector3 Pos;
    public uint UiTextureAtlasMemberID;
    public ushort UiTextureKitID;
    public ushort UiWidgetSetID;
    public int WmoGroupID;
    public ushort WorldStateID;
}