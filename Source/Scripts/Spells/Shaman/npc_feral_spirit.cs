// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Framework.Constants;

namespace Scripts.Spells.Shaman;

// 29264
[CreatureScript(29264)]
public class NPCFeralSpirit : ScriptedAI
{
    public NPCFeralSpirit(Creature pCreature) : base(pCreature) { }

    public override void DamageDealt(Unit unnamedParameter, ref double damage, DamageEffectType unnamedParameter3)
    {
        var tempSum = Me.ToTempSummon();

        if (tempSum != null)
        {
            var owner = tempSum.OwnerUnit;

            if (owner != null)
                if (owner.HasAura(ShamanSpells.FERAL_SPIRIT_ENERGIZE_DUMMY))
                    if (owner.GetPower(PowerType.Maelstrom) <= 95)
                        owner.ModifyPower(PowerType.Maelstrom, +5);
        }
    }
}