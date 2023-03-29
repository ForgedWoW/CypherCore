// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Entities.Players;

public class DuelInfo
{
    public Player Opponent { get; set; }
    public Player Initiator { get; set; }
    public bool IsMounted { get; set; }
    public DuelState State { get; set; }
    public long StartTime { get; set; }
    public long OutOfBoundsTime { get; set; }

    public DuelInfo(Player opponent, Player initiator, bool isMounted)
    {
        Opponent = opponent;
        Initiator = initiator;
        IsMounted = isMounted;
    }
}