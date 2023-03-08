﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script] // 67019 Flask of the North
internal class spell_item_flask_of_the_north : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ItemSpellIds.FlaskOfTheNorthSp, ItemSpellIds.FlaskOfTheNorthAp, ItemSpellIds.FlaskOfTheNorthStr);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
	}

	private void HandleDummy(int effIndex)
	{
		var caster = Caster;
		List<uint> possibleSpells = new();

		switch (caster.Class)
		{
			case Class.Warlock:
			case Class.Mage:
			case Class.Priest:
				possibleSpells.Add(ItemSpellIds.FlaskOfTheNorthSp);

				break;
			case Class.Deathknight:
			case Class.Warrior:
				possibleSpells.Add(ItemSpellIds.FlaskOfTheNorthStr);

				break;
			case Class.Rogue:
			case Class.Hunter:
				possibleSpells.Add(ItemSpellIds.FlaskOfTheNorthAp);

				break;
			case Class.Druid:
			case Class.Paladin:
				possibleSpells.Add(ItemSpellIds.FlaskOfTheNorthSp);
				possibleSpells.Add(ItemSpellIds.FlaskOfTheNorthStr);

				break;
			case Class.Shaman:
				possibleSpells.Add(ItemSpellIds.FlaskOfTheNorthSp);
				possibleSpells.Add(ItemSpellIds.FlaskOfTheNorthAp);

				break;
		}

		if (possibleSpells.Empty())
		{
			Log.outWarn(LogFilter.Spells, "Missing spells for class {0} in script spell_item_flask_of_the_north", caster.Class);

			return;
		}

		caster.CastSpell(caster, possibleSpells.SelectRandom(), true);
	}
}