// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Forged.RealmServer.Spells;
using Game.Common.Entities.Players;
using Game.Common.Entities.Units;

namespace Forged.RealmServer.Scripting.Interfaces.IPlayer;

public interface IPlayerOnDealDamage : IScriptObject, IClassRescriction
{
	void OnDamage(Player caster, Unit target, ref double damage, SpellInfo spellProto);
}