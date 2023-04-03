// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Achievements;

public class CompletedAchievementData
{
    public bool Changed { get; set; }
    public List<ObjectGuid> CompletingPlayers { get; set; } = new();
    public long Date { get; set; }
}