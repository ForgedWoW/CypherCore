// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Entities.Creatures;

public class FormationInfo
{
	public ulong LeaderSpawnId { get; set; }
	public float FollowDist { get; set; }
	public float FollowAngle { get; set; }
	public uint GroupAi { get; set; }
	public uint[] LeaderWaypointIDs { get; set; } = new uint[2];
}