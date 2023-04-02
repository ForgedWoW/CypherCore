// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;

namespace Forged.MapServer.DataStorage.Structs.L;

public sealed class LightRecord
{
    public short ContinentID;
    public Vector3 GameCoords;
    public float GameFalloffEnd;
    public float GameFalloffStart;
    public uint Id;
    public ushort[] LightParamsID = new ushort[8];
}