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
    public SpawnData(SpawnObjectType t) : base(t)
    {
        SpawnPoint = new Position();
        TerrainSwapMap = -1;
        SpawnDifficulties = new List<Difficulty>();
    }

    public uint Id { get; set; } // entry in respective _template table
    public uint PhaseGroup { get; set; }
    public uint PhaseId { get; set; }
    public PhaseUseFlagsValues PhaseUseFlags { get; set; }
    public uint PoolId { get; set; }
    public uint ScriptId { get; set; }
    public List<Difficulty> SpawnDifficulties { get; set; }
    public Position SpawnPoint { get; set; }
    public int Spawntimesecs { get; set; }
    public string StringId { get; set; }
    public int TerrainSwapMap { get; set; }
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