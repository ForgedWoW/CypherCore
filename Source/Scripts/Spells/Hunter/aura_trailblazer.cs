// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;
using Game.Spells.Events;

namespace Scripts.Spells.Hunter;

[SpellScript(199921)]
public class aura_trailblazer : AuraScript, IHasAuraEffects
{
	DelayedCastEvent _event;
	TimeSpan _ts;
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(EffectApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
		AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.ModIncreaseSpeed, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
	}

	private void EffectApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		RescheduleBuff();

		var player = Target.ToPlayer();

		if (player != null)
			player.SetSpeed(UnitMoveType.Run, player.GetSpeedRate(UnitMoveType.Run) + 0.15f);
	}

	private void HandleProc(AuraEffect UnnamedParameter, ProcEventInfo UnnamedParameter2)
	{
		RescheduleBuff();
	}

	private void RescheduleBuff()
	{
		var caster = Caster;
		caster.RemoveAura(HunterSpells.TRAILBLAZER_BUFF);

		if (_event == null)
		{
			_event = new DelayedCastEvent(caster, caster, HunterSpells.TRAILBLAZER_BUFF, new CastSpellExtraArgs(true));
			_ts = TimeSpan.FromSeconds(SpellInfo.GetEffect(0).BasePoints);
		}
		else
		{
			caster.Events.ScheduleAbortOnFirstMatchingEvent(e =>
			{
				if (e is DelayedCastEvent dce)
					return dce.SpellId == HunterSpells.TRAILBLAZER_BUFF;

				return false;
			});
		}

		caster.Events.AddEventAtOffset(_event, _ts);
	}

	private void OnRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		var player = Target.ToPlayer();

		if (player != null)
			player.SetSpeed(UnitMoveType.Run, player.GetSpeedRate(UnitMoveType.Run) - 0.15f);
	}
}