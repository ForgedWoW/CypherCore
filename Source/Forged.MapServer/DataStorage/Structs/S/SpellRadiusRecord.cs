// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.S;

public sealed record SpellRadiusRecord
{
    public uint Id;
    public float Radius;
    public float RadiusMax;
    public float RadiusMin;
    public float RadiusPerLevel;
}