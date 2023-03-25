﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Entities.Objects;

namespace Forged.RealmServer.Achievements;

public class CompletedAchievementData
{
	public long Date;
	public List<ObjectGuid> CompletingPlayers = new();
	public bool Changed;
}