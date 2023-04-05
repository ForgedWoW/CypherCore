// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Entities.Creatures;

public class CreatureModel
{
    public static CreatureModel DefaultInvisibleModel = new(11686, 1.0f, 1.0f);
    public static CreatureModel DefaultVisibleModel = new(17519, 1.0f, 1.0f);

    public CreatureModel() { }

    public CreatureModel(uint creatureDisplayId, float displayScale, float probability)
    {
        CreatureDisplayId = creatureDisplayId;
        DisplayScale = displayScale;
        Probability = probability;
    }

    public uint CreatureDisplayId { get; set; }
    public float DisplayScale { get; set; }
    public float Probability { get; set; }
}