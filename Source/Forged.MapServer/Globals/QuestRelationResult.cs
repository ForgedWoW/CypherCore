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