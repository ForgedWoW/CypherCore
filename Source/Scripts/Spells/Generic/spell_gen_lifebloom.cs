// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script("spell_hexlord_lifebloom", GenericSpellIds.HexlordMalacrass)]
[Script("spell_tur_ragepaw_lifebloom", GenericSpellIds.TurragePaw)]
[Script("spell_cenarion_scout_lifebloom", GenericSpellIds.CenarionScout)]
[Script("spell_twisted_visage_lifebloom", GenericSpellIds.TwistedVisage)]
[Script("spell_faction_champion_dru_lifebloom", GenericSpellIds.FactionChampionsDru)]
internal class spell_gen_lifebloom : AuraScript, IHasAuraEffects
{
	private readonly uint _spellId;

	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public spell_gen_lifebloom(uint spellId)
	{
		_spellId = spellId;
	}


	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(AfterRemove, 0, AuraType.PeriodicHeal, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
	}

	private void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		// Final heal only on duration end
		if (TargetApplication.RemoveMode != AuraRemoveMode.Expire &&
			TargetApplication.RemoveMode != AuraRemoveMode.EnemySpell)
			return;

		// final heal
		Target.CastSpell(Target, _spellId, new CastSpellExtraArgs(aurEff).SetOriginalCaster(CasterGUID));
	}
}