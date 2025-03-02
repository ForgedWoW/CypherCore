﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Items;

[Script]
internal class spell_item_ashbringer : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Load()
	{
		return Caster.TypeId == TypeId.Player;
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(OnDummyEffect, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
	}

	private void OnDummyEffect(int effIndex)
	{
		PreventHitDefaultEffect(effIndex);

		var player = Caster.AsPlayer;

		var sound_id = RandomHelper.RAND(SoundIds.Ashbringer1,
										SoundIds.Ashbringer2,
										SoundIds.Ashbringer3,
										SoundIds.Ashbringer4,
										SoundIds.Ashbringer5,
										SoundIds.Ashbringer6,
										SoundIds.Ashbringer7,
										SoundIds.Ashbringer8,
										SoundIds.Ashbringer9,
										SoundIds.Ashbringer10,
										SoundIds.Ashbringer11,
										SoundIds.Ashbringer12);

		// Ashbringers effect (SpellIds.ID 28441) retriggers every 5 seconds, with a chance of making it say one of the above 12 sounds
		if (RandomHelper.URand(0, 60) < 1)
			player.PlayDirectSound(sound_id, player);
	}
}