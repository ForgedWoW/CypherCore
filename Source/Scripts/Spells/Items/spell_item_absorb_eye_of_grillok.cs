// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Items;

[Script] // 38554 - Absorb Eye of Grillok (31463: Zezzak's Shard)
internal class SpellItemAbsorbEyeOfGrillok : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(PeriodicTick, 0, AuraType.PeriodicTriggerSpell));
    }

    private void PeriodicTick(AuraEffect aurEff)
    {
        PreventDefaultAction();

        if (!Caster ||
            !Target.IsTypeId(TypeId.Unit))
            return;

        Caster.SpellFactory.CastSpell(Caster, ItemSpellIds.EYE_OF_GRILLOK, new CastSpellExtraArgs(aurEff));
        Target.AsCreature.DespawnOrUnsummon();
    }
}