// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Framework.GameMath;

namespace Forged.MapServer.Collision.Models;

public class ModelMinimalData
{
    public byte AdtId;
    public byte Flags;
    public AxisAlignedBox IBound;
    public uint Id;
    public Vector3 IPos;
    public float IScale;
    public string Name;
}