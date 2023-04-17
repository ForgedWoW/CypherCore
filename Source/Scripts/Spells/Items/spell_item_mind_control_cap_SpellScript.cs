// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Items;

[Script] // 13180 - Gnomish Mind Control Cap
internal class SpellItemMindControlCapSpellScript : SpellScript, IHasSpellEffects
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
        var caster = Caster;
        var target = HitUnit;

        if (target)
        {
            if (RandomHelper.randChance(95))
                caster.SpellFactory.CastSpell(target, RandomHelper.randChance(32) ? ItemSpellIds.DULLARD : ItemSpellIds.GNOMISH_MIND_CONTROL_CAP, new CastSpellExtraArgs(CastItem));
            else
                target.SpellFactory.CastSpell(caster, ItemSpellIds.GNOMISH_MIND_CONTROL_CAP, true); // backfire - 5% chance
        }
    }
}