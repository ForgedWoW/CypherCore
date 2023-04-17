// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Mage;

[SpellScript(44448)]
public class SpellMagePyroblastClearcastingDriver : AuraScript, IAuraCheckProc, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public bool CheckProc(ProcEventInfo eventInfo)
    {
        var caster = Caster;

        var spellCanProc = (eventInfo.SpellInfo.Id == MageSpells.SCORCH || eventInfo.SpellInfo.Id == MageSpells.FIREBALL || eventInfo.SpellInfo.Id == MageSpells.FIRE_BLAST || eventInfo.SpellInfo.Id == MageSpells.FLAMESTRIKE || eventInfo.SpellInfo.Id == MageSpells.PYROBLAST || eventInfo.SpellInfo.Id == MageSpells.PHOENIX_FLAMES || (eventInfo.SpellInfo.Id == MageSpells.DRAGON_BREATH && caster.HasAura(MageSpells.ALEXSTRASZAS_FURY)));

        if (spellCanProc)
            return true;

        return false;
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private void HandleProc(AuraEffect unnamedParameter, ProcEventInfo eventInfo)
    {
        var procCheck = false;

        var caster = Caster;

        if ((eventInfo.HitMask & ProcFlagsHit.Normal) != 0)
        {
            if (caster.HasAura(MageSpells.HEATING_UP))
                caster.RemoveAura(MageSpells.HEATING_UP);

            return;
        }

        if (!caster.HasAura(MageSpells.HEATING_UP) && !caster.HasAura(MageSpells.HOT_STREAK))
        {
            caster.SpellFactory.CastSpell(caster, MageSpells.HEATING_UP, true);

            procCheck = true;

            var burn = caster.GetAuraEffect(MageSpells.CONTROLLED_BURN, 0);

            if (burn != null)
                if (RandomHelper.randChance(burn.Amount))
                    procCheck = false;
        }


        if (caster.HasAura(MageSpells.HEATING_UP) && !caster.HasAura(MageSpells.HOT_STREAK) && !procCheck)
        {
            caster.RemoveAura(MageSpells.HEATING_UP);
            caster.SpellFactory.CastSpell(caster, MageSpells.HOT_STREAK, true);
        }
    }
}