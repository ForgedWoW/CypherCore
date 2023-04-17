// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Scripting;

namespace Scripts.Spells.Shaman;

//100943
[CreatureScript(100943)]
public class NPCEarthenShieldTotem : ScriptedAI
{
    public NPCEarthenShieldTotem(Creature creature) : base(creature) { }

    public override void Reset()
    {
        Me.SpellFactory.CastSpell(Me, ShamanSpells.AT_EARTHEN_SHIELD_TOTEM, true);
    }
}