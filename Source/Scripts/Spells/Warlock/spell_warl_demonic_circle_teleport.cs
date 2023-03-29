﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock;

[SpellScript(48020)] // 48020 - Demonic Circle: Teleport
internal class spell_warl_demonic_circle_teleport : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(HandleTeleport, 0, AuraType.MechanicImmunity, AuraEffectHandleModes.Real, AuraScriptHookType.EffectApply));
    }

    private void HandleTeleport(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        var player = Target.AsPlayer;

        if (player)
        {
            var circle = player.GetGameObject(WarlockSpells.DEMONIC_CIRCLE_SUMMON);

            if (circle)
            {
                if (player.HasAura(WarlockSpells.ABYSS_WALKER))
                    player.AddAura(WarlockSpells.ABYSS_WALKER_BUFF);

                player.NearTeleportTo(circle.Location.X, circle.Location.Y, circle.Location.Z, circle.Location.Orientation);
                player.RemoveMovementImpairingAuras(false);
            }
        }
    }
}