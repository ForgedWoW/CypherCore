// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;
using Framework.Dynamic;

namespace Scripts.Spells.Generic;

[Script] // 28764 - Adaptive Warding (Frostfire Regalia Set)
internal class SpellGenAdaptiveWarding : AuraScript, IAuraCheckProc, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public bool CheckProc(ProcEventInfo eventInfo)
    {
        if (eventInfo.SpellInfo == null)
            return false;

        // find Mage Armor
        if (Target.GetAuraEffect(AuraType.ModManaRegenInterrupt, SpellFamilyNames.Mage, new FlagArray128(0x10000000, 0x0, 0x0)) == null)
            return false;

        switch (SharedConst.GetFirstSchoolInMask(eventInfo.SchoolMask))
        {
            case SpellSchools.Normal:
            case SpellSchools.Holy:
                return false;
        }

        return true;
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();

        uint spellId;

        switch (SharedConst.GetFirstSchoolInMask(eventInfo.SchoolMask))
        {
            case SpellSchools.Fire:
                spellId = GenericSpellIds.GEN_ADAPTIVE_WARDING_FIRE;

                break;
            case SpellSchools.Nature:
                spellId = GenericSpellIds.GEN_ADAPTIVE_WARDING_NATURE;

                break;
            case SpellSchools.Frost:
                spellId = GenericSpellIds.GEN_ADAPTIVE_WARDING_FROST;

                break;
            case SpellSchools.Shadow:
                spellId = GenericSpellIds.GEN_ADAPTIVE_WARDING_SHADOW;

                break;
            case SpellSchools.Arcane:
                spellId = GenericSpellIds.GEN_ADAPTIVE_WARDING_ARCANE;

                break;
            default:
                return;
        }

        Target.SpellFactory.CastSpell(Target, spellId, new CastSpellExtraArgs(aurEff));
    }
}