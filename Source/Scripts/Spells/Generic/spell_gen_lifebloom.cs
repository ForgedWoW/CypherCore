// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Generic;

[Script("spell_hexlord_lifebloom", GenericSpellIds.HEXLORD_MALACRASS)]
[Script("spell_tur_ragepaw_lifebloom", GenericSpellIds.TURRAGE_PAW)]
[Script("spell_cenarion_scout_lifebloom", GenericSpellIds.CENARION_SCOUT)]
[Script("spell_twisted_visage_lifebloom", GenericSpellIds.TWISTED_VISAGE)]
[Script("spell_faction_champion_dru_lifebloom", GenericSpellIds.FACTION_CHAMPIONS_DRU)]
internal class SpellGenLifebloom : AuraScript, IHasAuraEffects
{
    private readonly uint _spellId;

    public SpellGenLifebloom(uint spellId)
    {
        _spellId = spellId;
    }

    public List<IAuraEffectHandler> AuraEffects { get; } = new();


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
        Target.SpellFactory.CastSpell(Target, _spellId, new CastSpellExtraArgs(aurEff).SetOriginalCaster(CasterGUID));
    }
}