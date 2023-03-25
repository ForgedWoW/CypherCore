// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Chrono;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Objects.Update;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Globals;
using Forged.MapServer.Maps;
using Forged.MapServer.Maps.GridNotifiers;
using Forged.MapServer.Maps.Grids;
using Forged.MapServer.Maps.Workers;
using Forged.MapServer.Networking.Packets.Misc;
using Forged.MapServer.Networking.Packets.Spell;
using Forged.MapServer.Phasing;
using Forged.MapServer.Server;
using Forged.MapServer.Spells;
using Forged.MapServer.Weather;
using Framework.Constants;
using Framework.Database;
using Framework.IO;

namespace Forged.MapServer.Chat.Commands;

class MiscCommands
{
	// Teleport to Player
	[CommandNonGroup("appear", RBACPermissions.CommandAppear)]
	static bool HandleAppearCommand(CommandHandler handler, StringArguments args)
	{
		if (!handler.ExtractPlayerTarget(args, out var target, out var targetGuid, out var targetName))
			return false;

		var _player = handler.Session.Player;

		if (target == _player || targetGuid == _player.GUID)
		{
			handler.SendSysMessage(CypherStrings.CantTeleportSelf);

			return false;
		}

		if (target)
		{
			// check online security
			if (handler.HasLowerSecurity(target, ObjectGuid.Empty))
				return false;

			var chrNameLink = handler.PlayerLink(targetName);

			var map = target.Map;

			if (map.IsBattlegroundOrArena)
			{
				// only allow if gm mode is on
				if (!_player.IsGameMaster)
				{
					handler.SendSysMessage(CypherStrings.CannotGoToBgGm, chrNameLink);

					return false;
				}
				// if both players are in different bgs
				else if (_player.BattlegroundId != 0 && _player.BattlegroundId != target.BattlegroundId)
				{
					_player.LeaveBattleground(false); // Note: should be changed so _player gets no Deserter debuff
				}

				// all's well, set bg id
				// when porting out from the bg, it will be reset to 0
				_player.SetBattlegroundId(target.BattlegroundId, target.BattlegroundTypeId);

				// remember current position as entry point for return at bg end teleportation
				if (!_player.Map.IsBattlegroundOrArena)
					_player.SetBattlegroundEntryPoint();
			}
			else if (map.IsDungeon)
			{
				// we have to go to instance, and can go to player only if:
				//   1) we are in his group (either as leader or as member)
				//   2) we are not bound to any group and have GM mode on
				if (_player.Group)
				{
					// we are in group, we can go only if we are in the player group
					if (_player.Group != target.Group)
					{
						handler.SendSysMessage(CypherStrings.CannotGoToInstParty, chrNameLink);

						return false;
					}
				}
				else
				{
					// we are not in group, let's verify our GM mode
					if (!_player.IsGameMaster)
					{
						handler.SendSysMessage(CypherStrings.CannotGoToInstGm, chrNameLink);

						return false;
					}
				}

				if (map.IsRaid)
				{
					_player.RaidDifficultyId = target.RaidDifficultyId;
					_player.LegacyRaidDifficultyId = target.LegacyRaidDifficultyId;
				}
				else
				{
					_player.DungeonDifficultyId = target.DungeonDifficultyId;
				}
			}

			handler.SendSysMessage(CypherStrings.AppearingAt, chrNameLink);

			// stop flight if need
			if (_player.IsInFlight)
				_player.FinishTaxiFlight();
			else
				_player.SaveRecallPosition(); // save only in non-flight case

			// to point to see at target with same orientation
			var pos = new Position();
			target.GetClosePoint(pos, _player.CombatReach, 1.0f);
			pos.Orientation = _player.Location.GetAbsoluteAngle(target.Location);
			_player.TeleportTo(target.Location.MapId, pos, TeleportToOptions.GMMode, target.InstanceId);
			PhasingHandler.InheritPhaseShift(_player, target);
			_player.UpdateObjectVisibility();
		}
		else
		{
			// check offline security
			if (handler.HasLowerSecurity(null, targetGuid))
				return false;

			var nameLink = handler.PlayerLink(targetName);

			handler.SendSysMessage(CypherStrings.AppearingAt, nameLink);

			// to point where player stay (if loaded)
			if (!Player.LoadPositionFromDB(out var loc, out _, targetGuid))
				return false;

			// stop flight if need
			if (_player.IsInFlight)
				_player.FinishTaxiFlight();
			else
				_player.SaveRecallPosition(); // save only in non-flight case

			loc.Orientation = _player.Location.Orientation;
			_player.TeleportTo(loc);
		}

		return true;
	}

	[CommandNonGroup("bank", RBACPermissions.CommandBank)]
	static bool HandleBankCommand(CommandHandler handler)
	{
		handler.Session.SendShowBank(handler.Session.Player.GUID);

		return true;
	}

	[CommandNonGroup("bindsight", RBACPermissions.CommandBindsight)]
	static bool HandleBindSightCommand(CommandHandler handler)
	{
		var unit = handler.SelectedUnit;

		if (!unit)
			return false;

		handler.Session.Player.CastSpell(unit, 6277, true);

		return true;
	}

	[CommandNonGroup("combatstop", RBACPermissions.CommandCombatstop, true)]
	static bool HandleCombatStopCommand(CommandHandler handler, StringArguments args)
	{
		Player target = null;

		if (!args.Empty())
		{
			target = Global.ObjAccessor.FindPlayerByName(args.NextString());

			if (!target)
			{
				handler.SendSysMessage(CypherStrings.PlayerNotFound);

				return false;
			}
		}

		if (!target)
			if (!handler.ExtractPlayerTarget(args, out target))
				return false;

		// check online security
		if (handler.HasLowerSecurity(target, ObjectGuid.Empty))
			return false;

		target.CombatStop();

		return true;
	}

	[CommandNonGroup("cometome", RBACPermissions.CommandCometome)]
	static bool HandleComeToMeCommand(CommandHandler handler)
	{
		var caster = handler.SelectedCreature;

		if (!caster)
		{
			handler.SendSysMessage(CypherStrings.SelectCreature);

			return false;
		}

		var player = handler.Session.Player;
		caster.MotionMaster.MovePoint(0, player.Location.X, player.Location.Y, player.Location.Z);

		return true;
	}

	[CommandNonGroup("commands", RBACPermissions.CommandCommands, true)]
	static bool HandleCommandsCommand(CommandHandler handler)
	{
		ChatCommandNode.SendCommandHelpFor(handler, "");

		return true;
	}

	[CommandNonGroup("damage", RBACPermissions.CommandDamage)]
	static bool HandleDamageCommand(CommandHandler handler, StringArguments args)
	{
		if (args.Empty())
			return false;

		var str = args.NextString();

		if (str == "go")
		{
			var guidLow = args.NextUInt64();

			if (guidLow == 0)
			{
				handler.SendSysMessage(CypherStrings.BadValue);

				return false;
			}

			var damage = args.NextInt32();

			if (damage == 0)
			{
				handler.SendSysMessage(CypherStrings.BadValue);

				return false;
			}

			var player = handler.Session.Player;

			if (player)
			{
				var go = handler.GetObjectFromPlayerMapByDbGuid(guidLow);

				if (!go)
				{
					handler.SendSysMessage(CypherStrings.CommandObjnotfound, guidLow);

					return false;
				}

				if (!go.IsDestructibleBuilding)
				{
					handler.SendSysMessage(CypherStrings.InvalidGameobjectType);

					return false;
				}

				go.ModifyHealth(-damage, player);
				handler.SendSysMessage(CypherStrings.GameobjectDamaged, go.GetName(), guidLow, -damage, go.GoValue.Building.Health);
			}

			return true;
		}

		var target = handler.SelectedUnit;

		if (!target || handler.Session.Player.Target.IsEmpty)
		{
			handler.SendSysMessage(CypherStrings.SelectCharOrCreature);

			return false;
		}

		var player_ = target.AsPlayer;

		if (player_)
			if (handler.HasLowerSecurity(player_, ObjectGuid.Empty, false))
				return false;

		if (!target.IsAlive)
			return true;

		if (!double.TryParse(str, out var damage_int))
			return false;

		if (damage_int <= 0)
			return true;

		var damage_ = damage_int;

		var schoolStr = args.NextString();

		var attacker = handler.Session.Player;

		// flat melee damage without resistence/etc reduction
		if (string.IsNullOrEmpty(schoolStr))
		{
			damage_ = Unit.DealDamage(attacker, target, damage_, null, DamageEffectType.Direct, SpellSchoolMask.Normal, null, false);

			if (target != attacker)
				attacker.SendAttackStateUpdate(HitInfo.AffectsVictim, target, SpellSchoolMask.Normal, damage_, 0, 0, VictimState.Hit, 0);

			return true;
		}

		if (!int.TryParse(schoolStr, out var school) || school >= (int)SpellSchools.Max)
			return false;

		var schoolmask = (SpellSchoolMask)(1 << school);

		if (Unit.IsDamageReducedByArmor(schoolmask))
			damage_ = Unit.CalcArmorReducedDamage(handler.Player, target, damage_, null, WeaponAttackType.BaseAttack);

		var spellStr = args.NextString();

		// melee damage by specific school
		if (string.IsNullOrEmpty(spellStr))
		{
			DamageInfo dmgInfo = new(attacker, target, damage_, null, schoolmask, DamageEffectType.SpellDirect, WeaponAttackType.BaseAttack);
			Unit.CalcAbsorbResist(dmgInfo);

			if (dmgInfo.Damage == 0)
				return true;

			damage_ = dmgInfo.Damage;

			var absorb = dmgInfo.Absorb;
			var resist = dmgInfo.Resist;
			Unit.DealDamageMods(attacker, target, ref damage_, ref absorb);
			damage_ = Unit.DealDamage(attacker, target, damage_, null, DamageEffectType.Direct, schoolmask, null, false);
			attacker.SendAttackStateUpdate(HitInfo.AffectsVictim, target, schoolmask, damage_, absorb, resist, VictimState.Hit, 0);

			return true;
		}

		// non-melee damage
		// number or [name] Shift-click form |color|Hspell:spell_id|h[name]|h|r or Htalent form
		var spellid = handler.ExtractSpellIdFromLink(args);

		if (spellid == 0)
			return false;

		var spellInfo = Global.SpellMgr.GetSpellInfo(spellid, attacker.Map.DifficultyID);

		if (spellInfo == null)
			return false;

		SpellNonMeleeDamage damageInfo = new(attacker, target, spellInfo, new SpellCastVisual(spellInfo.GetSpellXSpellVisualId(attacker), 0), spellInfo.SchoolMask)
        {
            Damage = damage_
        };

        Unit.DealDamageMods(damageInfo.Attacker, damageInfo.Target, ref damageInfo.Damage, ref damageInfo.Absorb);
		target.DealSpellDamage(damageInfo, true);
		target.SendSpellNonMeleeDamageLog(damageInfo);

		return true;
	}

	[CommandNonGroup("dev", RBACPermissions.CommandDev)]
	static bool HandleDevCommand(CommandHandler handler, bool? enableArg)
	{
		var player = handler.Session.Player;

		if (!enableArg.HasValue)
		{
			handler.Session.SendNotification(player.IsDeveloper ? CypherStrings.DevOn : CypherStrings.DevOff);

			return true;
		}

		if (enableArg.Value)
		{
			player.SetDeveloper(true);
			handler.Session.SendNotification(CypherStrings.DevOn);
		}
		else
		{
			player.SetDeveloper(false);
			handler.Session.SendNotification(CypherStrings.DevOff);
		}

		return true;
	}

	[CommandNonGroup("die", RBACPermissions.CommandDie)]
	static bool HandleDieCommand(CommandHandler handler)
	{
		var target = handler.SelectedUnit;

		if (!target && handler.Player.Target.IsEmpty)
		{
			handler.SendSysMessage(CypherStrings.SelectCharOrCreature);

			return false;
		}

		var player = target.AsPlayer;

		if (player)
			if (handler.HasLowerSecurity(player, ObjectGuid.Empty, false))
				return false;

		if (target.IsAlive)
			Unit.Kill(handler.Session.Player, target);

		return true;
	}

	[CommandNonGroup("dismount", RBACPermissions.CommandDismount)]
	static bool HandleDismountCommand(CommandHandler handler)
	{
		var player = handler.SelectedPlayerOrSelf;

		// If player is not mounted, so go out :)
		if (!player.IsMounted)
		{
			handler.SendSysMessage(CypherStrings.CharNonMounted);

			return false;
		}

		if (player.IsInFlight)
		{
			handler.SendSysMessage(CypherStrings.CharInFlight);

			return false;
		}

		player.Dismount();
		player.RemoveAurasByType(AuraType.Mounted);

		return true;
	}

	[CommandNonGroup("distance", RBACPermissions.CommandDistance)]
	static bool HandleGetDistanceCommand(CommandHandler handler, StringArguments args)
	{
		WorldObject obj;

		if (!args.Empty())
		{
			HighGuid guidHigh = 0;
			var guidLow = handler.ExtractLowGuidFromLink(args, ref guidHigh);

			if (guidLow == 0)
				return false;

			switch (guidHigh)
			{
				case HighGuid.Player:
				{
					obj = Global.ObjAccessor.FindPlayer(ObjectGuid.Create(HighGuid.Player, guidLow));

					if (!obj)
						handler.SendSysMessage(CypherStrings.PlayerNotFound);

					break;
				}
				case HighGuid.Creature:
				{
					obj = handler.GetCreatureFromPlayerMapByDbGuid(guidLow);

					if (!obj)
						handler.SendSysMessage(CypherStrings.CommandNocreaturefound);

					break;
				}
				case HighGuid.GameObject:
				{
					obj = handler.GetObjectFromPlayerMapByDbGuid(guidLow);

					if (!obj)
						handler.SendSysMessage(CypherStrings.CommandNogameobjectfound);

					break;
				}
				default:
					return false;
			}

			if (!obj)
				return false;
		}
		else
		{
			obj = handler.SelectedUnit;

			if (!obj)
			{
				handler.SendSysMessage(CypherStrings.SelectCharOrCreature);

				return false;
			}
		}

		handler.SendSysMessage(CypherStrings.Distance, handler.Session.Player.GetDistance(obj), handler.Session.Player.GetDistance2d(obj), handler.Session.Player.Location.GetExactDist(obj.Location), handler.Session.Player.Location.GetExactDist2d(obj.Location));

		return true;
	}

	[CommandNonGroup("freeze", RBACPermissions.CommandFreeze)]
	static bool HandleFreezeCommand(CommandHandler handler, StringArguments args)
	{
		var player = handler.SelectedPlayer; // Selected player, if any. Might be null.
		var freezeDuration = 0;              // Freeze Duration (in seconds)
		var canApplyFreeze = false;          // Determines if every possible argument is set so Freeze can be applied
		var getDurationFromConfig = false;   // If there's no given duration, we'll retrieve the world cfg value later

		if (args.Empty())
		{
			// Might have a selected player. We'll check it later
			// Get the duration from world cfg
			getDurationFromConfig = true;
		}
		else
		{
			// Get the args that we might have (up to 2)
			var arg1 = args.NextString();
			var arg2 = args.NextString();

			// Analyze them to see if we got either a playerName or duration or both
			if (!arg1.IsEmpty())
			{
				if (arg1.IsNumber())
				{
					// case 2: .freeze duration
					// We have a selected player. We'll check him later
					if (!int.TryParse(arg1, out freezeDuration))
						return false;

					canApplyFreeze = true;
				}
				else
				{
					// case 3 or 4: .freeze player duration | .freeze player
					// find the player
					var name = arg1;
					ObjectManager.NormalizePlayerName(ref name);
					player = Global.ObjAccessor.FindPlayerByName(name);

					// Check if we have duration set
					if (!arg2.IsEmpty() && arg2.IsNumber())
					{
						if (!int.TryParse(arg2, out freezeDuration))
							return false;

						canApplyFreeze = true;
					}
					else
					{
						getDurationFromConfig = true;
					}
				}
			}
		}

		// Check if duration needs to be retrieved from config
		if (getDurationFromConfig)
		{
			freezeDuration = WorldConfig.GetIntValue(WorldCfg.GmFreezeDuration);
			canApplyFreeze = true;
		}

		// Player and duration retrieval is over
		if (canApplyFreeze)
		{
			if (!player) // can be null if some previous selection failed
			{
				handler.SendSysMessage(CypherStrings.CommandFreezeWrong);

				return true;
			}
			else if (player == handler.Session.Player)
			{
				// Can't freeze himself
				handler.SendSysMessage(CypherStrings.CommandFreezeError);

				return true;
			}
			else // Apply the effect
			{
				// Add the freeze aura and set the proper duration
				// Player combat status and flags are now handled
				// in Freeze Spell AuraScript (OnApply)
				var freeze = player.AddAura(9454, player);

				if (freeze != null)
				{
					if (freezeDuration != 0)
						freeze.SetDuration(freezeDuration * global::Time.InMilliseconds);

					handler.SendSysMessage(CypherStrings.CommandFreeze, player.GetName());
					// save player
					player.SaveToDB();

					return true;
				}
			}
		}

		return false;
	}

	[CommandNonGroup("gps", RBACPermissions.CommandGps)]
	static bool HandleGPSCommand(CommandHandler handler, StringArguments args)
	{
		WorldObject obj;

		if (!args.Empty())
		{
			HighGuid guidHigh = 0;
			var guidLow = handler.ExtractLowGuidFromLink(args, ref guidHigh);

			if (guidLow == 0)
				return false;

			switch (guidHigh)
			{
				case HighGuid.Player:
				{
					obj = Global.ObjAccessor.FindPlayer(ObjectGuid.Create(HighGuid.Player, guidLow));

					if (!obj)
						handler.SendSysMessage(CypherStrings.PlayerNotFound);

					break;
				}
				case HighGuid.Creature:
				{
					obj = handler.GetCreatureFromPlayerMapByDbGuid(guidLow);

					if (!obj)
						handler.SendSysMessage(CypherStrings.CommandNocreaturefound);

					break;
				}
				case HighGuid.GameObject:
				{
					obj = handler.GetObjectFromPlayerMapByDbGuid(guidLow);

					if (!obj)
						handler.SendSysMessage(CypherStrings.CommandNogameobjectfound);

					break;
				}
				default:
					return false;
			}

			if (!obj)
				return false;
		}
		else
		{
			obj = handler.SelectedUnit;

			if (!obj)
			{
				handler.SendSysMessage(CypherStrings.SelectCharOrCreature);

				return false;
			}
		}

		var cellCoord = GridDefines.ComputeCellCoord(obj.Location.X, obj.Location.Y);
		Cell cell = new(cellCoord);

		obj.GetZoneAndAreaId(out var zoneId, out var areaId);
		var mapId = obj.Location.MapId;

		var mapEntry = CliDB.MapStorage.LookupByKey(mapId);
		var zoneEntry = CliDB.AreaTableStorage.LookupByKey(zoneId);
		var areaEntry = CliDB.AreaTableStorage.LookupByKey(areaId);

		var zoneX = obj.Location.X;
		var zoneY = obj.Location.Y;

		Global.DB2Mgr.Map2ZoneCoordinates((int)zoneId, ref zoneX, ref zoneY);

		var map = obj.Map;
		var groundZ = obj.GetMapHeight(obj.Location.X, obj.Location.Y, MapConst.MaxHeight);
		var floorZ = obj.GetMapHeight(obj.Location.X, obj.Location.Y, obj.Location.Z);

		var gridCoord = GridDefines.ComputeGridCoord(obj.Location.X, obj.Location.Y);

		// 63? WHY?
		var gridX = (int)((MapConst.MaxGrids - 1) - gridCoord.X_Coord);
		var gridY = (int)((MapConst.MaxGrids - 1) - gridCoord.Y_Coord);

		var haveMap = TerrainInfo.ExistMap(mapId, gridX, gridY);
		var haveVMap = TerrainInfo.ExistVMap(mapId, gridX, gridY);
		var haveMMap = (Global.DisableMgr.IsPathfindingEnabled(mapId) && Global.MMapMgr.GetNavMesh(handler.Session.Player.Location.MapId) != null);

		if (haveVMap)
		{
			if (obj.IsOutdoors)
				handler.SendSysMessage(CypherStrings.GpsPositionOutdoors);
			else
				handler.SendSysMessage(CypherStrings.GpsPositionIndoors);
		}
		else
		{
			handler.SendSysMessage(CypherStrings.GpsNoVmap);
		}

		var unknown = handler.GetCypherString(CypherStrings.Unknown);

		handler.SendSysMessage(CypherStrings.MapPosition,
								mapId,
								(mapEntry != null ? mapEntry.MapName[handler.SessionDbcLocale] : unknown),
								zoneId,
								(zoneEntry != null ? zoneEntry.AreaName[handler.SessionDbcLocale] : unknown),
								areaId,
								(areaEntry != null ? areaEntry.AreaName[handler.SessionDbcLocale] : unknown),
								obj.Location.X,
								obj.Location.Y,
								obj.Location.Z,
								obj.Location.Orientation);

		var transport = obj.GetTransport<Transport>();

		if (transport)
			handler.SendSysMessage(CypherStrings.TransportPosition,
									transport.Template.MoTransport.SpawnMap,
									obj.TransOffsetX,
									obj.TransOffsetY,
									obj.TransOffsetZ,
									obj.TransOffsetO,
									transport.Entry,
									transport.GetName());

		handler.SendSysMessage(CypherStrings.GridPosition,
								cell.GetGridX(),
								cell.GetGridY(),
								cell.GetCellX(),
								cell.GetCellY(),
								obj.InstanceId,
								zoneX,
								zoneY,
								groundZ,
								floorZ,
								map.GetMinHeight(obj.PhaseShift, obj.Location.X, obj.Location.Y),
								haveMap,
								haveVMap,
								haveMMap);

		var status = map.GetLiquidStatus(obj.PhaseShift, obj.Location.X, obj.Location.Y, obj.Location.Z, LiquidHeaderTypeFlags.AllLiquids, out var liquidStatus);

		if (liquidStatus != null)
			handler.SendSysMessage(CypherStrings.LiquidStatus, liquidStatus.level, liquidStatus.depth_level, liquidStatus.entry, liquidStatus.type_flags, status);

		PhasingHandler.PrintToChat(handler, obj);

		return true;
	}

	[CommandNonGroup("guid", RBACPermissions.CommandGuid)]
	static bool HandleGUIDCommand(CommandHandler handler)
	{
		var guid = handler.Session.Player.Target;

		if (guid.IsEmpty)
		{
			handler.SendSysMessage(CypherStrings.NoSelection);

			return false;
		}

		handler.SendSysMessage(CypherStrings.ObjectGuid, guid.ToString(), guid.High);

		return true;
	}

	[CommandNonGroup("help", RBACPermissions.CommandHelp, true)]
	static bool HandleHelpCommand(CommandHandler handler, Tail cmd)
	{
		ChatCommandNode.SendCommandHelpFor(handler, cmd);

		if (cmd.IsEmpty())
			ChatCommandNode.SendCommandHelpFor(handler, "help");

		return true;
	}

	[CommandNonGroup("hidearea", RBACPermissions.CommandHidearea)]
	static bool HandleHideAreaCommand(CommandHandler handler, uint areaId)
	{
		var playerTarget = handler.SelectedPlayer;

		if (!playerTarget)
		{
			handler.SendSysMessage(CypherStrings.NoCharSelected);

			return false;
		}

		var area = CliDB.AreaTableStorage.LookupByKey(areaId);

		if (area == null)
		{
			handler.SendSysMessage(CypherStrings.BadValue);

			return false;
		}

		if (area.AreaBit < 0)
		{
			handler.SendSysMessage(CypherStrings.BadValue);

			return false;
		}

		var offset = (uint)(area.AreaBit / ActivePlayerData.ExploredZonesBits);

		if (offset >= PlayerConst.ExploredZonesSize)
		{
			handler.SendSysMessage(CypherStrings.BadValue);

			return false;
		}

		var val = 1u << (area.AreaBit % ActivePlayerData.ExploredZonesBits);
		playerTarget.RemoveExploredZones(offset, val);

		handler.SendSysMessage(CypherStrings.UnexploreArea);

		return true;
	}

	// move item to other slot
	[CommandNonGroup("itemmove", RBACPermissions.CommandItemmove)]
	static bool HandleItemMoveCommand(CommandHandler handler, byte srcSlot, byte dstSlot)
	{
		if (srcSlot == dstSlot)
			return true;

		if (handler.Session.Player.IsValidPos(InventorySlots.Bag0, srcSlot, true))
			return false;

		if (handler.Session.Player.IsValidPos(InventorySlots.Bag0, dstSlot, false))
			return false;

		var src = (ushort)((InventorySlots.Bag0 << 8) | srcSlot);
		var dst = (ushort)((InventorySlots.Bag0 << 8) | dstSlot);

		handler.Session.Player.SwapItem(src, dst);

		return true;
	}

	// kick player
	[CommandNonGroup("kick", RBACPermissions.CommandKick, true)]
	static bool HandleKickPlayerCommand(CommandHandler handler, StringArguments args)
	{
		if (!handler.ExtractPlayerTarget(args, out var target, out _, out var playerName))
			return false;

		if (handler.Session != null && target == handler.Session.Player)
		{
			handler.SendSysMessage(CypherStrings.CommandKickself);

			return false;
		}

		// check online security
		if (handler.HasLowerSecurity(target, ObjectGuid.Empty))
			return false;

		var kickReason = args.NextString("");
		var kickReasonStr = "No reason";

		if (kickReason != null)
			kickReasonStr = kickReason;

		if (WorldConfig.GetBoolValue(WorldCfg.ShowKickInWorld))
			Global.WorldMgr.SendWorldText(CypherStrings.CommandKickmessageWorld, (handler.Session != null ? handler.Session.PlayerName : "Server"), playerName, kickReasonStr);
		else
			handler.SendSysMessage(CypherStrings.CommandKickmessage, playerName);

		target.Session.KickPlayer("HandleKickPlayerCommand GM Command");

		return true;
	}

	[CommandNonGroup("linkgrave", RBACPermissions.CommandLinkgrave)]
	static bool HandleLinkGraveCommand(CommandHandler handler, uint graveyardId, [OptionalArg] string teamArg)
	{
		TeamFaction team;

		if (teamArg.IsEmpty())
			team = 0;
		else if (teamArg.Equals("horde", StringComparison.OrdinalIgnoreCase))
			team = TeamFaction.Horde;
		else if (teamArg.Equals("alliance", StringComparison.OrdinalIgnoreCase))
			team = TeamFaction.Alliance;
		else
			return false;

		var graveyard = Global.ObjectMgr.GetWorldSafeLoc(graveyardId);

		if (graveyard == null)
		{
			handler.SendSysMessage(CypherStrings.CommandGraveyardnoexist, graveyardId);

			return false;
		}

		var player = handler.Session.Player;

		var zoneId = player.Zone;

		var areaEntry = CliDB.AreaTableStorage.LookupByKey(zoneId);

		if (areaEntry == null || areaEntry.ParentAreaID != 0)
		{
			handler.SendSysMessage(CypherStrings.CommandGraveyardwrongzone, graveyardId, zoneId);

			return false;
		}

		if (Global.ObjectMgr.AddGraveYardLink(graveyardId, zoneId, team))
			handler.SendSysMessage(CypherStrings.CommandGraveyardlinked, graveyardId, zoneId);
		else
			handler.SendSysMessage(CypherStrings.CommandGraveyardalrlinked, graveyardId, zoneId);

		return true;
	}

	[CommandNonGroup("listfreeze", RBACPermissions.CommandListfreeze)]
	static bool HandleListFreezeCommand(CommandHandler handler)
	{
		// Get names from DB
		var stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_AURA_FROZEN);
		var result = DB.Characters.Query(stmt);

		if (result.IsEmpty())
		{
			handler.SendSysMessage(CypherStrings.CommandNoFrozenPlayers);

			return true;
		}

		// Header of the names
		handler.SendSysMessage(CypherStrings.CommandListFreeze);

		// Output of the results
		do
		{
			var player = result.Read<string>(0);
			var remaintime = result.Read<int>(1);
			// Save the frozen player to update remaining time in case of future .listfreeze uses
			// before the frozen state expires
			var frozen = Global.ObjAccessor.FindPlayerByName(player);

			if (frozen)
				frozen.SaveToDB();

			// Notify the freeze duration
			if (remaintime == -1) // Permanent duration
				handler.SendSysMessage(CypherStrings.CommandPermaFrozenPlayer, player);
			else
				// show time left (seconds)
				handler.SendSysMessage(CypherStrings.CommandTempFrozenPlayer, player, remaintime / global::Time.InMilliseconds);
		} while (result.NextRow());

		return true;
	}

	[CommandNonGroup("mailbox", RBACPermissions.CommandMailbox)]
	static bool HandleMailBoxCommand(CommandHandler handler)
	{
		var player = handler.Session.Player;

		handler.Session.SendShowMailBox(player.GUID);

		return true;
	}

	[CommandNonGroup("movegens", RBACPermissions.CommandMovegens)]
	static bool HandleMovegensCommand(CommandHandler handler)
	{
		var unit = handler.SelectedUnit;

		if (!unit)
		{
			handler.SendSysMessage(CypherStrings.SelectCharOrCreature);

			return false;
		}

		handler.SendSysMessage(CypherStrings.MovegensList, (unit.IsTypeId(TypeId.Player) ? "Player" : "Creature"), unit.GUID.ToString());

		if (unit.MotionMaster.Empty())
		{
			handler.SendSysMessage("Empty");

			return true;
		}

		unit.MotionMaster.GetDestination(out var x, out var y, out var z);

		var list = unit.MotionMaster.GetMovementGeneratorsInformation();

		foreach (var info in list)
			switch (info.Type)
			{
				case MovementGeneratorType.Idle:
					handler.SendSysMessage(CypherStrings.MovegensIdle);

					break;
				case MovementGeneratorType.Random:
					handler.SendSysMessage(CypherStrings.MovegensRandom);

					break;
				case MovementGeneratorType.Waypoint:
					handler.SendSysMessage(CypherStrings.MovegensWaypoint);

					break;
				case MovementGeneratorType.Confused:
					handler.SendSysMessage(CypherStrings.MovegensConfused);

					break;
				case MovementGeneratorType.Chase:
					if (info.TargetGUID.IsEmpty)
						handler.SendSysMessage(CypherStrings.MovegensChaseNull);
					else if (info.TargetGUID.IsPlayer)
						handler.SendSysMessage(CypherStrings.MovegensChasePlayer, info.TargetName, info.TargetGUID.ToString());
					else
						handler.SendSysMessage(CypherStrings.MovegensChaseCreature, info.TargetName, info.TargetGUID.ToString());

					break;
				case MovementGeneratorType.Follow:
					if (info.TargetGUID.IsEmpty)
						handler.SendSysMessage(CypherStrings.MovegensFollowNull);
					else if (info.TargetGUID.IsPlayer)
						handler.SendSysMessage(CypherStrings.MovegensFollowPlayer, info.TargetName, info.TargetGUID.ToString());
					else
						handler.SendSysMessage(CypherStrings.MovegensFollowCreature, info.TargetName, info.TargetGUID.ToString());

					break;
				case MovementGeneratorType.Home:
					if (unit.IsTypeId(TypeId.Unit))
						handler.SendSysMessage(CypherStrings.MovegensHomeCreature, x, y, z);
					else
						handler.SendSysMessage(CypherStrings.MovegensHomePlayer);

					break;
				case MovementGeneratorType.Flight:
					handler.SendSysMessage(CypherStrings.MovegensFlight);

					break;
				case MovementGeneratorType.Point:
					handler.SendSysMessage(CypherStrings.MovegensPoint, x, y, z);

					break;
				case MovementGeneratorType.Fleeing:
					handler.SendSysMessage(CypherStrings.MovegensFear);

					break;
				case MovementGeneratorType.Distract:
					handler.SendSysMessage(CypherStrings.MovegensDistract);

					break;
				case MovementGeneratorType.Effect:
					handler.SendSysMessage(CypherStrings.MovegensEffect);

					break;
				default:
					handler.SendSysMessage(CypherStrings.MovegensUnknown, info.Type);

					break;
			}

		return true;
	}

	// mute player for the specified duration
	[CommandNonGroup("mute", RBACPermissions.CommandMute, true)]
	static bool HandleMuteCommand(CommandHandler handler, PlayerIdentifier player, uint muteTime, Tail muteReason)
	{
		string muteReasonStr = muteReason;

		if (muteReason.IsEmpty())
			muteReasonStr = handler.GetCypherString(CypherStrings.NoReason);

		if (player == null)
			player = PlayerIdentifier.FromTarget(handler);

		if (player == null)
		{
			handler.SendSysMessage(CypherStrings.PlayerNotFound);

			return false;
		}

		var target = player.GetConnectedPlayer();
		var accountId = target != null ? target.Session.AccountId : Global.CharacterCacheStorage.GetCharacterAccountIdByGuid(player.GetGUID());

		// find only player from same account if any
		if (!target)
		{
			var session = Global.WorldMgr.FindSession(accountId);

			if (session != null)
				target = session.Player;
		}

		// must have strong lesser security level
		if (handler.HasLowerSecurity(target, player.GetGUID(), true))
			return false;

		var stmt = DB.Login.GetPreparedStatement(LoginStatements.UPD_MUTE_TIME);
		string muteBy;
		var gmPlayer = handler.Player;

		if (gmPlayer != null)
			muteBy = gmPlayer.GetName();
		else
			muteBy = handler.GetCypherString(CypherStrings.Console);

		if (target)
		{
			// Target is online, mute will be in effect right away.
			var mutedUntil = GameTime.GetGameTime() + muteTime * global::Time.Minute;
			target.Session.MuteTime = mutedUntil;
			stmt.AddValue(0, mutedUntil);
		}
		else
		{
			stmt.AddValue(0, -(muteTime * global::Time.Minute));
		}

		stmt.AddValue(1, muteReasonStr);
		stmt.AddValue(2, muteBy);
		stmt.AddValue(3, accountId);
		DB.Login.Execute(stmt);

		stmt = DB.Login.GetPreparedStatement(LoginStatements.INS_ACCOUNT_MUTE);
		stmt.AddValue(0, accountId);
		stmt.AddValue(1, muteTime);
		stmt.AddValue(2, muteBy);
		stmt.AddValue(3, muteReasonStr);
		DB.Login.Execute(stmt);

		var nameLink = handler.PlayerLink(player.GetName());

		if (WorldConfig.GetBoolValue(WorldCfg.ShowMuteInWorld))
			Global.WorldMgr.SendWorldText(CypherStrings.CommandMutemessageWorld, muteBy, nameLink, muteTime, muteReasonStr);

		if (target)
		{
			target.SendSysMessage(CypherStrings.YourChatDisabled, muteTime, muteBy, muteReasonStr);
			handler.SendSysMessage(CypherStrings.YouDisableChat, nameLink, muteTime, muteReasonStr);
		}
		else
		{
			handler.SendSysMessage(CypherStrings.CommandDisableChatDelayed, nameLink, muteTime, muteReasonStr);
		}

		return true;
	}

	// mutehistory command
	[CommandNonGroup("mutehistory", RBACPermissions.CommandMutehistory, true)]
	static bool HandleMuteHistoryCommand(CommandHandler handler, string accountName)
	{
		var accountId = Global.AccountMgr.GetId(accountName);

		if (accountId == 0)
		{
			handler.SendSysMessage(CypherStrings.AccountNotExist, accountName);

			return false;
		}

		var stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_ACCOUNT_MUTE_INFO);
		stmt.AddValue(0, accountId);

		var result = DB.Login.Query(stmt);

		if (result.IsEmpty())
		{
			handler.SendSysMessage(CypherStrings.CommandMutehistoryEmpty, accountName);

			return true;
		}

		handler.SendSysMessage(CypherStrings.CommandMutehistory, accountName);

		do
		{
			// we have to manually set the string for mutedate
			long sqlTime = result.Read<uint>(0);

			// set it to string
			var buffer = global::Time.UnixTimeToDateTime(sqlTime).ToShortTimeString();

			handler.SendSysMessage(CypherStrings.CommandMutehistoryOutput, buffer, result.Read<uint>(1), result.Read<string>(2), result.Read<string>(3));
		} while (result.NextRow());

		return true;
	}

	[CommandNonGroup("neargrave", RBACPermissions.CommandNeargrave)]
	static bool HandleNearGraveCommand(CommandHandler handler, [OptionalArg] string teamArg)
	{
		TeamFaction team;

		if (teamArg.IsEmpty())
			team = 0;
		else if (teamArg.Equals("horde", StringComparison.OrdinalIgnoreCase))
			team = TeamFaction.Horde;
		else if (teamArg.Equals("alliance", StringComparison.OrdinalIgnoreCase))
			team = TeamFaction.Alliance;
		else
			return false;

		var player = handler.Session.Player;
		var zoneId = player.Zone;

		var graveyard = Global.ObjectMgr.GetClosestGraveYard(player.Location, team, null);

		if (graveyard != null)
		{
			var graveyardId = graveyard.Id;

			var data = Global.ObjectMgr.FindGraveYardData(graveyardId, zoneId);

			if (data == null)
			{
				handler.SendSysMessage(CypherStrings.CommandGraveyarderror, graveyardId);

				return false;
			}

			team = (TeamFaction)data.team;

			var team_name = handler.GetCypherString(CypherStrings.CommandGraveyardNoteam);

			if (team == 0)
				team_name = handler.GetCypherString(CypherStrings.CommandGraveyardAny);
			else if (team == TeamFaction.Horde)
				team_name = handler.GetCypherString(CypherStrings.CommandGraveyardHorde);
			else if (team == TeamFaction.Alliance)
				team_name = handler.GetCypherString(CypherStrings.CommandGraveyardAlliance);

			handler.SendSysMessage(CypherStrings.CommandGraveyardnearest, graveyardId, team_name, zoneId);
		}
		else
		{
			var team_name = "";

			if (team == TeamFaction.Horde)
				team_name = handler.GetCypherString(CypherStrings.CommandGraveyardHorde);
			else if (team == TeamFaction.Alliance)
				team_name = handler.GetCypherString(CypherStrings.CommandGraveyardAlliance);

			if (team == 0)
				handler.SendSysMessage(CypherStrings.CommandZonenograveyards, zoneId);
			else
				handler.SendSysMessage(CypherStrings.CommandZonenografaction, zoneId, team_name);
		}

		return true;
	}

	[CommandNonGroup("pinfo", RBACPermissions.CommandPinfo, true)]
	static bool HandlePInfoCommand(CommandHandler handler, StringArguments args)
	{
		// Define ALL the player variables!
		Player target;
		ObjectGuid targetGuid;
		PreparedStatement stmt;

		// To make sure we get a target, we convert our guid to an omniversal...
		var parseGUID = ObjectGuid.Create(HighGuid.Player, args.NextUInt64());

		// ... and make sure we get a target, somehow.
		if (Global.CharacterCacheStorage.GetCharacterNameByGuid(parseGUID, out var targetName))
		{
			target = Global.ObjAccessor.FindPlayer(parseGUID);
			targetGuid = parseGUID;
		}
		// if not, then return false. Which shouldn't happen, now should it ?
		else if (!handler.ExtractPlayerTarget(args, out target, out targetGuid, out targetName))
		{
			return false;
		}

		/* The variables we extract for the command. They are
		* default as "does not exist" to prevent problems
		* The output is printed in the follow manner:
		*
		* Player %s %s (guid: %u)                   - I.    LANG_PINFO_PLAYER
		* ** GM Mode active, Phase: -1              - II.   LANG_PINFO_GM_ACTIVE (if GM)
		* ** Banned: (Type, Reason, Time, By)       - III.  LANG_PINFO_BANNED (if banned)
		* ** Muted: (Reason, Time, By)              - IV.   LANG_PINFO_MUTED (if muted)
		* * Account: %s (id: %u), GM Level: %u      - V.    LANG_PINFO_ACC_ACCOUNT
		* * Last Login: %u (Failed Logins: %u)      - VI.   LANG_PINFO_ACC_LASTLOGIN
		* * Uses OS: %s - Latency: %u ms            - VII.  LANG_PINFO_ACC_OS
		* * Registration Email: %s - Email: %s      - VIII. LANG_PINFO_ACC_REGMAILS
		* * Last IP: %u (Locked: %s)                - IX.   LANG_PINFO_ACC_IP
		* * Level: %u (%u/%u XP (%u XP left)        - X.    LANG_PINFO_CHR_LEVEL
		* * Race: %s %s, Class %s                   - XI.   LANG_PINFO_CHR_RACE
		* * Alive ?: %s                             - XII.  LANG_PINFO_CHR_ALIVE
		* * Phase: %s                               - XIII. LANG_PINFO_CHR_PHASE (if not GM)
		* * Money: %ug%us%uc                        - XIV.  LANG_PINFO_CHR_MONEY
		* * Map: %s, Area: %s                       - XV.   LANG_PINFO_CHR_MAP
		* * Guild: %s (Id: %u)                      - XVI.  LANG_PINFO_CHR_GUILD (if in guild)
		* ** Rank: %s                               - XVII. LANG_PINFO_CHR_GUILD_RANK (if in guild)
		* ** Note: %s                               - XVIII.LANG_PINFO_CHR_GUILD_NOTE (if in guild and has note)
		* ** O. Note: %s                            - XVIX. LANG_PINFO_CHR_GUILD_ONOTE (if in guild and has officer note)
		* * Played time: %s                         - XX.   LANG_PINFO_CHR_PLAYEDTIME
		* * Mails: %u Read/%u Total                 - XXI.  LANG_PINFO_CHR_MAILS (if has mails)
		*
		* Not all of them can be moved to the top. These should
		* place the most important ones to the head, though.
		*
		* For a cleaner overview, I segment each output in Roman numerals
		*/

		// Account data print variables
		var userName = handler.GetCypherString(CypherStrings.Error);
		uint accId;
		var lowguid = targetGuid.Counter;
		var eMail = handler.GetCypherString(CypherStrings.Error);
		var regMail = handler.GetCypherString(CypherStrings.Error);
		uint security = 0;
		var lastIp = handler.GetCypherString(CypherStrings.Error);
		byte locked = 0;
		var lastLogin = handler.GetCypherString(CypherStrings.Error);
		uint failedLogins = 0;
		uint latency = 0;
		var OS = handler.GetCypherString(CypherStrings.Unknown);

		// Mute data print variables
		long muteTime = -1;
		var muteReason = handler.GetCypherString(CypherStrings.NoReason);
		var muteBy = handler.GetCypherString(CypherStrings.Unknown);

		// Ban data print variables
		long banTime = -1;
		var banType = handler.GetCypherString(CypherStrings.Unknown);
		var banReason = handler.GetCypherString(CypherStrings.NoReason);
		var bannedBy = handler.GetCypherString(CypherStrings.Unknown);

		// Character data print variables
		Race raceid;
		PlayerClass classid;
		Gender gender;
		var locale = handler.SessionDbcLocale;
		uint totalPlayerTime;
		uint level;
		string alive;
		ulong money;
		uint xp = 0;
		uint xptotal = 0;

		// Position data print
		uint mapId;
		uint areaId;
		string areaName = null;
		string zoneName = null;

		// Guild data print variables defined so that they exist, but are not necessarily used
		ulong guildId = 0;
		byte guildRankId = 0;
		var guildName = "";
		var guildRank = "";
		var note = "";
		var officeNote = "";

		// Mail data print is only defined if you have a mail

		if (target)
		{
			// check online security
			if (handler.HasLowerSecurity(target, ObjectGuid.Empty))
				return false;

			accId = target.Session.AccountId;
			money = target.Money;
			totalPlayerTime = target.TotalPlayedTime;
			level = target.Level;
			latency = target.Session.Latency;
			raceid = target.Race;
			classid = target.Class;
			muteTime = target.Session.MuteTime;
			mapId = target.Location.MapId;
			areaId = target.Area;
			alive = target.IsAlive ? handler.GetCypherString(CypherStrings.Yes) : handler.GetCypherString(CypherStrings.No);
			gender = target.NativeGender;
		}
		// get additional information from DB
		else
		{
			// check offline security
			if (handler.HasLowerSecurity(null, targetGuid))
				return false;

			// Query informations from the DB
			stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_PINFO);
			stmt.AddValue(0, lowguid);
			var result = DB.Characters.Query(stmt);

			if (result.IsEmpty())
				return false;

			totalPlayerTime = result.Read<uint>(0);
			level = result.Read<byte>(1);
			money = result.Read<ulong>(2);
			accId = result.Read<uint>(3);
			raceid = (Race)result.Read<byte>(4);
			classid = (PlayerClass)result.Read<byte>(5);
			mapId = result.Read<ushort>(6);
			areaId = result.Read<ushort>(7);
			gender = (Gender)result.Read<byte>(8);
			var health = result.Read<uint>(9);
			var playerFlags = (PlayerFlags)result.Read<uint>(10);

			if (health == 0 || playerFlags.HasAnyFlag(PlayerFlags.Ghost))
				alive = handler.GetCypherString(CypherStrings.No);
			else
				alive = handler.GetCypherString(CypherStrings.Yes);
		}

		// Query the prepared statement for login data
		stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_PINFO);
		stmt.AddValue(0, Global.WorldMgr.Realm.Id.Index);
		stmt.AddValue(1, accId);
		var result0 = DB.Login.Query(stmt);

		if (!result0.IsEmpty())
		{
			userName = result0.Read<string>(0);
			security = result0.Read<byte>(1);

			// Only fetch these fields if commander has sufficient rights)
			if (handler.HasPermission(RBACPermissions.CommandsPinfoCheckPersonalData) && // RBAC Perm. 48, Role 39
				(!handler.Session || handler.Session.Security >= (AccountTypes)security))
			{
				eMail = result0.Read<string>(2);
				regMail = result0.Read<string>(3);
				lastIp = result0.Read<string>(4);
				lastLogin = result0.Read<string>(5);
			}
			else
			{
				eMail = handler.GetCypherString(CypherStrings.Unauthorized);
				regMail = handler.GetCypherString(CypherStrings.Unauthorized);
				lastIp = handler.GetCypherString(CypherStrings.Unauthorized);
				lastLogin = handler.GetCypherString(CypherStrings.Unauthorized);
			}

			muteTime = (long)result0.Read<ulong>(6);
			muteReason = result0.Read<string>(7);
			muteBy = result0.Read<string>(8);
			failedLogins = result0.Read<uint>(9);
			locked = result0.Read<byte>(10);
			OS = result0.Read<string>(11);
		}

		// Creates a chat link to the character. Returns nameLink
		var nameLink = handler.PlayerLink(targetName);

		// Returns banType, banTime, bannedBy, banreason
		var stmt2 = DB.Login.GetPreparedStatement(LoginStatements.SEL_PINFO_BANS);
		stmt2.AddValue(0, accId);
		var result2 = DB.Login.Query(stmt2);

		if (result2.IsEmpty())
		{
			banType = handler.GetCypherString(CypherStrings.Character);
			stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_PINFO_BANS);
			stmt.AddValue(0, lowguid);
			result2 = DB.Characters.Query(stmt);
		}
		else
		{
			banType = handler.GetCypherString(CypherStrings.Account);
		}

		if (!result2.IsEmpty())
		{
			var permanent = result2.Read<ulong>(1) != 0;
			banTime = !permanent ? result2.Read<uint>(0) : 0;
			bannedBy = result2.Read<string>(2);
			banReason = result2.Read<string>(3);
		}

		// Can be used to query data from Characters database
		stmt2 = DB.Characters.GetPreparedStatement(CharStatements.SEL_PINFO_XP);
		stmt2.AddValue(0, lowguid);
		var result4 = DB.Characters.Query(stmt2);

		if (!result4.IsEmpty())
		{
			xp = result4.Read<uint>(0);         // Used for "current xp" output and "%u XP Left" calculation
			var gguid = result4.Read<ulong>(1); // We check if have a guild for the person, so we might not require to query it at all
			xptotal = Global.ObjectMgr.GetXPForLevel(level);

			if (gguid != 0)
			{
				// Guild Data - an own query, because it may not happen.
				var stmt3 = DB.Characters.GetPreparedStatement(CharStatements.SEL_GUILD_MEMBER_EXTENDED);
				stmt3.AddValue(0, lowguid);
				var result5 = DB.Characters.Query(stmt3);

				if (!result5.IsEmpty())
				{
					guildId = result5.Read<ulong>(0);
					guildName = result5.Read<string>(1);
					guildRank = result5.Read<string>(2);
					guildRankId = result5.Read<byte>(3);
					note = result5.Read<string>(4);
					officeNote = result5.Read<string>(5);
				}
			}
		}

		// Initiate output
		// Output I. LANG_PINFO_PLAYER
		handler.SendSysMessage(CypherStrings.PinfoPlayer, target ? "" : handler.GetCypherString(CypherStrings.Offline), nameLink, targetGuid.ToString());

		// Output II. LANG_PINFO_GM_ACTIVE if character is gamemaster
		if (target && target.IsGameMaster)
			handler.SendSysMessage(CypherStrings.PinfoGmActive);

		// Output III. LANG_PINFO_BANNED if ban exists and is applied
		if (banTime >= 0)
			handler.SendSysMessage(CypherStrings.PinfoBanned, banType, banReason, banTime > 0 ? global::Time.secsToTimeString((ulong)(banTime - GameTime.GetGameTime()), TimeFormat.ShortText) : handler.GetCypherString(CypherStrings.Permanently), bannedBy);

		// Output IV. LANG_PINFO_MUTED if mute is applied
		if (muteTime > 0)
			handler.SendSysMessage(CypherStrings.PinfoMuted, muteReason, global::Time.secsToTimeString((ulong)(muteTime - GameTime.GetGameTime()), TimeFormat.ShortText), muteBy);

		// Output V. LANG_PINFO_ACC_ACCOUNT
		handler.SendSysMessage(CypherStrings.PinfoAccAccount, userName, accId, security);

		// Output VI. LANG_PINFO_ACC_LASTLOGIN
		handler.SendSysMessage(CypherStrings.PinfoAccLastlogin, lastLogin, failedLogins);

		// Output VII. LANG_PINFO_ACC_OS
		handler.SendSysMessage(CypherStrings.PinfoAccOs, OS, latency);

		// Output VIII. LANG_PINFO_ACC_REGMAILS
		handler.SendSysMessage(CypherStrings.PinfoAccRegmails, regMail, eMail);

		// Output IX. LANG_PINFO_ACC_IP
		handler.SendSysMessage(CypherStrings.PinfoAccIp, lastIp, locked != 0 ? handler.GetCypherString(CypherStrings.Yes) : handler.GetCypherString(CypherStrings.No));

		// Output X. LANG_PINFO_CHR_LEVEL
		if (level != WorldConfig.GetIntValue(WorldCfg.MaxPlayerLevel))
			handler.SendSysMessage(CypherStrings.PinfoChrLevelLow, level, xp, xptotal, (xptotal - xp));
		else
			handler.SendSysMessage(CypherStrings.PinfoChrLevelHigh, level);

		// Output XI. LANG_PINFO_CHR_RACE
		handler.SendSysMessage(CypherStrings.PinfoChrRace,
								(gender == 0 ? handler.GetCypherString(CypherStrings.CharacterGenderMale) : handler.GetCypherString(CypherStrings.CharacterGenderFemale)),
								Global.DB2Mgr.GetChrRaceName(raceid, locale),
								Global.DB2Mgr.GetClassName(classid, locale));

		// Output XII. LANG_PINFO_CHR_ALIVE
		handler.SendSysMessage(CypherStrings.PinfoChrAlive, alive);

		// Output XIII. phases
		if (target)
			PhasingHandler.PrintToChat(handler, target);

		// Output XIV. LANG_PINFO_CHR_MONEY
		var gold = money / MoneyConstants.Gold;
		var silv = (money % MoneyConstants.Gold) / MoneyConstants.Silver;
		var copp = (money % MoneyConstants.Gold) % MoneyConstants.Silver;
		handler.SendSysMessage(CypherStrings.PinfoChrMoney, gold, silv, copp);

		// Position data
		var map = CliDB.MapStorage.LookupByKey(mapId);
		var area = CliDB.AreaTableStorage.LookupByKey(areaId);

		if (area != null)
		{
			zoneName = area.AreaName[locale];

			var zone = CliDB.AreaTableStorage.LookupByKey(area.ParentAreaID);

			if (zone != null)
			{
				areaName = zoneName;
				zoneName = zone.AreaName[locale];
			}
		}

		if (zoneName == null)
			zoneName = handler.GetCypherString(CypherStrings.Unknown);

		if (areaName != null)
			handler.SendSysMessage(CypherStrings.PinfoChrMapWithArea, map.MapName[locale], zoneName, areaName);
		else
			handler.SendSysMessage(CypherStrings.PinfoChrMap, map.MapName[locale], zoneName);

		// Output XVII. - XVIX. if they are not empty
		if (!guildName.IsEmpty())
		{
			handler.SendSysMessage(CypherStrings.PinfoChrGuild, guildName, guildId);
			handler.SendSysMessage(CypherStrings.PinfoChrGuildRank, guildRank, guildRankId);

			if (!note.IsEmpty())
				handler.SendSysMessage(CypherStrings.PinfoChrGuildNote, note);

			if (!officeNote.IsEmpty())
				handler.SendSysMessage(CypherStrings.PinfoChrGuildOnote, officeNote);
		}

		// Output XX. LANG_PINFO_CHR_PLAYEDTIME
		handler.SendSysMessage(CypherStrings.PinfoChrPlayedtime, (global::Time.secsToTimeString(totalPlayerTime, TimeFormat.ShortText, true)));

		// Mail Data - an own query, because it may or may not be useful.
		// SQL: "SELECT SUM(CASE WHEN (checked & 1) THEN 1 ELSE 0 END) AS 'readmail', COUNT(*) AS 'totalmail' FROM mail WHERE `receiver` = ?"
		var stmt4 = DB.Characters.GetPreparedStatement(CharStatements.SEL_PINFO_MAILS);
		stmt4.AddValue(0, lowguid);
		var result6 = DB.Characters.Query(stmt4);

		if (!result6.IsEmpty())
		{
			var readmail = (uint)result6.Read<double>(0);
			var totalmail = (uint)result6.Read<ulong>(1);

			// Output XXI. LANG_INFO_CHR_MAILS if at least one mail is given
			if (totalmail >= 1)
				handler.SendSysMessage(CypherStrings.PinfoChrMails, readmail, totalmail);
		}

		return true;
	}

	[CommandNonGroup("playall", RBACPermissions.CommandPlayall)]
	static bool HandlePlayAllCommand(CommandHandler handler, uint soundId, uint? broadcastTextId)
	{
		if (!CliDB.SoundKitStorage.ContainsKey(soundId))
		{
			handler.SendSysMessage(CypherStrings.SoundNotExist, soundId);

			return false;
		}

		Global.WorldMgr.SendGlobalMessage(new PlaySound(handler.Session.Player.GUID, soundId, broadcastTextId.GetValueOrDefault(0)));

		handler.SendSysMessage(CypherStrings.CommandPlayedToAll, soundId);

		return true;
	}

	[CommandNonGroup("possess", RBACPermissions.CommandPossess)]
	static bool HandlePossessCommand(CommandHandler handler)
	{
		var unit = handler.SelectedUnit;

		if (!unit)
			return false;

		handler.Session.Player.CastSpell(unit, 530, true);

		return true;
	}

	[CommandNonGroup("pvpstats", RBACPermissions.CommandPvpstats, true)]
	static bool HandlePvPstatsCommand(CommandHandler handler)
	{
		if (WorldConfig.GetBoolValue(WorldCfg.BattlegroundStoreStatisticsEnable))
		{
			var stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_PVPSTATS_FACTIONS_OVERALL);
			var result = DB.Characters.Query(stmt);

			if (!result.IsEmpty())
			{
				var horde_victories = result.Read<uint>(1);

				if (!(result.NextRow()))
					return false;

				var alliance_victories = result.Read<uint>(1);

				handler.SendSysMessage(CypherStrings.Pvpstats, alliance_victories, horde_victories);
			}
			else
			{
				return false;
			}
		}
		else
		{
			handler.SendSysMessage(CypherStrings.PvpstatsDisabled);
		}

		return true;
	}

	// Teleport player to last position
	[CommandNonGroup("recall", RBACPermissions.CommandRecall)]
	static bool HandleRecallCommand(CommandHandler handler, StringArguments args)
	{
		if (!handler.ExtractPlayerTarget(args, out var target))
			return false;

		// check online security
		if (handler.HasLowerSecurity(target, ObjectGuid.Empty))
			return false;

		if (target.IsBeingTeleported)
		{
			handler.SendSysMessage(CypherStrings.IsTeleported, handler.GetNameLink(target));

			return false;
		}

		// stop flight if need
		target.FinishTaxiFlight();

		target.Recall();

		return true;
	}

	[CommandNonGroup("repairitems", RBACPermissions.CommandRepairitems, true)]
	static bool HandleRepairitemsCommand(CommandHandler handler, StringArguments args)
	{
		if (!handler.ExtractPlayerTarget(args, out var target))
			return false;

		// check online security
		if (handler.HasLowerSecurity(target, ObjectGuid.Empty))
			return false;

		// Repair items
		target.DurabilityRepairAll(false, 0, false);

		handler.SendSysMessage(CypherStrings.YouRepairItems, handler.GetNameLink(target));

		if (handler.NeedReportToTarget(target))
			target.SendSysMessage(CypherStrings.YourItemsRepaired, handler.NameLink);

		return true;
	}

	[CommandNonGroup("respawn", RBACPermissions.CommandRespawn)]
	static bool HandleRespawnCommand(CommandHandler handler)
	{
		var player = handler.Session.Player;

		// accept only explicitly selected target (not implicitly self targeting case)
		var target = !player.Target.IsEmpty ? handler.SelectedCreature : null;

		if (target)
		{
			if (target.IsPet)
			{
				handler.SendSysMessage(CypherStrings.SelectCreature);

				return false;
			}

			if (target.IsDead)
				target.Respawn();

			return true;
		}

		// First handle any creatures that still have a corpse around
		var worker = new WorldObjectWorker(player, new RespawnDo());
		Cell.VisitGrid(player, worker, player.GridActivationRange);

		// Now handle any that had despawned, but had respawn time logged.
		List<RespawnInfo> data = new();
		player.Map.GetRespawnInfo(data, SpawnObjectTypeMask.All);

		if (!data.Empty())
		{
			var gridId = GridDefines.ComputeGridCoord(player.Location.X, player.Location.Y).GetId();

			foreach (var info in data)
				if (info.GridId == gridId)
					player.Map.RemoveRespawnTime(info.ObjectType, info.SpawnId);
		}

		return true;
	}

	[CommandNonGroup("revive", RBACPermissions.CommandRevive, true)]
	static bool HandleReviveCommand(CommandHandler handler, StringArguments args)
	{
		if (!handler.ExtractPlayerTarget(args, out var target, out var targetGuid))
			return false;

		if (target != null)
		{
			target.ResurrectPlayer(0.5f);
			target.SpawnCorpseBones();
			target.SaveToDB();
		}
		else
		{
			Player.OfflineResurrect(targetGuid, null);
		}

		return true;
	}

	// Save all players in the world
	[CommandNonGroup("saveall", RBACPermissions.CommandSaveall, true)]
	static bool HandleSaveAllCommand(CommandHandler handler)
	{
		Global.ObjAccessor.SaveAllPlayers();
		handler.SendSysMessage(CypherStrings.PlayersSaved);

		return true;
	}

	[CommandNonGroup("save", RBACPermissions.CommandSave)]
	static bool HandleSaveCommand(CommandHandler handler)
	{
		var player = handler.Session.Player;

		// save GM account without delay and output message
		if (handler.Session.HasPermission(RBACPermissions.CommandsSaveWithoutDelay))
		{
			var target = handler.SelectedPlayer;

			if (target)
				target.SaveToDB();
			else
				player.SaveToDB();

			handler.SendSysMessage(CypherStrings.PlayerSaved);

			return true;
		}

		// save if the player has last been saved over 20 seconds ago
		var saveInterval = WorldConfig.GetUIntValue(WorldCfg.IntervalSave);

		if (saveInterval == 0 || (saveInterval > 20 * global::Time.InMilliseconds && player.SaveTimer <= saveInterval - 20 * global::Time.InMilliseconds))
			player.SaveToDB();

		return true;
	}

	[CommandNonGroup("showarea", RBACPermissions.CommandShowarea)]
	static bool HandleShowAreaCommand(CommandHandler handler, uint areaId)
	{
		var playerTarget = handler.SelectedPlayer;

		if (!playerTarget)
		{
			handler.SendSysMessage(CypherStrings.NoCharSelected);

			return false;
		}

		var area = CliDB.AreaTableStorage.LookupByKey(areaId);

		if (area == null)
		{
			handler.SendSysMessage(CypherStrings.BadValue);

			return false;
		}

		if (area.AreaBit < 0)
		{
			handler.SendSysMessage(CypherStrings.BadValue);

			return false;
		}

		var offset = (uint)(area.AreaBit / ActivePlayerData.ExploredZonesBits);

		if (offset >= PlayerConst.ExploredZonesSize)
		{
			handler.SendSysMessage(CypherStrings.BadValue);

			return false;
		}

		var val = 1ul << (area.AreaBit % ActivePlayerData.ExploredZonesBits);
		playerTarget.AddExploredZones(offset, val);

		handler.SendSysMessage(CypherStrings.ExploreArea);

		return true;
	}

	// Summon Player
	[CommandNonGroup("summon", RBACPermissions.CommandSummon)]
	static bool HandleSummonCommand(CommandHandler handler, StringArguments args)
	{
		if (!handler.ExtractPlayerTarget(args, out var target, out var targetGuid, out var targetName))
			return false;

		var _player = handler.Session.Player;

		if (target == _player || targetGuid == _player.GUID)
		{
			handler.SendSysMessage(CypherStrings.CantTeleportSelf);

			return false;
		}

		if (target)
		{
			var nameLink = handler.PlayerLink(targetName);

			// check online security
			if (handler.HasLowerSecurity(target, ObjectGuid.Empty))
				return false;

			if (target.IsBeingTeleported)
			{
				handler.SendSysMessage(CypherStrings.IsTeleported, nameLink);

				return false;
			}

			var map = _player.Map;

			if (map.IsBattlegroundOrArena)
			{
				// only allow if gm mode is on
				if (!_player.IsGameMaster)
				{
					handler.SendSysMessage(CypherStrings.CannotGoToBgGm, nameLink);

					return false;
				}
				// if both players are in different bgs
				else if (target.BattlegroundId != 0 && _player.BattlegroundId != target.BattlegroundId)
				{
					target.LeaveBattleground(false); // Note: should be changed so target gets no Deserter debuff
				}

				// all's well, set bg id
				// when porting out from the bg, it will be reset to 0
				target.SetBattlegroundId(_player.BattlegroundId, _player.BattlegroundTypeId);

				// remember current position as entry point for return at bg end teleportation
				if (!target.Map.IsBattlegroundOrArena)
					target.SetBattlegroundEntryPoint();
			}
			else if (map.IsDungeon)
			{
				var targetMap = target.Map;

				Player targetGroupLeader = null;
				var targetGroup = target.Group;

				if (targetGroup != null)
					targetGroupLeader = Global.ObjAccessor.GetPlayer(map, targetGroup.LeaderGUID);

				// check if far teleport is allowed
				if (targetGroupLeader == null || (targetGroupLeader.Location.MapId != map.Id) || (targetGroupLeader.InstanceId != map.InstanceId))
					if ((targetMap.Id != map.Id) || (targetMap.InstanceId != map.InstanceId))
					{
						handler.SendSysMessage(CypherStrings.CannotSummonToInst);

						return false;
					}

				// check if we're already in a different instance of the same map
				if ((targetMap.Id == map.Id) && (targetMap.InstanceId != map.InstanceId))
				{
					handler.SendSysMessage(CypherStrings.CannotSummonInstInst, nameLink);

					return false;
				}
			}

			handler.SendSysMessage(CypherStrings.Summoning, nameLink, "");

			if (handler.NeedReportToTarget(target))
				target.SendSysMessage(CypherStrings.SummonedBy, handler.PlayerLink(_player.GetName()));

			// stop flight if need
			if (_player.IsInFlight)
				_player.FinishTaxiFlight();
			else
				_player.SaveRecallPosition(); // save only in non-flight case

			// before GM
			var pos = new Position();
			_player.GetClosePoint(pos, target.CombatReach);
			pos.Orientation = target.Location.Orientation;
			target.TeleportTo(_player.Location.MapId, pos, 0, map.InstanceId);
			PhasingHandler.InheritPhaseShift(target, _player);
			target.UpdateObjectVisibility();
		}
		else
		{
			// check offline security
			if (handler.HasLowerSecurity(null, targetGuid))
				return false;

			var nameLink = handler.PlayerLink(targetName);

			handler.SendSysMessage(CypherStrings.Summoning, nameLink, handler.GetCypherString(CypherStrings.Offline));

			// in point where GM stay
			Player.SavePositionInDB(new WorldLocation(_player.Location.MapId, _player.Location.X, _player.Location.Y, _player.Location.Z, _player.Location.Orientation), _player.Zone, targetGuid);
		}

		return true;
	}

	[CommandNonGroup("unbindsight", RBACPermissions.CommandUnbindsight)]
	static bool HandleUnbindSightCommand(CommandHandler handler)
	{
		var player = handler.Session.Player;

		if (player.IsPossessing)
			return false;

		player.StopCastingBindSight();

		return true;
	}

	[CommandNonGroup("unfreeze", RBACPermissions.CommandUnfreeze)]
	static bool HandleUnFreezeCommand(CommandHandler handler, [OptionalArg] string targetNameArg)
	{
		var name = "";
		Player player;

		if (!targetNameArg.IsEmpty())
		{
			name = targetNameArg;
			ObjectManager.NormalizePlayerName(ref name);
			player = Global.ObjAccessor.FindPlayerByName(name);
		}
		else // If no name was entered - use target
		{
			player = handler.SelectedPlayer;

			if (player)
				name = player.GetName();
		}

		if (player)
		{
			handler.SendSysMessage(CypherStrings.CommandUnfreeze, name);

			// Remove Freeze spell (allowing movement and spells)
			// Player Flags + Neutral faction removal is now
			// handled on the Freeze Spell AuraScript (OnRemove)
			player.RemoveAura(9454);
		}
		else
		{
			if (!targetNameArg.IsEmpty())
			{
				// Check for offline players
				var guid = Global.CharacterCacheStorage.GetCharacterGuidByName(name);

				if (guid.IsEmpty)
				{
					handler.SendSysMessage(CypherStrings.CommandFreezeWrong);

					return true;
				}

				// If player found: delete his freeze aura    
				var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_AURA_FROZEN);
				stmt.AddValue(0, guid.Counter);
				DB.Characters.Execute(stmt);

				handler.SendSysMessage(CypherStrings.CommandUnfreeze, name);

				return true;
			}
			else
			{
				handler.SendSysMessage(CypherStrings.CommandFreezeWrong);

				return true;
			}
		}

		return true;
	}

	// unmute player
	[CommandNonGroup("unmute", RBACPermissions.CommandUnmute, true)]
	static bool HandleUnmuteCommand(CommandHandler handler, StringArguments args)
	{
		if (!handler.ExtractPlayerTarget(args, out var target, out var targetGuid, out var targetName))
			return false;

		var accountId = target ? target.Session.AccountId : Global.CharacterCacheStorage.GetCharacterAccountIdByGuid(targetGuid);

		// find only player from same account if any
		if (!target)
		{
			var session = Global.WorldMgr.FindSession(accountId);

			if (session != null)
				target = session.Player;
		}

		// must have strong lesser security level
		if (handler.HasLowerSecurity(target, targetGuid, true))
			return false;

		if (target)
		{
			if (target.Session.CanSpeak)
			{
				handler.SendSysMessage(CypherStrings.ChatAlreadyEnabled);

				return false;
			}

			target.Session.MuteTime = 0;
		}

		var stmt = DB.Login.GetPreparedStatement(LoginStatements.UPD_MUTE_TIME);
		stmt.AddValue(0, 0);
		stmt.AddValue(1, "");
		stmt.AddValue(2, "");
		stmt.AddValue(3, accountId);
		DB.Login.Execute(stmt);

		if (target)
			target.SendSysMessage(CypherStrings.YourChatEnabled);

		var nameLink = handler.PlayerLink(targetName);

		handler.SendSysMessage(CypherStrings.YouEnableChat, nameLink);

		return true;
	}

	[CommandNonGroup("unpossess", RBACPermissions.CommandUnpossess)]
	static bool HandleUnPossessCommand(CommandHandler handler)
	{
		var unit = handler.SelectedUnit;

		if (!unit)
			unit = handler.Session.Player;

		unit.RemoveCharmAuras();

		return true;
	}

	[CommandNonGroup("unstuck", RBACPermissions.CommandUnstuck, true)]
	static bool HandleUnstuckCommand(CommandHandler handler, StringArguments args)
	{
		uint SPELL_UNSTUCK_ID = 7355;
		uint SPELL_UNSTUCK_VISUAL = 2683;

		// No args required for players
		if (handler.Session != null && handler.Session.HasPermission(RBACPermissions.CommandsUseUnstuckWithArgs))
		{
			// 7355: "Stuck"
			var player1 = handler.Session.Player;

			if (player1)
				player1.CastSpell(player1, SPELL_UNSTUCK_ID, false);

			return true;
		}

		if (args.Empty())
			return false;

		var location_str = "inn";
		var loc = args.NextString();

		if (string.IsNullOrEmpty(loc))
			location_str = loc;

		if (!handler.ExtractPlayerTarget(args, out var player, out var targetGUID))
			return false;

		if (!player)
		{
			var stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_HOMEBIND);
			stmt.AddValue(0, targetGUID.Counter);
			var result = DB.Characters.Query(stmt);

			if (!result.IsEmpty())
			{
				Player.SavePositionInDB(new WorldLocation(result.Read<ushort>(0), result.Read<float>(2), result.Read<float>(3), result.Read<float>(4), 0.0f), result.Read<ushort>(1), targetGUID);

				return true;
			}

			return false;
		}

		if (player.IsInFlight || player.IsInCombat)
		{
			var spellInfo = Global.SpellMgr.GetSpellInfo(SPELL_UNSTUCK_ID, Difficulty.None);

			if (spellInfo == null)
				return false;

			var caster = handler.Session.Player;

			if (caster)
			{
				var castId = ObjectGuid.Create(HighGuid.Cast, SpellCastSource.Normal, player.Location.MapId, SPELL_UNSTUCK_ID, player.Map.GenerateLowGuid(HighGuid.Cast));
				Spell.SendCastResult(caster, spellInfo, new SpellCastVisual(SPELL_UNSTUCK_VISUAL, 0), castId, SpellCastResult.CantDoThatRightNow);
			}

			return false;
		}

		if (location_str == "inn")
		{
			player.TeleportTo(player.Homebind);

			return true;
		}

		if (location_str == "graveyard")
		{
			player.RepopAtGraveyard();

			return true;
		}

		//Not a supported argument
		return false;
	}

	[CommandNonGroup("wchange", RBACPermissions.CommandWchange)]
	static bool HandleChangeWeather(CommandHandler handler, uint type, float intensity)
	{
		// Weather is OFF
		if (!WorldConfig.GetBoolValue(WorldCfg.Weather))
		{
			handler.SendSysMessage(CypherStrings.WeatherDisabled);

			return false;
		}

		var player = handler.Session.Player;
		var zoneid = player.Zone;

		var weather = player.Map.GetOrGenerateZoneDefaultWeather(zoneid);

		if (weather == null)
		{
			handler.SendSysMessage(CypherStrings.NoWeather);

			return false;
		}

		weather.SetWeather((WeatherType)type, intensity);

		return true;
	}
}