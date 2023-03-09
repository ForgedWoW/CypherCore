// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Entities;
using Game.Spells;

namespace Game.Chat;

[CommandGroup("cast")]
class CastCommands
{
	[Command("", RBACPermissions.CommandCast)]
	static bool HandleCastCommand(CommandHandler handler, uint spellId, [OptionalArg] string triggeredStr)
	{
		var target = handler.SelectedUnit;

		if (!target)
		{
			handler.SendSysMessage(CypherStrings.SelectCharOrCreature);

			return false;
		}

		if (!CheckSpellExistsAndIsValid(handler, spellId))
			return false;

		var triggerFlags = GetTriggerFlags(triggeredStr);

		if (!triggerFlags.HasValue)
			return false;

		handler.Session.Player.CastSpell(target, spellId, new CastSpellExtraArgs(triggerFlags.Value));

		return true;
	}

	[Command("back", RBACPermissions.CommandCastBack)]
	static bool HandleCastBackCommand(CommandHandler handler, uint spellId, [OptionalArg] string triggeredStr)
	{
		var caster = handler.SelectedCreature;

		if (!caster)
		{
			handler.SendSysMessage(CypherStrings.SelectCharOrCreature);

			return false;
		}

		if (CheckSpellExistsAndIsValid(handler, spellId))
			return false;

		var triggerFlags = GetTriggerFlags(triggeredStr);

		if (!triggerFlags.HasValue)
			return false;

		caster.CastSpell(handler.Session.Player, spellId, new CastSpellExtraArgs(triggerFlags.Value));

		return true;
	}

	[Command("dist", RBACPermissions.CommandCastDist)]
	static bool HandleCastDistCommand(CommandHandler handler, uint spellId, float dist, [OptionalArg] string triggeredStr)
	{
		if (CheckSpellExistsAndIsValid(handler, spellId))
			return false;

		var triggerFlags = GetTriggerFlags(triggeredStr);

		if (!triggerFlags.HasValue)
			return false;

		var closestPos = new Position();
		handler.Session.Player.GetClosePoint(closestPos, dist);

		handler.Session.Player.CastSpell(closestPos, spellId, new CastSpellExtraArgs(triggerFlags.Value));

		return true;
	}

	[Command("self", RBACPermissions.CommandCastSelf)]
	static bool HandleCastSelfCommand(CommandHandler handler, uint spellId, [OptionalArg] string triggeredStr)
	{
		var target = handler.SelectedUnit;

		if (!target)
		{
			handler.SendSysMessage(CypherStrings.SelectCharOrCreature);

			return false;
		}

		if (!CheckSpellExistsAndIsValid(handler, spellId))
			return false;

		var triggerFlags = GetTriggerFlags(triggeredStr);

		if (!triggerFlags.HasValue)
			return false;

		target.CastSpell(target, spellId, new CastSpellExtraArgs(triggerFlags.Value));

		return true;
	}

	[Command("target", RBACPermissions.CommandCastTarget)]
	static bool HandleCastTargetCommad(CommandHandler handler, uint spellId, [OptionalArg] string triggeredStr)
	{
		var caster = handler.SelectedCreature;

		if (!caster)
		{
			handler.SendSysMessage(CypherStrings.SelectCharOrCreature);

			return false;
		}

		if (!caster.Victim)
		{
			handler.SendSysMessage(CypherStrings.SelectedTargetNotHaveVictim);

			return false;
		}

		if (CheckSpellExistsAndIsValid(handler, spellId))
			return false;

		var triggerFlags = GetTriggerFlags(triggeredStr);

		if (!triggerFlags.HasValue)
			return false;

		caster.CastSpell(caster.Victim, spellId, new CastSpellExtraArgs(triggerFlags.Value));

		return true;
	}

	[Command("dest", RBACPermissions.CommandCastDest)]
	static bool HandleCastDestCommand(CommandHandler handler, uint spellId, float x, float y, float z, [OptionalArg] string triggeredStr)
	{
		var caster = handler.SelectedUnit;

		if (!caster)
		{
			handler.SendSysMessage(CypherStrings.SelectCharOrCreature);

			return false;
		}

		if (CheckSpellExistsAndIsValid(handler, spellId))
			return false;

		var triggerFlags = GetTriggerFlags(triggeredStr);

		if (!triggerFlags.HasValue)
			return false;

		caster.CastSpell(new Position(x, y, z), spellId, new CastSpellExtraArgs(triggerFlags.Value));

		return true;
	}

	static TriggerCastFlags? GetTriggerFlags(string triggeredStr)
	{
		if (!triggeredStr.IsEmpty())
		{
			if (triggeredStr.StartsWith("triggered")) // check if "triggered" starts with *triggeredStr (e.g. "trig", "trigger", etc.)
				return TriggerCastFlags.FullDebugMask;
			else
				return null;
		}

		return TriggerCastFlags.None;
	}

	static bool CheckSpellExistsAndIsValid(CommandHandler handler, uint spellId)
	{
		var spellInfo = Global.SpellMgr.GetSpellInfo(spellId, Difficulty.None);

		if (spellInfo == null)
		{
			handler.SendSysMessage(CypherStrings.CommandNospellfound);

			return false;
		}

		if (!Global.SpellMgr.IsSpellValid(spellInfo, handler.Player))
		{
			handler.SendSysMessage(CypherStrings.CommandSpellBroken, spellInfo.Id);

			return false;
		}

		return true;
	}
}