// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.RealmServer.Misc;

public class QuestMenu
{
	readonly List<QuestMenuItem> _questMenuItems = new();

	public void AddMenuItem(uint QuestId, byte Icon)
	{
		if (_gameObjectManager.GetQuestTemplate(QuestId) == null)
			return;

		QuestMenuItem questMenuItem = new();

		questMenuItem.QuestId = QuestId;
		questMenuItem.QuestIcon = Icon;

		_questMenuItems.Add(questMenuItem);
	}

	public void ClearMenu()
	{
		_questMenuItems.Clear();
	}

	public int GetMenuItemCount()
	{
		return _questMenuItems.Count;
	}

	public bool IsEmpty()
	{
		return _questMenuItems.Empty();
	}

	public QuestMenuItem GetItem(int index)
	{
		return _questMenuItems.LookupByIndex(index);
	}

	bool HasItem(uint questId)
	{
		foreach (var item in _questMenuItems)
			if (item.QuestId == questId)
				return true;

		return false;
	}
}