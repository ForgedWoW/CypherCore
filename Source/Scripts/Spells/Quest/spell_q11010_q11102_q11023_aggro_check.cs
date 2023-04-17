// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Quest;

[Script] // 40112 Knockdown Fel Cannon: The Aggro Check
internal class SpellQ11010Q11102Q11023AggroCheck : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleDummy(int effIndex)
    {
        var playerTarget = HitPlayer;

        if (playerTarget)
            // Check if found player Target is on fly Mount or using flying form
            if (playerTarget.HasAuraType(AuraType.Fly) ||
                playerTarget.HasAuraType(AuraType.ModIncreaseMountedFlightSpeed))
                playerTarget.SpellFactory.CastSpell(playerTarget, QuestSpellIds.FLAK_CANNON_TRIGGER, new CastSpellExtraArgs(TriggerCastFlags.IgnoreCasterMountedOrOnVehicle));
    }
}