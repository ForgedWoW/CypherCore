﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.Entities.Creatures;

public class QuestMenu
{
    private readonly List<QuestMenuItem> _questMenuItems = new();

    public void AddMenuItem(uint QuestId, byte Icon)
    {
        if (Global.ObjectMgr.GetQuestTemplate(QuestId) == null)
            return;

        QuestMenuItem questMenuItem = new()
        {
            QuestId = QuestId,
            QuestIcon = Icon
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