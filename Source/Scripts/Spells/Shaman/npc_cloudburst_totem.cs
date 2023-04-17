// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Scripting;

namespace Scripts.Spells.Shaman;

//NPC ID : 78001
[CreatureScript(78001)]
public class NPCCloudburstTotem : ScriptedAI
{
    public NPCCloudburstTotem(Creature creature) : base(creature) { }

    public override void Reset()
    {
        if (Me.OwnerUnit)
            Me.SpellFactory.CastSpell(Me.OwnerUnit, TotemSpells.TOTEM_CLOUDBURST_EFFECT, true);
    }
}