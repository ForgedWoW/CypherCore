// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs.R;

public sealed record RandPropPointsRecord
{
    public int DamageReplaceStat;
    public float DamageReplaceStatF;
    public int DamageSecondary;
    public float DamageSecondaryF;
    public uint[] Epic = new uint[5];
    public float[] EpicF = new float[5];
    public uint[] Good = new uint[5];
    public float[] GoodF = new float[5];
    public uint Id;
    public uint[] Superior = new uint[5];
    public float[] SuperiorF = new float[5];
}