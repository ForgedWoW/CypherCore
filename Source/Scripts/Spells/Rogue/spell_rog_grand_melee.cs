// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Rogue;

[Script] // 193358 - Grand Melee
internal class SpellRogGrandMelee : AuraScript, IAuraCheckProc, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public bool CheckProc(ProcEventInfo eventInfo)
    {
        var procSpell = eventInfo.ProcSpell;

        return procSpell && procSpell.HasPowerTypeCost(PowerType.ComboPoints);
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private void HandleProc(AuraEffect aurEff, ProcEventInfo procInfo)
    {
        var procSpell = procInfo.ProcSpell;
        var amount = aurEff.Amount * procSpell.GetPowerTypeCostAmount(PowerType.ComboPoints).Value * 1000;

        var target = Target;

        if (target != null)
        {
            var aura = target.GetAura(RogueSpells.SliceAndDice);

            if (aura != null)
            {
                aura.SetDuration(aura.Duration + amount);
            }
            else
            {
                CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
                args.AddSpellMod(SpellValueMod.Duration, amount);
                target.SpellFactory.CastSpell(target, RogueSpells.SliceAndDice, args);
            }
        }
    }
}