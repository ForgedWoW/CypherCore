// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAreaTrigger;

namespace Scripts.Spells.DemonHunter;

[Script]
public class at_dh_artifact_fury_of_the_illidari : AreaTriggerScript, IAreaTriggerOnUpdate, IAreaTriggerOnRemove
{
    public void OnRemove()
    {
        var caster = At.GetCaster();

        if (caster == null || !caster.AsPlayer)
            return;

        //   int32 rageOfTheIllidari = caster->VariableStorage.GetValue<int32>("Spells.RageOfTheIllidariDamage");
        // if (!rageOfTheIllidari)
        //     return;

        // caster->VariableStorage.Set<int32>("Spells.RageOfTheIllidariDamage", 0);

        // Cannot cast custom spell on position...
        var target = caster.SummonCreature(SharedConst.WorldTrigger, At.Location, TempSummonType.TimedDespawn, TimeSpan.FromSeconds(1));

        if (target != null)
            caster.CastSpell(At, DemonHunterSpells.RAGE_OF_THE_ILLIDARI_VISUAL, true);
        //  caster->m_Events.AddEventAtOffset(() =>
        // {
        //caster->CastCustomSpell(RAGE_OF_THE_ILLIDARI_DAMAGE, SpellValueMod.BasePoint0, rageOfTheIllidari, target, TriggerCastFlags.FullMask);
        //}, TimeSpan.FromMilliseconds(750), [caster, target);
    }

    public void OnUpdate(uint diff)
    {
        var caster = At.GetCaster();

        if (caster == null)
            return;

        //  int32 timer = at->VariableStorage.GetValue<int32>("_timer") + diff;
        /* if (timer >= 490)
             {
                 at->VariableStorage.Set<int32>("_timer", timer - 490);
                 caster->CastSpell(at, FURY_OF_THE_ILLIDARI_MAINHAND, true);
                 caster->CastSpell(at, FURY_OF_THE_ILLIDARI_OFFHAND, true);
             }
             else
                 at->VariableStorage.Set("_timer", timer);*/
    }
}