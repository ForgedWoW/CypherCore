// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Globals;

namespace Forged.MapServer.Entities.Creatures;

public class QuestMenu
{
    private readonly GameObjectManager _objectManager;
    private readonly List<QuestMenuItem> _questMenuItems = new();

    public QuestMenu(GameObjectManager objectManager)
    {
        _objectManager = objectManager;
    }

    public void AddMenuItem(uint questId, byte icon)
    {
        if (_objectManager.GetQuestTemplate(questId) == null)
            return;

        QuestMenuItem questMenuItem = new()
        {
            QuestId = questId,
            QuestIcon = icon
        };

        _questMenuItems.Add(questMenuItem);
    }

    public void ClearMenu()
    {
        _questMenuItems.Clear();
    }

    public QuestMenuItem GetItem(int index)
    {
        return _questMenuItems.LookupByIndex(index);
    }

    public int GetMenuItemCount()
    {
        return _questMenuItems.Count;
    }

    public bool IsEmpty()
    {
        return _questMenuItems.Empty();
    }
    private bool HasItem(uint questId)
    {
        foreach (var item in _questMenuItems)
            if (item.QuestId == questId)
                return true;

        return false;
    }
}