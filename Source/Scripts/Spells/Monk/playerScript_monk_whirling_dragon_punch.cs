﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IPlayer;
using Game.Spells;

namespace Scripts.Spells.Monk;

[Script]
public class playerScript_monk_whirling_dragon_punch : ScriptObjectAutoAdd, IPlayerOnCooldownEnd, IPlayerOnCooldownStart, IPlayerOnChargeRecoveryTimeStart
{
	public PlayerClass PlayerClass => PlayerClass.Monk;

	public playerScript_monk_whirling_dragon_punch() : base("playerScript_monk_whirling_dragon_punch") { }

	public void OnChargeRecoveryTimeStart(Player player, uint chargeCategoryId, ref int chargeRecoveryTime)
	{
		var risingSunKickInfo = Global.SpellMgr.GetSpellInfo(MonkSpells.RISING_SUN_KICK, Difficulty.None);

		if (risingSunKickInfo.ChargeCategoryId == chargeCategoryId)
			ApplyCasterAura(player, chargeRecoveryTime, (int)player.SpellHistory.GetRemainingCooldown(Global.SpellMgr.GetSpellInfo(MonkSpells.FISTS_OF_FURY, Difficulty.None)).TotalMilliseconds);
	}

	public void OnCooldownEnd(Player player, SpellInfo spellInfo, uint itemId, uint categoryId)
	{
		if (spellInfo.Id == MonkSpells.FISTS_OF_FURY)
			player.RemoveAura(MonkSpells.WHIRLING_DRAGON_PUNCH_CASTER_AURA);
	}

	public void OnCooldownStart(Player player, SpellInfo spellInfo, uint itemId, uint categoryId, TimeSpan cooldown, ref DateTime cooldownEnd, ref DateTime categoryEnd, ref bool onHold)
	{
		if (spellInfo.Id == MonkSpells.FISTS_OF_FURY)
		{
			var risingSunKickInfo = Global.SpellMgr.GetSpellInfo(MonkSpells.RISING_SUN_KICK, Difficulty.None);
			ApplyCasterAura(player, (int)cooldown.TotalMilliseconds, player.SpellHistory.GetChargeRecoveryTime(risingSunKickInfo.ChargeCategoryId));
		}
	}

	private void ApplyCasterAura(Player player, int cooldown1, int cooldown2)
	{
		if (cooldown1 > 0 && cooldown2 > 0)
		{
			var whirlingDragonPunchAuraDuration = (uint)Math.Min(cooldown1, cooldown2);
			player.CastSpell(player, MonkSpells.WHIRLING_DRAGON_PUNCH_CASTER_AURA, true);

			var aura = player.GetAura(MonkSpells.WHIRLING_DRAGON_PUNCH_CASTER_AURA);

			if (aura != null)
				aura.SetDuration(whirlingDragonPunchAuraDuration);
		}
	}
}