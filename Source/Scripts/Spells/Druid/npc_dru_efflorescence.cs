// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Scripting;
using Framework.Constants;

namespace Scripts.Spells.Druid;

[CreatureScript(47649)]
public class NPCDruEfflorescence : ScriptedAI
{
    public NPCDruEfflorescence(Creature creature) : base(creature) { }

    public override void Reset()
    {
        Me.SpellFactory.CastSpell(Me, EfflorescenceSpells.EfflorescenceDummy, true);
        Me.SetUnitFlag(UnitFlags.NonAttackable);
        Me.SetUnitFlag(UnitFlags.Uninteractible);
        Me.SetUnitFlag(UnitFlags.RemoveClientControl);
        Me.ReactState = ReactStates.Passive;
    }
}