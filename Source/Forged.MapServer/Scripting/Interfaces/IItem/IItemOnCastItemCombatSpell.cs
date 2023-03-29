// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Spells;

namespace Forged.MapServer.Scripting.Interfaces.IItem;

public interface IItemOnCastItemCombatSpell : IScriptObject
{
    bool OnCastItemCombatSpell(Player player, Unit victim, SpellInfo spellInfo, Item item);
}