// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Server;
using Framework.Constants;

namespace Forged.MapServer.Entities.Creatures;

public class CreatureMovementData
{
	public CreatureGroundMovementType Ground;
	public CreatureFlightMovementType Flight;
	public bool Swim;
	public bool Rooted;
	public CreatureChaseMovementType Chase;
	public CreatureRandomMovementType Random;
	public uint InteractionPauseTimer;

	public CreatureMovementData()
	{
		Ground = CreatureGroundMovementType.Run;
		Flight = CreatureFlightMovementType.None;
		Swim = true;
		Rooted = false;
		Chase = CreatureChaseMovementType.Run;
		Random = CreatureRandomMovementType.Walk;
		InteractionPauseTimer = WorldConfig.GetUIntValue(WorldCfg.CreatureStopForPlayer);
	}

	public bool IsGroundAllowed()
	{
		return Ground != CreatureGroundMovementType.None;
	}

	public bool IsSwimAllowed()
	{
		return Swim;
	}

	public bool IsFlightAllowed()
	{
		return Flight != CreatureFlightMovementType.None;
	}

	public bool IsRooted()
	{
		return Rooted;
	}

	public CreatureChaseMovementType GetChase()
	{
		return Chase;
	}

	public CreatureRandomMovementType GetRandom()
	{
		return Random;
	}

	public uint GetInteractionPauseTimer()
	{
		return InteractionPauseTimer;
	}

	public override string ToString()
	{
		return $"Ground: {Ground}, Swim: {Swim}, Flight: {Flight} {(Rooted ? ", Rooted" : "")}, Chase: {Chase}, Random: {Random}, InteractionPauseTimer: {InteractionPauseTimer}";
	}
}