// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.AreaTriggers;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Maps;

public class SpawnData : SpawnMetadata
{
    public uint Id; // entry in respective _template table
    public uint PhaseGroup;
    public uint PhaseId;
    public PhaseUseFlagsValues PhaseUseFlags;
    public uint PoolId;
    public uint ScriptId;
    public List<Difficulty> SpawnDifficulties;
    public Position SpawnPoint;
    public int Spawntimesecs;
    public string StringId;
    public int TerrainSwapMap;
    public SpawnData(SpawnObjectType t) : base(t)
    {
        SpawnPoint = new Position();
        TerrainSwapMap = -1;
        SpawnDifficulties = new List<Difficulty>();
    }

    public static SpawnObjectType TypeFor<T>()
    {
        return typeof(T).Name switch
        {
            nameof(Creature)    => SpawnObjectType.Creature,
            nameof(GameObject)  => SpawnObjectType.GameObject,
            nameof(AreaTrigger) => SpawnObjectType.AreaTrigger,
            _                   => SpawnObjectType.NumSpawnTypes
        };
    }
}