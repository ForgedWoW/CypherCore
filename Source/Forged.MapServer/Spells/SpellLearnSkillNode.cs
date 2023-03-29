// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Spells;

public class SpellLearnSkillNode
{
    public SkillType Skill;
    public ushort Step;
    public ushort Value;    // 0  - max skill value for player level
    public ushort Maxvalue; // 0  - max skill value for player level
}