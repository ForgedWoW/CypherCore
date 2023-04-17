// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Globals;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAreaTrigger;

namespace Scripts.Spells.Mage;

[Script]
public class AtMageFrozenOrb : AreaTriggerScript, IAreaTriggerOnInitialize, IAreaTriggerOnCreate, IAreaTriggerOnUpdate
{
    public uint DamageInterval;
    public bool ProcDone = false;

    public void OnCreate()
    {
        var caster = At.GetCaster();

        if (caster == null)
            return;

        var pos = new WorldLocation(caster.Location);
        At.MovePositionToFirstCollision(pos, 40.0f, 0.0f);
        At.SetDestination(4000, pos);
    }

    public void OnInitialize()
    {
        DamageInterval = 500;
    }

    public void OnUpdate(uint diff)
    {
        var caster = At.GetCaster();

        if (caster == null || !caster.IsPlayer)
            return;

        if (DamageInterval <= diff)
        {
            if (!ProcDone)
                foreach (var guid in At.InsideUnits)
                {
                    var unit = ObjectAccessor.Instance.GetUnit(caster, guid);

                    if (unit != null)
                        if (caster.IsValidAttackTarget(unit))
                        {
                            if (caster.HasAura(MageSpells.FINGERS_OF_FROST_AURA))
                                caster.SpellFactory.CastSpell(caster, MageSpells.FINGERS_OF_FROST_VISUAL_UI, true);

                            caster.SpellFactory.CastSpell(caster, MageSpells.FINGERS_OF_FROST_AURA, true);

                            // at->UpdateTimeToTarget(8000); TODO
                            ProcDone = true;

                            break;
                        }
                }

            caster.SpellFactory.CastSpell(At.Location, MageSpells.FROZEN_ORB_DAMAGE, true);
            DamageInterval = 500;
        }
        else
        {
            DamageInterval -= diff;
        }
    }
}