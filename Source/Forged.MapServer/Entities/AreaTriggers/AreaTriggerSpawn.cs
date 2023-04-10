// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Maps;
using Framework.Constants;

namespace Forged.MapServer.Entities.AreaTriggers;

public class AreaTriggerSpawn : SpawnData
{
    public AreaTriggerSpawn() : base(SpawnObjectType.AreaTrigger) { }

    public AreaTriggerShapeInfo Shape { get; set; } = new();
    public AreaTriggerId TriggerId { get; set; }
}