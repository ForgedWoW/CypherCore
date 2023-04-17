// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Shaman;

// 28823 - Totemic Power
[SpellScript(28823)]
internal class SpellShaT36PBonus : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();

        uint spellId;
        var caster = eventInfo.Actor;
        var target = eventInfo.ProcTarget;

        switch (target.Class)
        {
            case PlayerClass.Paladin:
            case PlayerClass.Priest:
            case PlayerClass.Shaman:
            case PlayerClass.Druid:
                spellId = ShamanSpells.TOTEMIC_POWER_MP5;

                break;
            case PlayerClass.Mage:
            case PlayerClass.Warlock:
                spellId = ShamanSpells.TOTEMIC_POWER_SPELL_POWER;

                break;
            case PlayerClass.Hunter:
            case PlayerClass.Rogue:
                spellId = ShamanSpells.TOTEMIC_POWER_ATTACK_POWER;

                break;
            case PlayerClass.Warrior:
                spellId = ShamanSpells.TOTEMIC_POWER_ARMOR;

                break;
            default:
                return;
        }

        caster.SpellFactory.CastSpell(target, spellId, new CastSpellExtraArgs(aurEff));
    }
}