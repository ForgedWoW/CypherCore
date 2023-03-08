// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script("spell_gen_sunreaver_disguise")]
[Script("spell_gen_silver_covenant_disguise")]
internal class spell_gen_dalaran_disguise : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return spellInfo.Id switch
		{
			GenericSpellIds.SunreaverTrigger      => ValidateSpellInfo(GenericSpellIds.SunreaverFemale, GenericSpellIds.SunreaverMale),
			GenericSpellIds.SilverCovenantTrigger => ValidateSpellInfo(GenericSpellIds.SilverCovenantFemale, GenericSpellIds.SilverCovenantMale),
			_                                     => false
		};
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleScript(int effIndex)
	{
		var player = HitPlayer;

		if (player)
		{
			var gender = player.NativeGender;

			var spellId = SpellInfo.Id;

			switch (spellId)
			{
				case GenericSpellIds.SunreaverTrigger:
					spellId = gender == Gender.Female ? GenericSpellIds.SunreaverFemale : GenericSpellIds.SunreaverMale;

					break;
				case GenericSpellIds.SilverCovenantTrigger:
					spellId = gender == Gender.Female ? GenericSpellIds.SilverCovenantFemale : GenericSpellIds.SilverCovenantMale;

					break;
				default:
					break;
			}

			Caster.CastSpell(player, spellId, true);
		}
	}
}