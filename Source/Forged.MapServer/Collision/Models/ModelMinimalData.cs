// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Framework.GameMath;

namespace Forged.MapServer.Collision.Models;

public class ModelMinimalData
{
    public byte AdtId;
    public AxisAlignedBox Bound;
    public byte Flags;
    public uint Id;
    public string Name;
    public Vector3 Pos;
    public float Scale;
}