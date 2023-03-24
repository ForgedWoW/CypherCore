// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Entities.Creatures;

public class CreatureModel
{
	public static CreatureModel DefaultInvisibleModel = new(11686, 1.0f, 1.0f);
	public static CreatureModel DefaultVisibleModel = new(17519, 1.0f, 1.0f);

	public uint CreatureDisplayId;
	public float DisplayScale;
	public float Probability;

	public CreatureModel() { }

	public CreatureModel(uint creatureDisplayId, float displayScale, float probability)
	{
		CreatureDisplayId = creatureDisplayId;
		DisplayScale = displayScale;
		Probability = probability;
	}
}
