// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Maps;
using Framework.Constants;

namespace Forged.MapServer.Entities.AreaTriggers;

public class AreaTriggerSpawn : SpawnData
{
    public AreaTriggerId TriggerId;
    public AreaTriggerShapeInfo Shape = new();

    public AreaTriggerSpawn() : base(SpawnObjectType.AreaTrigger) { }
}