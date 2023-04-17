// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Druid;

[SpellScript(127757)]
public class AuraDruCharmWoodlandCreature : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.AoeCharm, AuraEffectHandleModes.Real));
        AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.AoeCharm, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
    }

    private void OnApply(AuraEffect unnamedParameter, AuraEffectHandleModes unnamedParameter2)
    {
        // Make targeted creature follow the player - Using pet's default dist and angle
        //if (Unit* caster = GetCaster())
        //if (Unit* target = GetTarget())
        //target->GetMotionMaster()->MoveFollow(caster, PET_FOLLOW_DIST, PET_FOLLOW_ANGLE);

        var caster = Caster;
        var target = Target;

        if (caster != null && target != null)
            target.MotionMaster.MoveFollow(caster, SharedConst.PetFollowDist, SharedConst.PetFollowAngle);
    }

    private void OnRemove(AuraEffect unnamedParameter, AuraEffectHandleModes unnamedParameter2)
    {
        //if (Unit* target = GetTarget())
        //if (target->GetMotionMaster()->GetCurrentMovementGeneratorType() == FOLLOW_MOTION_TYPE)
        //target->GetMotionMaster()->MovementExpired(true); // reset movement
        var target = Target;

        if (target != null)
            if (target.MotionMaster.GetCurrentMovementGeneratorType() == MovementGeneratorType.Follow)
                target.MotionMaster.Initialize();
    }
}