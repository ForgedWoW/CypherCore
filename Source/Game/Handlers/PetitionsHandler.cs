// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Framework.Database;
using Game.Entities;
using Game.Guilds;
using Game.Networking;
using Game.Networking.Packets;

namespace Game;

public partial class WorldSession
{
	public void SendPetitionQuery(ObjectGuid petitionGuid)
	{
		QueryPetitionResponse responsePacket = new();
		responsePacket.PetitionID = (uint)petitionGuid.Counter; // PetitionID (in Trinity always same as GUID_LOPART(petition guid))

		var petition = Global.PetitionMgr.GetPetition(petitionGuid);

		if (petition == null)
		{
			responsePacket.Allow = false;
			SendPacket(responsePacket);
			Log.outDebug(LogFilter.Network, $"CMSG_PETITION_Select failed for petition ({petitionGuid})");

			return;
		}

		var reqSignatures = WorldConfig.GetUIntValue(WorldCfg.MinPetitionSigns);

		PetitionInfo petitionInfo = new();
		petitionInfo.PetitionID = (int)petitionGuid.Counter;
		petitionInfo.Petitioner = petition.OwnerGuid;
		petitionInfo.MinSignatures = reqSignatures;
		petitionInfo.MaxSignatures = reqSignatures;
		petitionInfo.Title = petition.PetitionName;

		responsePacket.Allow = true;
		responsePacket.Info = petitionInfo;

		SendPacket(responsePacket);
	}

	public void SendPetitionShowList(ObjectGuid guid)
	{
		var creature = Player.GetNPCIfCanInteractWith(guid, NPCFlags.Petitioner, NPCFlags2.None);

		if (!creature)
		{
			Log.outDebug(LogFilter.Network, "WORLD: HandlePetitionShowListOpcode - {0} not found or you can't interact with him.", guid.ToString());

			return;
		}

		WorldPacket data = new(ServerOpcodes.PetitionShowList);
		data.WritePackedGuid(guid); // npc guid

		ServerPetitionShowList packet = new();
		packet.Unit = guid;
		packet.Price = WorldConfig.GetUIntValue(WorldCfg.CharterCostGuild);
		SendPacket(packet);
	}

	[WorldPacketHandler(ClientOpcodes.PetitionBuy)]
	void HandlePetitionBuy(PetitionBuy packet)
	{
		// prevent cheating
		var creature = Player.GetNPCIfCanInteractWith(packet.Unit, NPCFlags.Petitioner, NPCFlags2.None);

		if (!creature)
		{
			Log.outDebug(LogFilter.Network, "WORLD: HandlePetitionBuyOpcode - {0} not found or you can't interact with him.", packet.Unit.ToString());

			return;
		}

		// remove fake death
		if (Player.HasUnitState(UnitState.Died))
			Player.RemoveAurasByType(AuraType.FeignDeath);

		var charterItemID = GuildConst.CharterItemId;
		var cost = WorldConfig.GetIntValue(WorldCfg.CharterCostGuild);

		// do not let if already in guild.
		if (Player.GuildId != 0)
			return;

		if (Global.GuildMgr.GetGuildByName(packet.Title))
		{
			Guild.SendCommandResult(this, GuildCommandType.CreateGuild, GuildCommandError.NameExists_S, packet.Title);

			return;
		}

		if (Global.ObjectMgr.IsReservedName(packet.Title) || !ObjectManager.IsValidCharterName(packet.Title))
		{
			Guild.SendCommandResult(this, GuildCommandType.CreateGuild, GuildCommandError.NameInvalid, packet.Title);

			return;
		}

		var pProto = Global.ObjectMgr.GetItemTemplate(charterItemID);

		if (pProto == null)
		{
			Player.SendBuyError(BuyResult.CantFindItem, null, charterItemID);

			return;
		}

		if (!Player.HasEnoughMoney(cost))
		{
			//player hasn't got enough money
			Player.SendBuyError(BuyResult.NotEnoughtMoney, creature, charterItemID);

			return;
		}

		List<ItemPosCount> dest = new();
		var msg = Player.CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, charterItemID, pProto.BuyCount);

		if (msg != InventoryResult.Ok)
		{
			Player.SendEquipError(msg, null, null, charterItemID);

			return;
		}

		Player.ModifyMoney(-cost);
		var charter = Player.StoreNewItem(dest, charterItemID, true);

		if (!charter)
			return;

		charter.SetPetitionId((uint)charter.GUID.Counter);
		charter.SetState(ItemUpdateState.Changed, Player);
		Player.SendNewItem(charter, 1, true, false);

		// a petition is invalid, if both the owner and the type matches
		// we checked above, if this player is in an arenateam, so this must be
		// datacorruption
		var petition = Global.PetitionMgr.GetPetitionByOwner(_player.GUID);

		if (petition != null)
		{
			// clear from petition store
			Global.PetitionMgr.RemovePetition(petition.PetitionGuid);
			Log.outDebug(LogFilter.Network, $"Invalid petition GUID: {petition.PetitionGuid.Counter}");
		}

		// fill petition store
		Global.PetitionMgr.AddPetition(charter.GUID, _player.GUID, packet.Title, false);
	}

	[WorldPacketHandler(ClientOpcodes.PetitionShowSignatures)]
	void HandlePetitionShowSignatures(PetitionShowSignatures packet)
	{
		var petition = Global.PetitionMgr.GetPetition(packet.Item);

		if (petition == null)
		{
			Log.outDebug(LogFilter.PlayerItems, $"Petition {packet.Item} is not found for player {Player.GUID.Counter} {Player.GetName()}");

			return;
		}

		// if has guild => error, return;
		if (_player.GuildId != 0)
			return;

		SendPetitionSigns(petition, _player);
	}

	void SendPetitionSigns(Petition petition, Player sendTo)
	{
		ServerPetitionShowSignatures signaturesPacket = new();
		signaturesPacket.Item = petition.PetitionGuid;
		signaturesPacket.Owner = petition.OwnerGuid;
		signaturesPacket.OwnerAccountID = ObjectGuid.Create(HighGuid.WowAccount, Global.CharacterCacheStorage.GetCharacterAccountIdByGuid(petition.OwnerGuid));
		signaturesPacket.PetitionID = (int)petition.PetitionGuid.Counter;

		foreach (var signature in petition.Signatures)
		{
			ServerPetitionShowSignatures.PetitionSignature signaturePkt = new();
			signaturePkt.Signer = signature.PlayerGuid;
			signaturePkt.Choice = 0;
			signaturesPacket.Signatures.Add(signaturePkt);
		}

		SendPacket(signaturesPacket);
	}

	[WorldPacketHandler(ClientOpcodes.QueryPetition)]
	void HandleQueryPetition(QueryPetition packet)
	{
		SendPetitionQuery(packet.ItemGUID);
	}

	[WorldPacketHandler(ClientOpcodes.PetitionRenameGuild)]
	void HandlePetitionRenameGuild(PetitionRenameGuild packet)
	{
		var item = Player.GetItemByGuid(packet.PetitionGuid);

		if (!item)
			return;

		var petition = Global.PetitionMgr.GetPetition(packet.PetitionGuid);

		if (petition == null)
		{
			Log.outDebug(LogFilter.Network, $"CMSG_PETITION_QUERY failed for petition {packet.PetitionGuid}");

			return;
		}

		if (Global.GuildMgr.GetGuildByName(packet.NewGuildName))
		{
			Guild.SendCommandResult(this, GuildCommandType.CreateGuild, GuildCommandError.NameExists_S, packet.NewGuildName);

			return;
		}

		if (Global.ObjectMgr.IsReservedName(packet.NewGuildName) || !ObjectManager.IsValidCharterName(packet.NewGuildName))
		{
			Guild.SendCommandResult(this, GuildCommandType.CreateGuild, GuildCommandError.NameInvalid, packet.NewGuildName);

			return;
		}

		// update petition storage
		petition.UpdateName(packet.NewGuildName);

		PetitionRenameGuildResponse renameResponse = new();
		renameResponse.PetitionGuid = packet.PetitionGuid;
		renameResponse.NewGuildName = packet.NewGuildName;
		SendPacket(renameResponse);
	}

	[WorldPacketHandler(ClientOpcodes.SignPetition)]
	void HandleSignPetition(SignPetition packet)
	{
		var petition = Global.PetitionMgr.GetPetition(packet.PetitionGUID);

		if (petition == null)
		{
			Log.outError(LogFilter.Network, $"Petition {packet.PetitionGUID} is not found for player {Player.GUID} {Player.GetName()}");

			return;
		}

		var ownerGuid = petition.OwnerGuid;
		var signs = petition.Signatures.Count;

		if (ownerGuid == Player.GUID)
			return;

		// not let enemies sign guild charter
		if (!WorldConfig.GetBoolValue(WorldCfg.AllowTwoSideInteractionGuild) && Player.Team != Global.CharacterCacheStorage.GetCharacterTeamByGuid(ownerGuid))
		{
			Guild.SendCommandResult(this, GuildCommandType.CreateGuild, GuildCommandError.NotAllied);

			return;
		}

		if (Player.GuildId != 0)
		{
			Guild.SendCommandResult(this, GuildCommandType.InvitePlayer, GuildCommandError.AlreadyInGuild_S, Player.GetName());

			return;
		}

		if (Player.GuildIdInvited != 0)
		{
			Guild.SendCommandResult(this, GuildCommandType.InvitePlayer, GuildCommandError.AlreadyInvitedToGuild_S, Player.GetName());

			return;
		}

		if (++signs > 10) // client signs maximum
			return;

		// Client doesn't allow to sign petition two times by one character, but not check sign by another character from same account
		// not allow sign another player from already sign player account

		PetitionSignResults signResult = new();
		signResult.Player = Player.GUID;
		signResult.Item = packet.PetitionGUID;

		var isSigned = petition.IsPetitionSignedByAccount(AccountId);

		if (isSigned)
		{
			signResult.Error = PetitionSigns.AlreadySigned;

			// close at signer side
			SendPacket(signResult);

			// update for owner if online
			var owner = Global.ObjAccessor.FindConnectedPlayer(ownerGuid);

			if (owner != null)
				owner.Session.SendPacket(signResult);

			return;
		}

		// fill petition store
		petition.AddSignature(AccountId, _player.GUID, false);

		Log.outDebug(LogFilter.Network, "PETITION SIGN: {0} by player: {1} ({2} Account: {3})", packet.PetitionGUID.ToString(), Player.GetName(), Player.GUID.ToString(), AccountId);

		signResult.Error = PetitionSigns.Ok;
		SendPacket(signResult);

		// update signs count on charter
		var item = _player.GetItemByGuid(packet.PetitionGUID);

		if (item != null)
		{
			item.SetPetitionNumSignatures((uint)signs);
			item.SetState(ItemUpdateState.Changed, _player);
		}

		// update for owner if online
		var owner1 = Global.ObjAccessor.FindPlayer(ownerGuid);

		if (owner1)
			owner1.SendPacket(signResult);
	}

	[WorldPacketHandler(ClientOpcodes.DeclinePetition)]
	void HandleDeclinePetition(DeclinePetition packet)
	{
		// Disabled because packet isn't handled by the client in any way
		/*
		Petition petition = sPetitionMgr.GetPetition(packet.PetitionGUID);
		if (petition == null)
			return;

		// petition owner online
		Player owner = Global.ObjAccessor.FindConnectedPlayer(petition.ownerGuid);
		if (owner != null)                                               // petition owner online
		{
			PetitionDeclined packet = new PetitionDeclined();
			packet.Decliner = _player.GetGUID();
			owner.GetSession().SendPacket(packet);
		}
		*/
	}

	[WorldPacketHandler(ClientOpcodes.OfferPetition)]
	void HandleOfferPetition(OfferPetition packet)
	{
		var player = Global.ObjAccessor.FindConnectedPlayer(packet.TargetPlayer);

		if (!player)
			return;

		var petition = Global.PetitionMgr.GetPetition(packet.ItemGUID);

		if (petition == null)
			return;

		if (!WorldConfig.GetBoolValue(WorldCfg.AllowTwoSideInteractionGuild) && Player.Team != player.Team)
		{
			Guild.SendCommandResult(this, GuildCommandType.CreateGuild, GuildCommandError.NotAllied);

			return;
		}

		if (player.GuildId != 0)
		{
			Guild.SendCommandResult(this, GuildCommandType.InvitePlayer, GuildCommandError.AlreadyInGuild_S, Player.GetName());

			return;
		}

		if (player.GuildIdInvited != 0)
		{
			Guild.SendCommandResult(this, GuildCommandType.InvitePlayer, GuildCommandError.AlreadyInvitedToGuild_S, Player.GetName());

			return;
		}

		SendPetitionSigns(petition, player);
	}

	[WorldPacketHandler(ClientOpcodes.TurnInPetition)]
	void HandleTurnInPetition(TurnInPetition packet)
	{
		// Check if player really has the required petition charter
		var item = Player.GetItemByGuid(packet.Item);

		if (!item)
			return;

		var petition = Global.PetitionMgr.GetPetition(packet.Item);

		if (petition == null)
		{
			Log.outError(LogFilter.Network, "Player {0} ({1}) tried to turn in petition ({2}) that is not present in the database", Player.GetName(), Player.GUID.ToString(), packet.Item.ToString());

			return;
		}

		var name = petition.PetitionName; // we need a copy, Guild::AddMember invalidates petition

		// Only the petition owner can turn in the petition
		if (Player.GUID != petition.OwnerGuid)
			return;

		TurnInPetitionResult resultPacket = new();

		// Check if player is already in a guild
		if (Player.GuildId != 0)
		{
			resultPacket.Result = PetitionTurns.AlreadyInGuild;
			SendPacket(resultPacket);

			return;
		}

		// Check if guild name is already taken
		if (Global.GuildMgr.GetGuildByName(name))
		{
			Guild.SendCommandResult(this, GuildCommandType.CreateGuild, GuildCommandError.NameExists_S, name);

			return;
		}

		var signatures = petition.Signatures; // we need a copy, Guild::AddMember invalidates petition
		var requiredSignatures = WorldConfig.GetUIntValue(WorldCfg.MinPetitionSigns);

		// Notify player if signatures are missing
		if (signatures.Count < requiredSignatures)
		{
			resultPacket.Result = PetitionTurns.NeedMoreSignatures;
			SendPacket(resultPacket);

			return;
		}
		// Proceed with guild/arena team creation

		// Delete charter item
		Player.DestroyItem(item.BagSlot, item.Slot, true);

		// Create guild
		Guild guild = new();

		if (!guild.Create(Player, name))
			return;

		// Register guild and add guild master
		Global.GuildMgr.AddGuild(guild);

		Guild.SendCommandResult(this, GuildCommandType.CreateGuild, GuildCommandError.Success, name);

		SQLTransaction trans = new();

		// Add members from signatures
		foreach (var signature in signatures)
			guild.AddMember(trans, signature.PlayerGuid);

		DB.Characters.CommitTransaction(trans);

		Global.PetitionMgr.RemovePetition(packet.Item);

		// created
		Log.outDebug(LogFilter.Network, $"Player {Player.GetName()} ({Player.GUID}) turning in petition {packet.Item}");

		resultPacket.Result = PetitionTurns.Ok;
		SendPacket(resultPacket);
	}

	[WorldPacketHandler(ClientOpcodes.PetitionShowList)]
	void HandlePetitionShowList(PetitionShowList packet)
	{
		SendPetitionShowList(packet.PetitionUnit);
	}
}