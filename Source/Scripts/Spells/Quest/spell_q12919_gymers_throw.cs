// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Quest;

[Script] // 55421 - Gymer's Throw
internal class SpellQ12919GymersThrow : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleScript(int effIndex)
    {
        var caster = Caster;

        if (caster.IsVehicle)
        {
            var passenger = caster.VehicleKit1.GetPassenger(1);

            if (passenger)
            {
                passenger.ExitVehicle();
                caster.SpellFactory.CastSpell(passenger, QuestSpellIds.VARGUL_EXPLOSION, true);
            }
        }
    }
}