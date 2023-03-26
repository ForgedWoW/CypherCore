// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.Globals;

public class QuestRelationResult : List<uint>
{
    private readonly bool _onlyActive;

    public QuestRelationResult() { }

    public QuestRelationResult(List<uint> range, bool onlyActive) : base(range)
    {
        _onlyActive = onlyActive;
    }

    public bool HasQuest(uint questId)
    {
        return Contains(questId) && (!_onlyActive || Quest.Quest.IsTakingQuestEnabled(questId));
    }
}