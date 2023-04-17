// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.AI.ScriptedAI;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Scripting;
using Framework.Constants;
using static Scripts.EasternKingdoms.Deadmines.Bosses.BossCaptainCookie;

namespace Scripts.EasternKingdoms.Deadmines.NPC;

[CreatureScript(47754)]
public class NPCCaptainCookieCauldron : ScriptedAI
{
    public NPCCaptainCookieCauldron(Creature pCreature) : base(pCreature)
    {
        Me.ReactState = ReactStates.Passive;
        Me.SetUnitFlag(UnitFlags.Uninteractible);
    }

    public override void Reset()
    {
        DoCast(Me, ESpell.CAULDRON_VISUAL, new Game.Spells.SpellFactory.CastSpellExtraArgs(true));
        DoCast(Me, ESpell.CAULDRON_FIRE);
        Me.SetUnitFlag(UnitFlags.Stunned);
    }
}