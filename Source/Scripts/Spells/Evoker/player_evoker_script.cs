// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Framework.Database;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IUnit;

namespace Scripts.Spells.Evoker;

[Script]
internal class player_evoker_script : ScriptObjectAutoAdd, IUnitOnHeal, IUnitOnDamage
{
	public PlayerClass PlayerClass { get; } = PlayerClass.Evoker;

	public player_evoker_script() : base("player_evoker_script") { }

	public void OnHeal(HealInfo healInfo, ref uint gain)
	{
		EmeraldCommunion(healInfo, gain);
		Reversion(healInfo);
    }

    private void Reversion(HealInfo healInfo)
    {
		if (healInfo.SpellInfo.Id == EvokerSpells.BRONZE_REVERSION
            && healInfo.IsCritical
			&& healInfo.Target.TryGetAura(EvokerSpells.BRONZE_REVERSION, out var aura))
			aura.ModDuration(aura.GetEffect(0).Period);
    }

    private void EmeraldCommunion(HealInfo healInfo, uint gain)
	{
		if (healInfo.SpellInfo.Id == EvokerSpells.GREEN_EMERALD_COMMUNION &&
			healInfo.Healer == healInfo.Target &&
			gain < healInfo.Heal)
		{
			var healer = healInfo.Healer;
			// get targets
			var targetList = new List<Unit>();
			healer.GetAlliesWithinRange(targetList, 100);
			targetList.RemoveIf(a => a.IsFullHealth);

			if (targetList.Count == 0)
				return;

			// reduce targetList to the number allowed
			targetList.RandomResize(1);

			// cast on targets
			HealInfo info = new(healer,
								targetList[0],
								healInfo.Heal - gain,
								healInfo.SpellInfo,
								healInfo.SchoolMask);

			Unit.DealHeal(info);
		}
	}

    public void OnDamage(Unit attacker, Unit victim, ref double damage)
    {
		RenewingBlaze(victim, damage);
    }

    private void RenewingBlaze(Unit victim, double damage)
    {
		if (victim != null && victim.TryGetAsPlayer(out var player) && player.HasAura(EvokerSpells.RED_RENEWING_BLAZE))
		{
			if (!player.TryGetAura(EvokerSpells.RED_RENEWING_BLAZE_AURA, out var rnAura))
                rnAura = player.AddAura(EvokerSpells.RED_RENEWING_BLAZE_AURA);

			var eff = rnAura.GetEffect(0);
			var remainingTicks = rnAura.Duration / eff.Period;
			var newTotal = damage + (eff.Amount * remainingTicks); // add new damage to the remaining total amount

			//          total healed           number of ticks
			eff.SetAmount(newTotal / (rnAura.MaxDuration / eff.Period));
			rnAura.SetDuration(rnAura.MaxDuration);
		}
    }
}