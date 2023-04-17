// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Framework.Util;
using Microsoft.Extensions.Configuration;

namespace Forged.MapServer.Entities.Creatures;

public class CreatureMovementData
{
    public CreatureMovementData(IConfiguration configuration)
    {
        Ground = CreatureGroundMovementType.Run;
        Flight = CreatureFlightMovementType.None;
        Swim = true;
        Rooted = false;
        Chase = CreatureChaseMovementType.Run;
        Random = CreatureRandomMovementType.Walk;
        InteractionPauseTimer = configuration.GetDefaultValue("Creature:MovingStopTimeForPlayer", 3u * Time.MINUTE * Time.IN_MILLISECONDS);
    }

    public CreatureChaseMovementType Chase { get; set; }
    public CreatureFlightMovementType Flight { get; set; }
    public CreatureGroundMovementType Ground { get; set; }
    public uint InteractionPauseTimer { get; set; }
    public bool IsFlightAllowed => Flight != CreatureFlightMovementType.None;
    public bool IsGroundAllowed => Ground != CreatureGroundMovementType.None;
    public CreatureRandomMovementType Random { get; set; }
    public bool Rooted { get; set; }
    public bool Swim { get; set; }

    public override string ToString()
    {
        return $"Ground: {Ground}, Swim: {Swim}, Flight: {Flight} {(Rooted ? ", Rooted" : "")}, Chase: {Chase}, Random: {Random}, InteractionPauseTimer: {InteractionPauseTimer}";
    }
}