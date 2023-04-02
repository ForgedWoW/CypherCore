// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;

namespace Forged.MapServer.DataStorage.Structs.U;

public sealed class UiMapLinkRecord
{
    public int ChildUiMapID;
    public int Flags;
    public uint Id;
    public int OrderIndex;
    public int OverrideHighlightAtlasID;
    public int OverrideHighlightFileDataID;
    public int ParentUiMapID;
    public int PlayerConditionID;
    public Vector2 UiMax;
    public Vector2 UiMin;
}