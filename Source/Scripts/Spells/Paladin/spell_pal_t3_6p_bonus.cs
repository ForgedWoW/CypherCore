// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Paladin;

[SpellScript(28789)] // 28789 - Holy Power
internal class spell_pal_t3_6p_bonus : AuraScript, IHasAuraEffects
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
                spellId = PaladinSpells.HolyPowerMp5;

                break;
            case PlayerClass.Mage:
            case PlayerClass.Warlock:
                spellId = PaladinSpells.HolyPowerSpellPower;

                break;
            case PlayerClass.Hunter:
            case PlayerClass.Rogue:
                spellId = PaladinSpells.HolyPowerAttackPower;

                break;
            case PlayerClass.Warrior:
                spellId = PaladinSpells.HolyPowerArmor;

                break;
            default:
                return;
        }

        caster.CastSpell(target, spellId, new CastSpellExtraArgs(aurEff));
    }
}