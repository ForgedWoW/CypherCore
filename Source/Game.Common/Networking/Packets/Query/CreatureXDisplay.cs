﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Networking.Packets;

public class CreatureXDisplay
{
	public uint CreatureDisplayID;
	public float Scale = 1.0f;
	public float Probability = 1.0f;

	public CreatureXDisplay(uint creatureDisplayID, float displayScale, float probability)
	{
		CreatureDisplayID = creatureDisplayID;
		Scale = displayScale;
		Probability = probability;
	}
}