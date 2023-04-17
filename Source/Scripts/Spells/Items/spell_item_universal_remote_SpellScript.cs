// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Items;

[Script] // 8344 - Universal Remote (Gnomish Universal Remote)
internal class SpellItemUniversalRemoteSpellScript : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override bool Load()
    {
        if (!CastItem)
            return false;

        return true;
    }


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleDummy(int effIndex)
    {
        var target = HitUnit;

        if (target)
        {
            var chance = RandomHelper.URand(0, 99);

            if (chance < 15)
                Caster.SpellFactory.CastSpell(target, ItemSpellIds.TARGET_LOCK, new CastSpellExtraArgs(CastItem));
            else if (chance < 25)
                Caster.SpellFactory.CastSpell(target, ItemSpellIds.MOBILITY_MALFUNCTION, new CastSpellExtraArgs(CastItem));
            else
                Caster.SpellFactory.CastSpell(target, ItemSpellIds.CONTROL_MACHINE, new CastSpellExtraArgs(CastItem));
        }
    }
}