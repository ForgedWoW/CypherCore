// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Items;

[Script] // 17619 - Alchemist Stone
internal class SpellItemAlchemistStone : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private bool CheckProc(ProcEventInfo eventInfo)
    {
        return eventInfo.DamageInfo.SpellInfo.SpellFamilyName == SpellFamilyNames.Potion;
    }

    private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();

        uint spellId = 0;
        var amount = (int)(eventInfo.DamageInfo.Damage * 0.4f);

        if (eventInfo.DamageInfo.SpellInfo.HasEffect(SpellEffectName.Heal))
            spellId = ItemSpellIds.ALCHEMIST_STONE_EXTRA_HEAL;
        else if (eventInfo.DamageInfo.SpellInfo.HasEffect(SpellEffectName.Energize))
            spellId = ItemSpellIds.ALCHEMIST_STONE_EXTRA_MANA;

        if (spellId == 0)
            return;

        var caster = eventInfo.ActionTarget;
        CastSpellExtraArgs args = new(aurEff);
        args.AddSpellMod(SpellValueMod.BasePoint0, amount);
        caster.SpellFactory.CastSpell((Unit)null, spellId, args);
    }
}