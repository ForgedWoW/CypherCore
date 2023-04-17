// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Items;

[Script] // 51640 - Taunt Flag Targeting
internal class SpellItemTauntFlagTargeting : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.CorpseSrcAreaEnemy));
        SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void FilterTargets(List<WorldObject> targets)
    {
        targets.RemoveAll(obj => !obj.IsTypeId(TypeId.Player) && !obj.IsTypeId(TypeId.Corpse));

        if (targets.Empty())
        {
            FinishCast(SpellCastResult.NoValidTargets);

            return;
        }

        targets.RandomResize(1);
    }

    private void HandleDummy(int effIndex)
    {
        // we *really* want the unit implementation here
        // it sends a packet like seen on sniff
        Caster.TextEmote(TextIds.EMOTE_PLANTS_FLAG, HitUnit, false);

        Caster.SpellFactory.CastSpell(HitUnit, ItemSpellIds.TAUNT_FLAG, true);
    }
}