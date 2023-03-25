// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Entities;
using Forged.RealmServer.Spells;
using Forged.RealmServer.Entities.Items;
using Forged.RealmServer.Entities.Objects;
using Forged.RealmServer.Entities.Players;

namespace Forged.RealmServer.Scripting.Interfaces.IItem;

public interface IItemOnUse : IScriptObject
{
	bool OnUse(Player player, Item item, SpellCastTargets targets, ObjectGuid castId);
}