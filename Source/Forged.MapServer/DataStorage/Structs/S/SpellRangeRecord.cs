// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.S;

public sealed class SpellRangeRecord
{
    public string DisplayName;
    public string DisplayNameShort;
    public SpellRangeFlag Flags;
    public uint Id;
    public float[] RangeMax = new float[2];
    public float[] RangeMin = new float[2];
}