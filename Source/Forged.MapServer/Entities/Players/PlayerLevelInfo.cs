// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Entities.Players;

public class PlayerLevelInfo
{
    public int[] Stats { get; set; } = new int[(int)Framework.Constants.Stats.Max];
}