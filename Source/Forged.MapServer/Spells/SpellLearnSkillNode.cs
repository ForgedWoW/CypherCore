// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Spells;

public class SpellLearnSkillNode
{
    public ushort Maxvalue { get; set; }
    public SkillType Skill { get; set; }
    public ushort Step { get; set; }

    public ushort Value { get; set; } // 0  - max skill value for player level
    // 0  - max skill value for player level
}