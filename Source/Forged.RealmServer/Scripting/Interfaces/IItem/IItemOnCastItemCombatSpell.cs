// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Entities;
using Forged.RealmServer.Spells;
using Forged.RealmServer.Entities.Items;
using Forged.RealmServer.Entities.Players;
using Forged.RealmServer.Entities.Units;

namespace Forged.RealmServer.Scripting.Interfaces.IItem;

public interface IItemOnCastItemCombatSpell : IScriptObject
{
	bool OnCastItemCombatSpell(Player player, Unit victim, SpellInfo spellInfo, Item item);
}