// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Paladin;

[SpellScript(28789)] // 28789 - Holy Power
internal class SpellPalT36PBonus : AuraScript, IHasAuraEffects
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
                spellId = PaladinSpells.HOLY_POWER_MP5;

                break;
            case PlayerClass.Mage:
            case PlayerClass.Warlock:
                spellId = PaladinSpells.HOLY_POWER_SPELL_POWER;

                break;
            case PlayerClass.Hunter:
            case PlayerClass.Rogue:
                spellId = PaladinSpells.HOLY_POWER_ATTACK_POWER;

                break;
            case PlayerClass.Warrior:
                spellId = PaladinSpells.HOLY_POWER_ARMOR;

                break;
            default:
                return;
        }

        caster.SpellFactory.CastSpell(target, spellId, new CastSpellExtraArgs(aurEff));
    }
}