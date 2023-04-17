// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Priest;

[Script] // 41635 - Prayer of Mending (Aura) - PRAYER_OF_MENDING_AURA
internal class SpellPriPrayerOfMendingAuraScript : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleHeal, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private void HandleHeal(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        // Caster: player (priest) that cast the Prayer of Mending
        // Target: player that currently has Prayer of Mending aura on him
        var target = Target;
        var caster = Caster;

        if (caster != null)
        {
            // Cast the spell to heal the owner
            caster.SpellFactory.CastSpell(target, PriestSpells.PRAYER_OF_MENDING_HEAL, new CastSpellExtraArgs(aurEff));

            // Only cast Jump if stack is higher than 0
            int stackAmount = StackAmount;

            if (stackAmount > 1)
            {
                CastSpellExtraArgs args = new(aurEff);
                args.OriginalCaster = caster.GUID;
                args.AddSpellMod(SpellValueMod.BasePoint0, stackAmount - 1);
                target.SpellFactory.CastSpell(target, PriestSpells.PRAYER_OF_MENDING_JUMP, args);
            }

            Remove();
        }
    }
}