// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Quest;

[Script] // 49370 - Wyrmrest Defender: Destabilize Azure Dragonshrine Effect
internal class SpellQ12372DestabilizeAzureDragonshrineDummy : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleDummy(int effIndex)
    {
        if (HitCreature)
        {
            var caster = OriginalCaster;

            if (caster)
            {
                var vehicle = caster.VehicleKit1;

                if (vehicle)
                {
                    var passenger = vehicle.GetPassenger(0);

                    if (passenger)
                    {
                        var player = passenger.AsPlayer;

                        if (player)
                            player.KilledMonsterCredit(CreatureIds.WYRMREST_TEMPLE_CREDIT);
                    }
                }
            }
        }
    }
}