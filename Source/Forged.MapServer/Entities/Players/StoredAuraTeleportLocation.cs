// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Entities.Players;

class StoredAuraTeleportLocation
{
	public enum State
	{
		Unchanged,
		Changed,
		Deleted
	}

	public WorldLocation Loc { get; set; }
	public State CurrentState { get; set; }
}