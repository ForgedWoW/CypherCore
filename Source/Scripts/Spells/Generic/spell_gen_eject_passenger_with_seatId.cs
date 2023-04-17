// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Generic;

[Script("spell_gen_eject_passenger_1", 0)]
[Script("spell_gen_eject_passenger_3", 2)]
internal class SpellGenEjectPassengerWithSeatId : SpellScript, IHasSpellEffects
{
    private readonly sbyte _seatId;

    public SpellGenEjectPassengerWithSeatId(sbyte seatId)
    {
        _seatId = seatId;
    }

    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(EjectPassenger, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
    }

    private void EjectPassenger(int effIndex)
    {
        var vehicle = HitUnit.VehicleKit1;

        if (vehicle != null)
        {
            var passenger = vehicle.GetPassenger(_seatId);

            passenger?.ExitVehicle();
        }
    }
}