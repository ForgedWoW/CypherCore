// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Quest;

public struct QuestRewardDisplaySpell
{
    public uint SpellId;
    public uint PlayerConditionId;

    public QuestRewardDisplaySpell(uint spellId, uint playerConditionId)
    {
        SpellId = spellId;
        PlayerConditionId = playerConditionId;
    }
}