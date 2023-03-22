// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;

namespace Game.Misc;

public class InteractionData
{
	public ObjectGuid SourceGuid { get; set; }
	public uint TrainerId { get; set; }
	public uint PlayerChoiceId { get; set; }

	public void Reset()
	{
		SourceGuid.Clear();
		TrainerId = 0;
	}
}