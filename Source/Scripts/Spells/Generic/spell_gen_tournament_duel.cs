﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_tournament_duel : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleScriptEffect, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleScriptEffect(int effIndex)
    {
        var rider = Caster.Charmer;

        if (rider)
        {
            var playerTarget = HitPlayer;

            if (playerTarget)
            {
                if (playerTarget.HasAura(GenericSpellIds.OnTournamentMount) &&
                    playerTarget.VehicleBase)
                    rider.CastSpell(playerTarget, GenericSpellIds.MountedDuel, true);

                return;
            }

            var unitTarget = HitUnit;

            if (unitTarget)
                if (unitTarget.Charmer &&
                    unitTarget.Charmer.IsTypeId(TypeId.Player) &&
                    unitTarget.Charmer.HasAura(GenericSpellIds.OnTournamentMount))
                    rider.CastSpell(unitTarget.Charmer, GenericSpellIds.MountedDuel, true);
        }
    }
}