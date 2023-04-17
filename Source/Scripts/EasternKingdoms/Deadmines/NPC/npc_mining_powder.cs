// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.EasternKingdoms.Deadmines.NPC;

[CreatureScript(48284)]
public class NPCMiningPowder : ScriptedAI
{
    private bool _damaged = false;

    public NPCMiningPowder(Creature creature) : base(creature) { }

    public override void DamageTaken(Unit attacker, ref double damage, DamageEffectType damageType, SpellInfo spellInfo = null)
    {
        if (_damaged)
            return;

        _damaged = true;
        Me.SpellFactory.CastSpell(Me, DmSpells.EXPLODE);
        Me.DespawnOrUnsummon(TimeSpan.FromMilliseconds(100));
    }
}