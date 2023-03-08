﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using static Scripts.EasternKingdoms.Deadmines.Bosses.boss_captain_cookie;

namespace Scripts.EasternKingdoms.Deadmines.NPC;

[CreatureScript(47754)]
public class npc_captain_cookie_cauldron : ScriptedAI
{
	public npc_captain_cookie_cauldron(Creature pCreature) : base(pCreature)
	{
		me.ReactState = ReactStates.Passive;
		me.SetUnitFlag(UnitFlags.Uninteractible);
	}

	public override void Reset()
	{
		DoCast(me, eSpell.CAULDRON_VISUAL, new Game.Spells.CastSpellExtraArgs(true));
		DoCast(me, eSpell.CAULDRON_FIRE);
		me.SetUnitFlag(UnitFlags.Stunned);
	}
}