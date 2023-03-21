﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Framework.Database;
using Forged.RealmServer.AI;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Networking;
using Forged.RealmServer.Networking.Packets;
using Forged.RealmServer.Spells;

namespace Forged.RealmServer;

public partial class WorldSession
{
	[WorldPacketHandler(ClientOpcodes.DismissCritter)]
	void HandleDismissCritter(DismissCritter packet)
	{
		Unit pet = ObjectAccessor.GetCreatureOrPetOrVehicle(Player, packet.CritterGUID);

		if (!pet)
		{
			Log.outDebug(LogFilter.Network,
						"Critter {0} does not exist - player '{1}' ({2} / account: {3}) attempted to dismiss it (possibly lagged out)",
						packet.CritterGUID.ToString(),
						Player.GetName(),
						Player.GUID.ToString(),
						AccountId);

			return;
		}

		if (Player.CritterGUID == pet.GUID)
			if (pet.IsCreature && pet.IsSummon)
			{
				if (!_player.SummonedBattlePetGUID.IsEmpty && _player.SummonedBattlePetGUID == pet.BattlePetCompanionGUID)
					_player.SetBattlePetData(null);

				pet.ToTempSummon().UnSummon();
			}
	}

	void HandlePetActionHelper(Unit pet, ObjectGuid guid1, uint spellid, ActiveStates flag, ObjectGuid guid2, float x, float y, float z)
	{
		var charmInfo = pet.GetCharmInfo();

		if (charmInfo == null)
		{
			Log.outError(LogFilter.Network,
						"WorldSession.HandlePetAction(petGuid: {0}, tagGuid: {1}, spellId: {2}, flag: {3}): object (GUID: {4} Entry: {5} TypeId: {6}) is considered pet-like but doesn't have a charminfo!",
						guid1,
						guid2,
						spellid,
						flag,
						pet.GUID.ToString(),
						pet.Entry,
						pet.TypeId);

			return;
		}

		switch (flag)
		{
			case ActiveStates.Command: //0x07
				switch ((CommandStates)spellid)
				{
					case CommandStates.Stay: // flat = 1792  //STAY
						pet.MotionMaster.Clear(MovementGeneratorPriority.Normal);
						pet.MotionMaster.MoveIdle();
						charmInfo.SetCommandState(CommandStates.Stay);

						charmInfo.SetIsCommandAttack(false);
						charmInfo.SetIsAtStay(true);
						charmInfo.SetIsCommandFollow(false);
						charmInfo.SetIsFollowing(false);
						charmInfo.SetIsReturning(false);
						charmInfo.SaveStayPosition();

						break;
					case CommandStates.Follow: // spellid = 1792  //FOLLOW
						pet.AttackStop();
						pet.InterruptNonMeleeSpells(false);
						pet.MotionMaster.MoveFollow(Player, SharedConst.PetFollowDist, pet.FollowAngle);
						charmInfo.SetCommandState(CommandStates.Follow);

						charmInfo.SetIsCommandAttack(false);
						charmInfo.SetIsAtStay(false);
						charmInfo.SetIsReturning(true);
						charmInfo.SetIsCommandFollow(true);
						charmInfo.SetIsFollowing(false);

						break;
					case CommandStates.Attack: // spellid = 1792  //ATTACK
					{
						// Can't attack if owner is pacified
						if (Player.HasAuraType(AuraType.ModPacify))
							// @todo Send proper error message to client
							return;

						// only place where pet can be player
						var TargetUnit = Global.ObjAccessor.GetUnit(Player, guid2);

						if (!TargetUnit)
							return;

						var owner = pet.OwnerUnit;

						if (owner)
							if (!owner.IsValidAttackTarget(TargetUnit))
								return;

						// This is true if pet has no target or has target but targets differs.
						if (pet.Victim != TargetUnit || !pet.GetCharmInfo().IsCommandAttack())
						{
							if (pet.Victim)
								pet.AttackStop();

							if (!pet.IsTypeId(TypeId.Player) && pet.AsCreature.IsAIEnabled)
							{
								charmInfo.SetIsCommandAttack(true);
								charmInfo.SetIsAtStay(false);
								charmInfo.SetIsFollowing(false);
								charmInfo.SetIsCommandFollow(false);
								charmInfo.SetIsReturning(false);

								var AI = pet.AsCreature.AI;

								if (AI is PetAI)
									((PetAI)AI)._AttackStart(TargetUnit); // force target switch
								else
									AI.AttackStart(TargetUnit);

								//10% chance to play special pet attack talk, else growl
								if (pet.IsPet && pet.AsPet.PetType == PetType.Summon && pet != TargetUnit && RandomHelper.IRand(0, 100) < 10)
									pet.SendPetTalk(PetTalk.Attack);
								else
									// 90% chance for pet and 100% chance for charmed creature
									pet.SendPetAIReaction(guid1);
							}
							else // charmed player
							{
								charmInfo.SetIsCommandAttack(true);
								charmInfo.SetIsAtStay(false);
								charmInfo.SetIsFollowing(false);
								charmInfo.SetIsCommandFollow(false);
								charmInfo.SetIsReturning(false);

								pet.Attack(TargetUnit, true);
								pet.SendPetAIReaction(guid1);
							}
						}

						break;
					}
					case CommandStates.Abandon: // abandon (hunter pet) or dismiss (summoned pet)
						if (pet.CharmerGUID == Player.GUID)
						{
							Player.StopCastingCharm();
						}
						else if (pet.OwnerGUID == Player.GUID)
						{
							if (pet.IsPet)
							{
								if (pet.AsPet.PetType == PetType.Hunter)
									Player.RemovePet(pet.AsPet, PetSaveMode.AsDeleted);
								else
									Player.RemovePet(pet.AsPet, PetSaveMode.NotInSlot);
							}
							else if (pet.HasUnitTypeMask(UnitTypeMask.Minion))
							{
								((Minion)pet).UnSummon();
							}
						}

						break;
					case CommandStates.MoveTo:
						pet.StopMoving();
						pet.MotionMaster.Clear();
						pet.MotionMaster.MovePoint(0, x, y, z);
						charmInfo.SetCommandState(CommandStates.MoveTo);

						charmInfo.SetIsCommandAttack(false);
						charmInfo.SetIsAtStay(true);
						charmInfo.SetIsFollowing(false);
						charmInfo.SetIsReturning(false);
						charmInfo.SaveStayPosition();

						break;
					default:
						Log.outError(LogFilter.Network, "WORLD: unknown PET flag Action {0} and spellid {1}.", flag, spellid);

						break;
				}

				break;
			case ActiveStates.Reaction: // 0x6
				switch ((ReactStates)spellid)
				{
					case ReactStates.Passive: //passive
						pet.AttackStop();
						goto case ReactStates.Defensive;
					case ReactStates.Defensive:  //recovery
					case ReactStates.Aggressive: //activete
						if (pet.IsTypeId(TypeId.Unit))
							pet.AsCreature.ReactState = (ReactStates)spellid;

						break;
				}

				break;
			case ActiveStates.Disabled: // 0x81    spell (disabled), ignore
			case ActiveStates.Passive:  // 0x01
			case ActiveStates.Enabled:  // 0xC1    spell
			{
				Unit unit_target = null;

				if (!guid2.IsEmpty)
					unit_target = Global.ObjAccessor.GetUnit(Player, guid2);

				// do not cast unknown spells
				var spellInfo = Global.SpellMgr.GetSpellInfo(spellid, pet.Map.DifficultyID);

				if (spellInfo == null)
				{
					Log.outError(LogFilter.Network, "WORLD: unknown PET spell id {0}", spellid);

					return;
				}

				foreach (var spellEffectInfo in spellInfo.Effects)
					if (spellEffectInfo.TargetA.Target == Targets.UnitSrcAreaEnemy || spellEffectInfo.TargetA.Target == Targets.UnitDestAreaEnemy || spellEffectInfo.TargetA.Target == Targets.DestDynobjEnemy)
						return;

				// do not cast not learned spells
				if (!pet.HasSpell(spellid) || spellInfo.IsPassive)
					return;

				//  Clear the flags as if owner clicked 'attack'. AI will reset them
				//  after AttackStart, even if spell failed
				if (pet.GetCharmInfo() != null)
				{
					pet.GetCharmInfo().SetIsAtStay(false);
					pet.GetCharmInfo().SetIsCommandAttack(true);
					pet.GetCharmInfo().SetIsReturning(false);
					pet.GetCharmInfo().SetIsFollowing(false);
				}

				Spell spell = new(pet, spellInfo, TriggerCastFlags.None);

				var result = spell.CheckPetCast(unit_target);

				//auto turn to target unless possessed
				if (result == SpellCastResult.UnitNotInfront && !pet.IsPossessed && !pet.IsVehicle)
				{
					var unit_target2 = spell.Targets.UnitTarget;

					if (unit_target)
					{
						if (!pet.HasSpellFocus())
							pet.SetInFront(unit_target);

						var player = unit_target.AsPlayer;

						if (player)
							pet.SendUpdateToPlayer(player);
					}
					else if (unit_target2)
					{
						if (!pet.HasSpellFocus())
							pet.SetInFront(unit_target2);

						var player = unit_target2.AsPlayer;

						if (player)
							pet.SendUpdateToPlayer(player);
					}

					var powner = pet.CharmerOrOwner;

					if (powner)
					{
						var player = powner.AsPlayer;

						if (player)
							pet.SendUpdateToPlayer(player);
					}

					result = SpellCastResult.SpellCastOk;
				}

				if (result == SpellCastResult.SpellCastOk)
				{
					unit_target = spell.Targets.UnitTarget;

					//10% chance to play special pet attack talk, else growl
					//actually this only seems to happen on special spells, fire shield for imp, torment for voidwalker, but it's stupid to check every spell
					if (pet.IsPet && (pet.AsPet.PetType == PetType.Summon) && (pet != unit_target) && (RandomHelper.IRand(0, 100) < 10))
						pet.SendPetTalk(PetTalk.SpecialSpell);
					else
						pet.SendPetAIReaction(guid1);

					if (unit_target && !Player.IsFriendlyTo(unit_target) && !pet.IsPossessed && !pet.IsVehicle)
						// This is true if pet has no target or has target but targets differs.
						if (pet.Victim != unit_target)
						{
							var ai = pet.AsCreature.AI;

							if (ai != null)
							{
								var petAI = (PetAI)ai;

								if (petAI != null)
									petAI._AttackStart(unit_target); // force victim switch
								else
									ai.AttackStart(unit_target);
							}
						}

					spell.Prepare(spell.Targets);
				}
				else
				{
					if (pet.IsPossessed || pet.IsVehicle) // @todo: confirm this check
						Spell.SendCastResult(Player, spellInfo, spell.SpellVisual, spell.CastId, result);
					else
						spell.SendPetCastResult(result);

					if (!pet.SpellHistory.HasCooldown(spellid))
						pet.SpellHistory.ResetCooldown(spellid, true);

					spell.Finish(result);
					spell.Dispose();

					// reset specific flags in case of spell fail. AI will reset other flags
					if (pet.GetCharmInfo() != null)
						pet.GetCharmInfo().SetIsCommandAttack(false);
				}

				break;
			}
			default:
				Log.outError(LogFilter.Network, "WORLD: unknown PET flag Action {0} and spellid {1}.", flag, spellid);

				break;
		}
	}

	void SendQueryPetNameResponse(ObjectGuid guid)
	{
		QueryPetNameResponse response = new();
		response.UnitGUID = guid;

		var unit = ObjectAccessor.GetCreatureOrPetOrVehicle(Player, guid);

		if (unit)
		{
			response.Allow = true;
			response.Timestamp = unit.UnitData.PetNameTimestamp;
			response.Name = unit.GetName();

			var pet = unit.AsPet;

			if (pet)
			{
				var names = pet.GetDeclinedNames();

				if (names != null)
				{
					response.HasDeclined = true;
					response.DeclinedNames = names;
				}
			}
		}

		Player.SendPacket(response);
	}

	bool CheckStableMaster(ObjectGuid guid)
	{
		// spell case or GM
		if (guid == Player.GUID)
		{
			if (!Player.IsGameMaster && !Player.HasAuraType(AuraType.OpenStable))
			{
				Log.outDebug(LogFilter.Network, "{0} attempt open stable in cheating way.", guid.ToString());

				return false;
			}
		}
		// stable master case
		else
		{
			if (!Player.GetNPCIfCanInteractWith(guid, NPCFlags.StableMaster, NPCFlags2.None))
			{
				Log.outDebug(LogFilter.Network, "Stablemaster {0} not found or you can't interact with him.", guid.ToString());

				return false;
			}
		}

		return true;
	}

	[WorldPacketHandler(ClientOpcodes.PetSetAction)]
	void HandlePetSetAction(PetSetAction packet)
	{
		var petguid = packet.PetGUID;
		var pet = Global.ObjAccessor.GetUnit(Player, petguid);

		if (!pet || pet != Player.GetFirstControlled())
		{
			Log.outError(LogFilter.Network, "HandlePetSetAction: Unknown {0} or pet owner {1}", petguid.ToString(), Player.GUID.ToString());

			return;
		}

		var charmInfo = pet.GetCharmInfo();

		if (charmInfo == null)
		{
			Log.outError(LogFilter.Network, "WorldSession.HandlePetSetAction: {0} is considered pet-like but doesn't have a charminfo!", pet.GUID.ToString());

			return;
		}

		List<Unit> pets = new();

		foreach (var controlled in _player.Controlled)
			if (controlled.Entry == pet.Entry && controlled.IsAlive)
				pets.Add(controlled);

		var position = packet.Index;
		var actionData = packet.Action;

		var spell_id = UnitActionBarEntry.UNIT_ACTION_BUTTON_ACTION(actionData);
		var act_state = (ActiveStates)UnitActionBarEntry.UNIT_ACTION_BUTTON_TYPE(actionData);

		Log.outDebug(LogFilter.Network, "Player {0} has changed pet spell action. Position: {1}, Spell: {2}, State: {3}", Player.GetName(), position, spell_id, act_state);

		foreach (var petControlled in pets)
			//if it's act for spell (en/disable/cast) and there is a spell given (0 = remove spell) which pet doesn't know, don't add
			if (!((act_state == ActiveStates.Enabled || act_state == ActiveStates.Disabled || act_state == ActiveStates.Passive) && spell_id != 0 && !petControlled.HasSpell(spell_id)))
			{
				var spellInfo = Global.SpellMgr.GetSpellInfo(spell_id, petControlled.Map.DifficultyID);

				if (spellInfo != null)
				{
					//sign for autocast
					if (act_state == ActiveStates.Enabled)
					{
						if (petControlled.TypeId == TypeId.Unit && petControlled.IsPet)
							((Pet)petControlled).ToggleAutocast(spellInfo, true);
						else
							foreach (var unit in Player.Controlled)
								if (unit.Entry == petControlled.Entry)
									unit.GetCharmInfo().ToggleCreatureAutocast(spellInfo, true);
					}
					//sign for no/turn off autocast
					else if (act_state == ActiveStates.Disabled)
					{
						if (petControlled.TypeId == TypeId.Unit && petControlled.IsPet)
							petControlled.AsPet.ToggleAutocast(spellInfo, false);
						else
							foreach (var unit in Player.Controlled)
								if (unit.Entry == petControlled.Entry)
									unit.GetCharmInfo().ToggleCreatureAutocast(spellInfo, false);
					}
				}

				charmInfo.SetActionBar((byte)position, spell_id, act_state);
			}
	}

	[WorldPacketHandler(ClientOpcodes.PetRename)]
	void HandlePetRename(PetRename packet)
	{
		var petguid = packet.RenameData.PetGUID;
		var isdeclined = packet.RenameData.HasDeclinedNames;
		var name = packet.RenameData.NewName;

		var petStable = _player.PetStable1;
		var pet = ObjectAccessor.GetPet(Player, petguid);

		// check it!
		if (!pet ||
			!pet.IsPet ||
			pet.AsPet.PetType != PetType.Hunter ||
			!pet.HasPetFlag(UnitPetFlags.CanBeRenamed) ||
			pet.OwnerGUID != _player.GUID ||
			pet.GetCharmInfo() == null ||
			petStable == null ||
			petStable.GetCurrentPet() == null ||
			petStable.GetCurrentPet().PetNumber != pet.GetCharmInfo().GetPetNumber())
			return;

		var res = ObjectManager.CheckPetName(name);

		if (res != PetNameInvalidReason.Success)
		{
			SendPetNameInvalid(res, name, null);

			return;
		}

		if (Global.ObjectMgr.IsReservedName(name))
		{
			SendPetNameInvalid(PetNameInvalidReason.Reserved, name, null);

			return;
		}

		pet.SetName(name);
		pet.GroupUpdateFlag = GroupUpdatePetFlags.Name;
		pet.RemovePetFlag(UnitPetFlags.CanBeRenamed);

		petStable.GetCurrentPet().Name = name;
		petStable.GetCurrentPet().WasRenamed = true;

		PreparedStatement stmt;
		SQLTransaction trans = new();

		if (isdeclined)
		{
			stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_PET_DECLINEDNAME);
			stmt.AddValue(0, pet.GetCharmInfo().GetPetNumber());
			trans.Append(stmt);

			stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHAR_PET_DECLINEDNAME);
			stmt.AddValue(0, pet.GetCharmInfo().GetPetNumber());
			stmt.AddValue(1, Player.GUID.ToString());

			for (byte i = 0; i < SharedConst.MaxDeclinedNameCases; i++)
				stmt.AddValue(i + 1, packet.RenameData.DeclinedNames.Name[i]);

			trans.Append(stmt);
		}

		stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHAR_PET_NAME);
		stmt.AddValue(0, name);
		stmt.AddValue(1, Player.GUID.ToString());
		stmt.AddValue(2, pet.GetCharmInfo().GetPetNumber());
		trans.Append(stmt);

		DB.Characters.CommitTransaction(trans);

		pet.SetPetNameTimestamp((uint)GameTime.GetGameTime()); // cast can't be helped
	}

	[WorldPacketHandler(ClientOpcodes.PetAbandon)]
	void HandlePetAbandon(PetAbandon packet)
	{
		if (!Player.IsInWorld)
			return;

		// pet/charmed
		var pet = ObjectAccessor.GetCreatureOrPetOrVehicle(Player, packet.Pet);

		if (pet && pet.AsPet && pet.AsPet.PetType == PetType.Hunter)
			_player.RemovePet((Pet)pet, PetSaveMode.AsDeleted);
	}

	[WorldPacketHandler(ClientOpcodes.PetSpellAutocast, Processing = PacketProcessing.Inplace)]
	void HandlePetSpellAutocast(PetSpellAutocast packet)
	{
		var pet = ObjectAccessor.GetCreatureOrPetOrVehicle(Player, packet.PetGUID);

		if (!pet)
		{
			Log.outError(LogFilter.Network, "WorldSession.HandlePetSpellAutocast: {0} not found.", packet.PetGUID.ToString());

			return;
		}

		if (pet != Player.GetGuardianPet() && pet != Player.Charmed)
		{
			Log.outError(LogFilter.Network,
						"WorldSession.HandlePetSpellAutocast: {0} isn't pet of player {1} ({2}).",
						packet.PetGUID.ToString(),
						Player.GetName(),
						Player.GUID.ToString());

			return;
		}

		var spellInfo = Global.SpellMgr.GetSpellInfo(packet.SpellID, pet.Map.DifficultyID);

		if (spellInfo == null)
		{
			Log.outError(LogFilter.Network, "WorldSession.HandlePetSpellAutocast: Unknown spell id {0} used by {1}.", packet.SpellID, packet.PetGUID.ToString());

			return;
		}

		List<Unit> pets = new();

		foreach (var controlled in _player.Controlled)
			if (controlled.Entry == pet.Entry && controlled.IsAlive)
				pets.Add(controlled);

		foreach (var petControlled in pets)
		{
			// do not add not learned spells/ passive spells
			if (!petControlled.HasSpell(packet.SpellID) || !spellInfo.IsAutocastable)
				return;

			var charmInfo = petControlled.GetCharmInfo();

			if (charmInfo == null)
			{
				Log.outError(LogFilter.Network, "WorldSession.HandlePetSpellAutocastOpcod: object {0} is considered pet-like but doesn't have a charminfo!", petControlled.GUID.ToString());

				return;
			}

			if (petControlled.IsPet)
				petControlled.AsPet.ToggleAutocast(spellInfo, packet.AutocastEnabled);
			else
				charmInfo.ToggleCreatureAutocast(spellInfo, packet.AutocastEnabled);

			charmInfo.SetSpellAutocast(spellInfo, packet.AutocastEnabled);
		}
	}

	void SendPetNameInvalid(PetNameInvalidReason error, string name, DeclinedName declinedName)
	{
		PetNameInvalid petNameInvalid = new();
		petNameInvalid.Result = error;
		petNameInvalid.RenameData.NewName = name;

		for (var i = 0; i < SharedConst.MaxDeclinedNameCases; i++)
			petNameInvalid.RenameData.DeclinedNames.Name[i] = declinedName.Name[i];

		SendPacket(petNameInvalid);
	}
}