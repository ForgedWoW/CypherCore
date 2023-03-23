// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Forged.RealmServer.Spells;
using Game.Common.Entities.Items;
using Game.Common.Entities.Players;
using Game.Common.Entities.Units;

namespace Forged.RealmServer.Scripting.Interfaces.IItem;

public interface IItemOnCastItemCombatSpell : IScriptObject
{
	bool OnCastItemCombatSpell(Player player, Unit victim, SpellInfo spellInfo, Item item);
}