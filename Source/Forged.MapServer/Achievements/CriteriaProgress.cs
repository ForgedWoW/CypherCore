// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Achievements;

public class CriteriaProgress
{
    public ObjectGuid PlayerGUID;
    public bool Changed { get; set; }
    public ulong Counter { get; set; }

    public long Date { get; set; } // latest update time.
    // GUID of the player that completed this criteria (guild achievements)
}