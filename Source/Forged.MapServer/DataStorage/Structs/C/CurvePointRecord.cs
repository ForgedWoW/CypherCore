// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;

namespace Forged.MapServer.DataStorage.Structs.C;

public sealed record CurvePointRecord
{
    public uint CurveID;
    public uint Id;
    public byte OrderIndex;
    public Vector2 Pos;
    public Vector2 PreSLSquishPos;
}