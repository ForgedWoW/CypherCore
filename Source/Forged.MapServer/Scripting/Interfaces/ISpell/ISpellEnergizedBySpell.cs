// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Forged.MapServer.Scripting.Interfaces.ISpell;

public interface ISpellEnergizedBySpell : ISpellScript
{
	void EnergizeBySpell(Unit target, SpellInfo spellInfo, ref double amount, PowerType powerType);
}