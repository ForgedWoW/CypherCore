// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Generic;

[Script]
internal class SpellGenTournamentDuel : SpellScript, IHasSpellEffects
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
                if (playerTarget.HasAura(GenericSpellIds.ON_TOURNAMENT_MOUNT) &&
                    playerTarget.VehicleBase)
                    rider.SpellFactory.CastSpell(playerTarget, GenericSpellIds.MOUNTED_DUEL, true);

                return;
            }

            var unitTarget = HitUnit;

            if (unitTarget)
                if (unitTarget.Charmer &&
                    unitTarget.Charmer.IsTypeId(TypeId.Player) &&
                    unitTarget.Charmer.HasAura(GenericSpellIds.ON_TOURNAMENT_MOUNT))
                    rider.SpellFactory.CastSpell(unitTarget.Charmer, GenericSpellIds.MOUNTED_DUEL, true);
        }
    }
}