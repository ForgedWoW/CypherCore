// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Warlock;

// 145072 - Item - Warlock T16 2P Bonus
[SpellScript(145072)]
internal class SpellWarlockT16Demo2P : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private void OnProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        uint procSpellId = 0;
        var spellInfo = eventInfo.DamageInfo.SpellInfo;

        if (spellInfo != null)
            procSpellId = spellInfo.Id;

        double chance = 0;
        uint triggeredSpellId = 0;

        switch (procSpellId)
        {
            case WarlockSpells.CONFLAGRATE:
            case WarlockSpells.CONFLAGRATE_FIRE_AND_BRIMSTONE:
                chance = aurEff.SpellInfo.GetEffect(3).BasePoints;
                triggeredSpellId = 145075; // Destructive Influence

                break;
            case WarlockSpells.UNSTABLE_AFFLICTION:
                chance = aurEff.SpellInfo.GetEffect(1).BasePoints;
                triggeredSpellId = 145082; // Empowered Grasp

                break;
            case WarlockSpells.SOUL_FIRE:
            case WarlockSpells.SOUL_FIRE_METAMORPHOSIS:
                chance = aurEff.SpellInfo.GetEffect(3).BasePoints;
                triggeredSpellId = 145085; // Fiery Wrath

                break;
            default:
                return;
        }

        if (!RandomHelper.randChance(chance))
            return;

        var caster = OwnerAsUnit;
        caster.SpellFactory.CastSpell(caster, triggeredSpellId, true);
    }
}