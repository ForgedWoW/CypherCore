// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;

namespace Game.Common.DataStorage.Structs.L;

public sealed class LightRecord
{
	public uint Id;
	public Vector3 GameCoords;
	public float GameFalloffStart;
	public float GameFalloffEnd;
	public short ContinentID;
	public ushort[] LightParamsID = new ushort[8];
}
