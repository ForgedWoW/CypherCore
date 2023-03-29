// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.DataStorage.Structs;

public struct M2Track
{
    public ushort interpolation_type;
    public ushort global_sequence;
    public M2Array timestamps;
    public M2Array values;
}