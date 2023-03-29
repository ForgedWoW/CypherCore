// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;

namespace Forged.MapServer.DataStorage.Structs.U;

public sealed class UiMapAssignmentRecord
{
    public Vector2 UiMin;
    public Vector2 UiMax;
    public Vector3[] Region = new Vector3[2];
    public uint Id;
    public int UiMapID;
    public int OrderIndex;
    public int MapID;
    public int AreaID;
    public int WmoDoodadPlacementID;
    public int WmoGroupID;
}