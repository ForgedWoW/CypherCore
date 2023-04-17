// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Scripting;

namespace Scripts.Spells.Shaman;

//NPC ID : 100099
//NPC NAME : Voodoo Totem
[CreatureScript(100099)]
public class NPCVoodooTotem : ScriptedAI
{
    public NPCVoodooTotem(Creature creature) : base(creature) { }

    public override void Reset()
    {
        Me.SpellFactory.CastSpell(null, TotemSpells.TOTEM_VOODOO_AT, true);
    }
}