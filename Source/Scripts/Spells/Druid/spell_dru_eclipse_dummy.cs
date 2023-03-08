// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Framework.Dynamic;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid;

[Script] // 79577 - Eclipse - ECLIPSE_DUMMY
internal class spell_dru_eclipse_dummy : AuraScript, IAuraOnProc, IAuraEnterLeaveCombat, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public void EnterLeaveCombat(bool isNowInCombat)
	{
		if (!isNowInCombat)
			Target.CastSpell(Target, DruidSpellIds.EclipseOoc, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
	}

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(DruidSpellIds.EclipseSolarSpellCnt, DruidSpellIds.EclipseLunarSpellCnt, DruidSpellIds.EclipseSolarAura, DruidSpellIds.EclipseLunarAura);
	}

	public void OnProc(ProcEventInfo eventInfo)
	{
		var spellInfo = eventInfo.SpellInfo;

		if (spellInfo != null)
		{
			if (spellInfo.SpellFamilyFlags & new FlagArray128(0x4, 0x0, 0x0, 0x0)) // Starfire
				OnSpellCast(DruidSpellIds.EclipseSolarSpellCnt, DruidSpellIds.EclipseLunarSpellCnt, DruidSpellIds.EclipseSolarAura);
			else if (spellInfo.SpellFamilyFlags & new FlagArray128(0x1, 0x0, 0x0, 0x0)) // Wrath
				OnSpellCast(DruidSpellIds.EclipseLunarSpellCnt, DruidSpellIds.EclipseSolarSpellCnt, DruidSpellIds.EclipseLunarAura);
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(HandleApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterApply));
		AuraEffects.Add(new AuraEffectApplyHandler(HandleRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
	}

	private void HandleApply(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		// counters are applied with a delay
		Target.Events.AddEventAtOffset(new InitializeEclipseCountersEvent(Target, (uint)aurEff.Amount), TimeSpan.FromSeconds(1));
	}

	private void HandleRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		Target.RemoveAura(DruidSpellIds.EclipseSolarSpellCnt);
		Target.RemoveAura(DruidSpellIds.EclipseLunarSpellCnt);
	}

	private void OnSpellCast(uint cntSpellId, uint otherCntSpellId, uint eclipseAuraSpellId)
	{
		var target = Target;
		var aura = target.GetAura(cntSpellId);

		if (aura != null)
		{
			uint remaining = aura.StackAmount;

			if (remaining == 0)
				return;

			if (remaining > 1)
			{
				aura.SetStackAmount((byte)(remaining - 1));
			}
			else
			{
				// cast eclipse
				target.CastSpell(target, eclipseAuraSpellId, new CastSpellExtraArgs(TriggerCastFlags.FullMask));

				// Remove stacks from other one as well
				// reset remaining power on other spellId
				target.RemoveAura(cntSpellId);
				target.RemoveAura(otherCntSpellId);
			}
		}
	}

	private class InitializeEclipseCountersEvent : BasicEvent
	{
		private readonly uint _count;
		private readonly Unit _owner;

		public InitializeEclipseCountersEvent(Unit owner, uint count)
		{
			_owner = owner;
			_count = count;
		}

		public override bool Execute(ulong etime, uint pTime)
		{
			spell_dru_eclipse_common.SetSpellCount(_owner, DruidSpellIds.EclipseSolarSpellCnt, _count);
			spell_dru_eclipse_common.SetSpellCount(_owner, DruidSpellIds.EclipseLunarSpellCnt, _count);

			return true;
		}
	}
}