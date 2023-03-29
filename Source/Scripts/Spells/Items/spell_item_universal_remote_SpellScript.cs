// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script] // 8344 - Universal Remote (Gnomish Universal Remote)
internal class spell_item_universal_remote_SpellScript : SpellScript, IHasSpellEffects
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
                Caster.CastSpell(target, ItemSpellIds.TargetLock, new CastSpellExtraArgs(CastItem));
            else if (chance < 25)
                Caster.CastSpell(target, ItemSpellIds.MobilityMalfunction, new CastSpellExtraArgs(CastItem));
            else
                Caster.CastSpell(target, ItemSpellIds.ControlMachine, new CastSpellExtraArgs(CastItem));
        }
    }
}