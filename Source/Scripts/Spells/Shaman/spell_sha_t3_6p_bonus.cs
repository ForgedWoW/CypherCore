// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Shaman;

// 28823 - Totemic Power
[SpellScript(28823)]
internal class spell_sha_t3_6p_bonus : AuraScript, IHasAuraEffects
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
                spellId = ShamanSpells.TotemicPowerMp5;

                break;
            case PlayerClass.Mage:
            case PlayerClass.Warlock:
                spellId = ShamanSpells.TotemicPowerSpellPower;

                break;
            case PlayerClass.Hunter:
            case PlayerClass.Rogue:
                spellId = ShamanSpells.TotemicPowerAttackPower;

                break;
            case PlayerClass.Warrior:
                spellId = ShamanSpells.TotemicPowerArmor;

                break;
            default:
                return;
        }

        caster.CastSpell(target, spellId, new CastSpellExtraArgs(aurEff));
    }
}