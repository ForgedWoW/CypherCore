// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Maps.Instances;

public struct ObjectData
{
    public uint Entry;

    public uint Type;

    public ObjectData(uint entry, uint type)
    {
        Entry = entry;
        Type = type;
    }
}