// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Warlock;

// 32863 - Seed of Corruption
// 36123 - Seed of Corruption
// 38252 - Seed of Corruption
// 39367 - Seed of Corruption
// 44141 - Seed of Corruption
// 70388 - Seed of Corruption
[SpellScript(new uint[]
{
    32863, 36123, 38252, 39367, 44141, 70388
})] // Monster spells, triggered only on amount drop (not on death)
internal class SpellWarlSeedOfCorruptionGeneric : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 1, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
        var damageInfo = eventInfo.DamageInfo;

        if (damageInfo == null ||
            damageInfo.Damage == 0)
            return;

        var amount = aurEff.Amount - (int)damageInfo.Damage;

        if (amount > 0)
        {
            aurEff.SetAmount(amount);

            return;
        }

        Remove();

        var caster = Caster;

        if (!caster)
            return;

        caster.SpellFactory.CastSpell(eventInfo.ActionTarget, WarlockSpells.SEED_OF_CORRUPTION_GENERIC, new CastSpellExtraArgs(aurEff));
    }
}