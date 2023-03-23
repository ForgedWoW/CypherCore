// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Game.DataStorage;

namespace Game.Common.DataStorage.Structs.C;

public sealed class CurvePointRecord
{
	public Vector2 Pos;
	public Vector2 PreSLSquishPos;
	public uint Id;
	public ushort CurveID;
	public byte OrderIndex;
}
