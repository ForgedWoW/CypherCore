// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Generic;

[Script] // 27539 - Obsidian Armor
internal class SpellGenObsidianArmor : AuraScript, IAuraCheckProc, IHasAuraEffects
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
                spellId = GenericSpellIds.HOLY;

                break;
            case SpellSchools.Fire:
                spellId = GenericSpellIds.FIRE;

                break;
            case SpellSchools.Nature:
                spellId = GenericSpellIds.NATURE;

                break;
            case SpellSchools.Frost:
                spellId = GenericSpellIds.FROST;

                break;
            case SpellSchools.Shadow:
                spellId = GenericSpellIds.SHADOW;

                break;
            case SpellSchools.Arcane:
                spellId = GenericSpellIds.ARCANE;

                break;
            default:
                return;
        }

        Target.SpellFactory.CastSpell(Target, spellId, new CastSpellExtraArgs(aurEff));
    }
}