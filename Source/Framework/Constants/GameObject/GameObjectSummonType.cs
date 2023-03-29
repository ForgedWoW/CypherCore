// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum GameObjectSummonType
{
    TimedOrCorpseDespawn = 0, // despawns after a specified time OR when the summoner dies
    TimedDespawn = 1          // despawns after a specified time
}