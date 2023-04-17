﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Maps;

public class SpawnMetadata
{
    public SpawnMetadata(SpawnObjectType t)
    {
        Type = t;
    }

    public bool DbData { get; set; } = true;
    public uint MapId { get; set; } = 0xFFFFFFFF;
    public SpawnGroupTemplateData SpawnGroupData { get; set; } = null;
    public ulong SpawnId { get; set; }
    public SpawnObjectType Type { get; set; }

    public static bool TypeHasData(SpawnObjectType type)
    {
        return type < SpawnObjectType.NumSpawnTypesWithData;
    }

    public static bool TypeInMask(SpawnObjectType type, SpawnObjectTypeMask mask)
    {
        return ((1 << (int)type) & (int)mask) != 0;
    }

    public static bool TypeIsValid(SpawnObjectType type)
    {
        return type < SpawnObjectType.NumSpawnTypes;
    }

    public SpawnData ToSpawnData()
    {
        return TypeHasData(Type) ? (SpawnData)this : null;
    }
}