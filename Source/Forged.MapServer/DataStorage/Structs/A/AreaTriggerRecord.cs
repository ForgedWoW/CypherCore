// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;

namespace Forged.MapServer.DataStorage.Structs.A;

public sealed record AreaTriggerRecord
{
    public short AreaTriggerActionSetID;
    public float BoxHeight;
    public float BoxLength;
    public float BoxWidth;
    public float BoxYaw;
    public ushort ContinentID;
    public sbyte Flags;
    public uint Id;
    public ushort PhaseGroupID;
    public ushort PhaseID;
    public sbyte PhaseUseFlags;
    public Vector3 Pos;
    public float Radius;
    public short ShapeID;
    public sbyte ShapeType;
}