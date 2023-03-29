// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script] // 27539 - Obsidian Armor
internal class spell_gen_obsidian_armor : AuraScript, IAuraCheckProc, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public bool CheckProc(ProcEventInfo eventInfo)
    {
        if (eventInfo.SpellInfo == null)
            return false;

        if (SharedConst.GetFirstSchoolInMask(eventInfo.SchoolMask) == SpellSchools.Normal)
            return false;

        return true;
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(OnProcEffect, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private void OnProcEffect(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();

        uint spellId;

        switch (SharedConst.GetFirstSchoolInMask(eventInfo.SchoolMask))
        {
            case SpellSchools.Holy:
                spellId = GenericSpellIds.Holy;

                break;
            case SpellSchools.Fire:
                spellId = GenericSpellIds.Fire;

                break;
            case SpellSchools.Nature:
                spellId = GenericSpellIds.Nature;

                break;
            case SpellSchools.Frost:
                spellId = GenericSpellIds.Frost;

                break;
            case SpellSchools.Shadow:
                spellId = GenericSpellIds.Shadow;

                break;
            case SpellSchools.Arcane:
                spellId = GenericSpellIds.Arcane;

                break;
            default:
                return;
        }

        Target.CastSpell(Target, spellId, new CastSpellExtraArgs(aurEff));
    }
}