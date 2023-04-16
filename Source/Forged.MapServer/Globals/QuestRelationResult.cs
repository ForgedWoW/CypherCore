// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Pools;

namespace Forged.MapServer.Globals;

public class QuestRelationResult : List<uint>
{
    private readonly bool _onlyActive;
    private readonly QuestPoolManager _questPoolManager;

    public QuestRelationResult() { }

    public QuestRelationResult(List<uint> range, bool onlyActive, QuestPoolManager questPoolManager) : base(range)
    {
        _onlyActive = onlyActive;
        _questPoolManager = questPoolManager;
    }

    public bool HasQuest(uint questId)
    {
        return Contains(questId) && (!_onlyActive || _questPoolManager.IsQuestActive(questId));
    }
}