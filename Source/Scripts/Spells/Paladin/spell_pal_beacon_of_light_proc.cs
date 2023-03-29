// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Paladin;

// 53651 - Beacon of Light Proc / Beacon of Faith (proc aura) - 177173
[SpellScript(new uint[]
{
    53651, 177173
})]
public class spell_pal_beacon_of_light_proc : AuraScript, IHasAuraEffects, IAuraCheckProc
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public bool CheckProc(ProcEventInfo eventInfo)
    {
        var ownerOfBeacon = Target;
        var targetOfBeacon = Caster;
        var targetOfHeal = eventInfo.ActionTarget;

        //if (eventInfo.GetSpellInfo() && eventInfo.GetSpellInfo()->Id != BEACON_OF_LIGHT_HEAL && eventInfo.GetSpellInfo()->Id != LIGHT_OF_THE_MARTYR && targetOfBeacon->IsWithinLOSInMap(ownerOfBeacon) && targetOfHeal->GetGUID() != targetOfBeacon->GetGUID())
        return true;
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private int GetPctBySpell(uint spellID)
    {
        var pct = 0;

        switch (spellID)
        {
            case PaladinSpells.ARCING_LIGHT_HEAL:   // Light's Hammer
            case PaladinSpells.HolyPrismTargetAlly: // Holy Prism
            case PaladinSpells.LIGHT_OF_DAWN:       // Light of Dawn
                pct = 15;                           // 15% heal from these spells

                break;
            default:
                pct = 40; // 40% heal from all other heals

                break;
        }

        return pct;
    }

    private void OnProc(AuraEffect UnnamedParameter, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
        var auraCheck = false;
        var ownerOfBeacon = Target;
        var targetOfBeacon = Caster;

        if (targetOfBeacon == null)
            return;

        var healInfo = eventInfo.HealInfo;

        if (healInfo == null)
            return;

        var bp = MathFunctions.CalculatePct(healInfo.Heal, GetPctBySpell(SpellInfo.Id));

        if (SpellInfo.Id == PaladinSpells.BEACON_OF_LIGHT_PROC_AURA && (targetOfBeacon.HasAura(PaladinSpells.BeaconOfLight) || targetOfBeacon.HasAura(PaladinSpells.BEACON_OF_VIRTUE)))
        {
            ownerOfBeacon.CastSpell(targetOfBeacon, PaladinSpells.BeaconOfLightHeal, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)bp));
            auraCheck = true;
        }

        if ((SpellInfo.Id == PaladinSpells.BEACON_OF_FAITH_PROC_AURA && targetOfBeacon.HasAura(PaladinSpells.BEACON_OF_FAITH)))
        {
            bp /= 2;
            ownerOfBeacon.CastSpell(targetOfBeacon, PaladinSpells.BeaconOfLightHeal, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)bp));
            auraCheck = true;
        }

        if (!auraCheck)
            ownerOfBeacon.RemoveAura(SpellInfo.Id);
    }
}