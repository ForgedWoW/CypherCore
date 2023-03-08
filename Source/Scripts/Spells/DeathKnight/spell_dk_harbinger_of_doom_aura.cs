// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[SpellScript(276023)]
public class spell_dk_harbinger_of_doom_aura : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
		AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
	}

	private void OnApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		var caster = Caster;

		if (caster != null)
		{
			var player = caster.ToPlayer();

			if (player != null)
			{
				var spell = Target.FindCurrentSpellBySpellId(DeathKnightSpells.DEATH_COIL_SUDDEN_DOOM);
				spell.SpellInfo.ProcBasePpm = MathFunctions.CalculatePct(spell.SpellInfo.ProcBasePpm, 100 - 30);
			}
		}
	}

	private void OnRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		var spell = Target.FindCurrentSpellBySpellId(DeathKnightSpells.DEATH_COIL_SUDDEN_DOOM);
		spell.SpellInfo.ProcBasePpm = Global.SpellMgr.GetSpellInfo(DeathKnightSpells.DEATH_COIL_SUDDEN_DOOM).ProcBasePpm;
	}
}