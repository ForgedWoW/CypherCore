// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Players;

namespace Forged.MapServer.Scripting.Interfaces.IItem;

public interface IItemOnQuestAccept : IScriptObject
{
    bool OnQuestAccept(Player player, Item item, Quest.Quest quest);
}