// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;

namespace Forged.MapServer.DataStorage.Structs.U;

public sealed record UiMapAssignmentRecord
{
    public int AreaID;
    public uint Id;
    public int MapID;
    public int OrderIndex;
    public Vector3[] Region = new Vector3[2];
    public int UiMapID;
    public Vector2 UiMax;
    public Vector2 UiMin;
    public int WmoDoodadPlacementID;
    public int WmoGroupID;
}