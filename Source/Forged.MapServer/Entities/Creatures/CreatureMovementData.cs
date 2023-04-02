// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Framework.Util;
using Microsoft.Extensions.Configuration;

namespace Forged.MapServer.Entities.Creatures;

public class CreatureMovementData
{
    public CreatureChaseMovementType Chase;
    public CreatureFlightMovementType Flight;
    public CreatureGroundMovementType Ground;
    public uint InteractionPauseTimer;
    public CreatureRandomMovementType Random;
    public bool Rooted;
    public bool Swim;
    public CreatureMovementData(IConfiguration configuration)
    {
        Ground = CreatureGroundMovementType.Run;
        Flight = CreatureFlightMovementType.None;
        Swim = true;
        Rooted = false;
        Chase = CreatureChaseMovementType.Run;
        Random = CreatureRandomMovementType.Walk;
        InteractionPauseTimer = configuration.GetDefaultValue("Creature.MovingStopTimeForPlayer", 3u * Time.MINUTE * Time.IN_MILLISECONDS);
    }

    public CreatureChaseMovementType GetChase()
    {
        return Chase;
    }

    public uint GetInteractionPauseTimer()
    {
        return InteractionPauseTimer;
    }

    public CreatureRandomMovementType GetRandom()
    {
        return Random;
    }

    public bool IsFlightAllowed()
    {
        return Flight != CreatureFlightMovementType.None;
    }

    public bool IsGroundAllowed()
    {
        return Ground != CreatureGroundMovementType.None;
    }

    public bool IsRooted()
    {
        return Rooted;
    }

    public bool IsSwimAllowed()
    {
        return Swim;
    }
    public override string ToString()
    {
        return $"Ground: {Ground}, Swim: {Swim}, Flight: {Flight} {(Rooted ? ", Rooted" : "")}, Chase: {Chase}, Random: {Random}, InteractionPauseTimer: {InteractionPauseTimer}";
    }
}