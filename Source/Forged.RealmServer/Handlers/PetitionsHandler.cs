// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Framework.Database;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Guilds;
using Forged.RealmServer.Networking;
using Game.Common.Handlers;
using Forged.RealmServer.Networking.Packets;
using Serilog;
using Forged.RealmServer.Globals;
using Forged.RealmServer.Cache;

namespace Forged.RealmServer;

public class PetitionsHandler : IWorldSessionHandler
{
    private readonly WorldSession _session;
    private readonly WorldConfig _worldConfig;
    private readonly PetitionManager _petitionManager;
    private readonly GuildManager _guildManager;
    private readonly GameObjectManager _gameObjectManager;
    private readonly ObjectAccessor _objectAccessor;
    private readonly CharacterDatabase _characterDatabase;
    private readonly CharacterCache _characterCache;

    public PetitionsHandler(WorldSession session, WorldConfig worldConfig, PetitionManager petitionManager,
		GuildManager guildManager, GameObjectManager gameObjectManager, ObjectAccessor objectAccessor,
		CharacterDatabase characterDatabase, CharacterCache characterCache)
    {
        _session = session;
        _worldConfig = worldConfig;
        _petitionManager = petitionManager;
        _guildManager = guildManager;
        _gameObjectManager = gameObjectManager;
        _objectAccessor = objectAccessor;
        _characterDatabase = characterDatabase;
        _characterCache = characterCache;
    }

    public void SendPetitionQuery(ObjectGuid petitionGuid)
	{
		QueryPetitionResponse responsePacket = new();
		responsePacket.PetitionID = (uint)petitionGuid.Counter; // PetitionID (in Trinity always same as GUID_LOPART(petition guid))

		var petition = _petitionManager.GetPetition(petitionGuid);

		if (petition == null)
		{
			responsePacket.Allow = false;
			_session.SendPacket(responsePacket);
			Log.Logger.Debug($"CMSG_PETITION_Select failed for petition ({petitionGuid})");

			return;
		}

		var reqSignatures = _worldConfig.GetUIntValue(WorldCfg.MinPetitionSigns);

		PetitionInfo petitionInfo = new();
		petitionInfo.PetitionID = (int)petitionGuid.Counter;
		petitionInfo.Petitioner = petition.OwnerGuid;
		petitionInfo.MinSignatures = reqSignatures;
		petitionInfo.MaxSignatures = reqSignatures;
		petitionInfo.Title = petition.PetitionName;

		responsePacket.Allow = true;
		responsePacket.Info = petitionInfo;

		_session.SendPacket(responsePacket);
	}

	public void SendPetitionShowList(ObjectGuid guid)
	{
		var creature = _session.Player.GetNPCIfCanInteractWith(guid, NPCFlags.Petitioner, NPCFlags2.None);

		if (!creature)
		{
			Log.Logger.Debug("WORLD: HandlePetitionShowListOpcode - {0} not found or you can't interact with him.", guid.ToString());

			return;
		}

		WorldPacket data = new(ServerOpcodes.PetitionShowList);
		data.WritePackedGuid(guid); // npc guid

		ServerPetitionShowList packet = new();
		packet.Unit = guid;
		packet.Price = _worldConfig.GetUIntValue(WorldCfg.CharterCostGuild);
		_session.SendPacket(packet);
	}

	[WorldPacketHandler(ClientOpcodes.PetitionBuy)]
	void HandlePetitionBuy(PetitionBuy packet)
	{
		// prevent cheating
		var creature = _session.Player.GetNPCIfCanInteractWith(packet.Unit, NPCFlags.Petitioner, NPCFlags2.None);

		if (!creature)
		{
			Log.Logger.Debug("WORLD: HandlePetitionBuyOpcode - {0} not found or you can't interact with him.", packet.Unit.ToString());

			return;
		}

		// remove fake death
		if (_session.Player.HasUnitState(UnitState.Died))
            _session.Player.RemoveAurasByType(AuraType.FeignDeath);

		var charterItemID = GuildConst.CharterItemId;
		var cost = _worldConfig.GetIntValue(WorldCfg.CharterCostGuild);

		// do not let if already in guild.
		if (_session.Player.GuildId != 0)
			return;

		if (_guildManager.GetGuildByName(packet.Title))
		{
			Guild.SendCommandResult(_session, GuildCommandType.CreateGuild, GuildCommandError.NameExists_S, packet.Title);

			return;
		}

		if (_gameObjectManager.IsReservedName(packet.Title) || !GameObjectManager.IsValidCharterName(packet.Title))
		{
			Guild.SendCommandResult(_session, GuildCommandType.CreateGuild, GuildCommandError.NameInvalid, packet.Title);

			return;
		}

		var pProto = _gameObjectManager.GetItemTemplate(charterItemID);

		if (pProto == null)
		{
            _session.Player.SendBuyError(BuyResult.CantFindItem, null, charterItemID);

			return;
		}

		if (!_session.Player.HasEnoughMoney(cost))
		{
            //player hasn't got enough money
            _session.Player.SendBuyError(BuyResult.NotEnoughtMoney, creature, charterItemID);

			return;
		}

		List<ItemPosCount> dest = new();
		var msg = _session.Player.CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, charterItemID, pProto.BuyCount);

		if (msg != InventoryResult.Ok)
		{
            _session.Player.SendEquipError(msg, null, null, charterItemID);

			return;
		}

        _session.Player.ModifyMoney(-cost);
		var charter = _session.Player.StoreNewItem(dest, charterItemID, true);

		if (!charter)
			return;

		charter.SetPetitionId((uint)charter.GUID.Counter);
		charter.SetState(ItemUpdateState.Changed, _session.Player);
        _session.Player.SendNewItem(charter, 1, true, false);

		// a petition is invalid, if both the owner and the type matches
		// we checked above, if this player is in an arenateam, so this must be
		// datacorruption
		var petition = _petitionManager.GetPetitionByOwner(_session.Player.GUID);

		if (petition != null)
		{
			// clear from petition store
			_petitionManager.RemovePetition(petition.PetitionGuid);
			Log.Logger.Debug($"Invalid petition GUID: {petition.PetitionGuid.Counter}");
		}

		// fill petition store
		_petitionManager.AddPetition(charter.GUID, _session.Player.GUID, packet.Title, false);
	}

	[WorldPacketHandler(ClientOpcodes.PetitionShowSignatures)]
	void HandlePetitionShowSignatures(PetitionShowSignatures packet)
	{
		var petition = _petitionManager.GetPetition(packet.Item);

		if (petition == null)
		{
			Log.Logger.Debug($"Petition {packet.Item} is not found for player {_session.Player.GUID.Counter} {_session.Player.GetName()}");

			return;
		}

		// if has guild => error, return;
		if (_session.Player.GuildId != 0)
			return;

		SendPetitionSigns(petition, _session.Player);
	}

	void SendPetitionSigns(Petition petition, Player sendTo)
	{
		ServerPetitionShowSignatures signaturesPacket = new();
		signaturesPacket.Item = petition.PetitionGuid;
		signaturesPacket.Owner = petition.OwnerGuid;
		signaturesPacket.OwnerAccountID = ObjectGuid.Create(HighGuid.WowAccount, _characterCache.GetCharacterAccountIdByGuid(petition.OwnerGuid));
		signaturesPacket.PetitionID = (int)petition.PetitionGuid.Counter;

		foreach (var signature in petition.Signatures)
		{
			ServerPetitionShowSignatures.PetitionSignature signaturePkt = new();
			signaturePkt.Signer = signature.PlayerGuid;
			signaturePkt.Choice = 0;
			signaturesPacket.Signatures.Add(signaturePkt);
		}

		_session.SendPacket(signaturesPacket);
	}

	[WorldPacketHandler(ClientOpcodes.QueryPetition)]
	void HandleQueryPetition(QueryPetition packet)
	{
		SendPetitionQuery(packet.ItemGUID);
	}

	[WorldPacketHandler(ClientOpcodes.PetitionRenameGuild)]
	void HandlePetitionRenameGuild(PetitionRenameGuild packet)
	{
		var item = _session.Player.GetItemByGuid(packet.PetitionGuid);

		if (!item)
			return;

		var petition = _petitionManager.GetPetition(packet.PetitionGuid);

		if (petition == null)
		{
			Log.Logger.Debug($"CMSG_PETITION_QUERY failed for petition {packet.PetitionGuid}");

			return;
		}

		if (_guildManager.GetGuildByName(packet.NewGuildName))
		{
			Guild.SendCommandResult(_session, GuildCommandType.CreateGuild, GuildCommandError.NameExists_S, packet.NewGuildName);

			return;
		}

		if (_gameObjectManager.IsReservedName(packet.NewGuildName) || !GameObjectManager.IsValidCharterName(packet.NewGuildName))
		{
			Guild.SendCommandResult(_session, GuildCommandType.CreateGuild, GuildCommandError.NameInvalid, packet.NewGuildName);

			return;
		}

		// update petition storage
		petition.UpdateName(packet.NewGuildName);

		PetitionRenameGuildResponse renameResponse = new();
		renameResponse.PetitionGuid = packet.PetitionGuid;
		renameResponse.NewGuildName = packet.NewGuildName;
		_session.SendPacket(renameResponse);
	}

	[WorldPacketHandler(ClientOpcodes.SignPetition)]
	void HandleSignPetition(SignPetition packet)
	{
		var petition = _petitionManager.GetPetition(packet.PetitionGUID);

		if (petition == null)
		{
			Log.Logger.Error($"Petition {packet.PetitionGUID} is not found for player {_session.Player.GUID} {_session.Player.GetName()}");

			return;
		}

		var ownerGuid = petition.OwnerGuid;
		var signs = petition.Signatures.Count;

		if (ownerGuid == _session.Player.GUID)
			return;

		// not let enemies sign guild charter
		if (!_worldConfig.GetBoolValue(WorldCfg.AllowTwoSideInteractionGuild) && _session.Player.Team != _characterCache.GetCharacterTeamByGuid(ownerGuid))
		{
			Guild.SendCommandResult(_session, GuildCommandType.CreateGuild, GuildCommandError.NotAllied);

			return;
		}

		if (_session.Player.GuildId != 0)
		{
			Guild.SendCommandResult(_session, GuildCommandType.InvitePlayer, GuildCommandError.AlreadyInGuild_S, _session.Player.GetName());

			return;
		}

		if (_session.Player.GuildIdInvited != 0)
		{
			Guild.SendCommandResult(_session, GuildCommandType.InvitePlayer, GuildCommandError.AlreadyInvitedToGuild_S, _session.Player.GetName());

			return;
		}

		if (++signs > 10) // client signs maximum
			return;

		// Client doesn't allow to sign petition two times by one character, but not check sign by another character from same account
		// not allow sign another player from already sign player account

		PetitionSignResults signResult = new();
		signResult.Player = _session.Player.GUID;
		signResult.Item = packet.PetitionGUID;

		var isSigned = petition.IsPetitionSignedByAccount(_session.AccountId);

		if (isSigned)
		{
			signResult.Error = PetitionSigns.AlreadySigned;

			// close at signer side
			_session.SendPacket(signResult);

			// update for owner if online
			var owner = _objectAccessor.FindConnectedPlayer(ownerGuid);

			if (owner != null)
				owner.Session.SendPacket(signResult);

			return;
		}

		// fill petition store
		petition.AddSignature(_session.AccountId, _session.Player.GUID, false);

		Log.Logger.Debug("PETITION SIGN: {0} by player: {1} ({2} Account: {3})", packet.PetitionGUID.ToString(), _session.Player.GetName(), _session.Player.GUID.ToString(), _session.AccountId);

		signResult.Error = PetitionSigns.Ok;
		_session.SendPacket(signResult);

		// update signs count on charter
		var item = _session.Player.GetItemByGuid(packet.PetitionGUID);

		if (item != null)
		{
			item.SetPetitionNumSignatures((uint)signs);
			item.SetState(ItemUpdateState.Changed, _session.Player);
		}

		// update for owner if online
		var owner1 = _objectAccessor.FindPlayer(ownerGuid);

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
		Player owner = _objectAccessor.FindConnectedPlayer(petition.ownerGuid);
		if (owner != null)                                               // petition owner online
		{
			PetitionDeclined packet = new PetitionDeclined();
			packet.Decliner = _session.Player.GetGUID();
			owner.GetSession()._session.SendPacket(packet);
		}
		*/
    }

    [WorldPacketHandler(ClientOpcodes.OfferPetition)]
	void HandleOfferPetition(OfferPetition packet)
	{
		var player = _objectAccessor.FindConnectedPlayer(packet.TargetPlayer);

		if (!player)
			return;

		var petition = _petitionManager.GetPetition(packet.ItemGUID);

		if (petition == null)
			return;

		if (!_worldConfig.GetBoolValue(WorldCfg.AllowTwoSideInteractionGuild) && _session.Player.Team != player.Team)
		{
			Guild.SendCommandResult(_session, GuildCommandType.CreateGuild, GuildCommandError.NotAllied);

			return;
		}

		if (player.GuildId != 0)
		{
			Guild.SendCommandResult(_session, GuildCommandType.InvitePlayer, GuildCommandError.AlreadyInGuild_S, _session.Player.GetName());

			return;
		}

		if (player.GuildIdInvited != 0)
		{
			Guild.SendCommandResult(_session, GuildCommandType.InvitePlayer, GuildCommandError.AlreadyInvitedToGuild_S, _session.Player.GetName());

			return;
		}

		SendPetitionSigns(petition, player);
	}

	[WorldPacketHandler(ClientOpcodes.TurnInPetition)]
	void HandleTurnInPetition(TurnInPetition packet)
	{
		// Check if player really has the required petition charter
		var item = _session.Player.GetItemByGuid(packet.Item);

		if (!item)
			return;

		var petition = _petitionManager.GetPetition(packet.Item);

		if (petition == null)
		{
			Log.Logger.Error("Player {0} ({1}) tried to turn in petition ({2}) that is not present in the database", _session.Player.GetName(), _session.Player.GUID.ToString(), packet.Item.ToString());

			return;
		}

		var name = petition.PetitionName; // we need a copy, Guild::AddMember invalidates petition

		// Only the petition owner can turn in the petition
		if (_session.Player.GUID != petition.OwnerGuid)
			return;

		TurnInPetitionResult resultPacket = new();

		// Check if player is already in a guild
		if (_session.Player.GuildId != 0)
		{
			resultPacket.Result = PetitionTurns.AlreadyInGuild;
			_session.SendPacket(resultPacket);

			return;
		}

		// Check if guild name is already taken
		if (_guildManager.GetGuildByName(name))
		{
			Guild.SendCommandResult(_session, GuildCommandType.CreateGuild, GuildCommandError.NameExists_S, name);

			return;
		}

		var signatures = petition.Signatures; // we need a copy, Guild::AddMember invalidates petition
		var requiredSignatures = _worldConfig.GetUIntValue(WorldCfg.MinPetitionSigns);

		// Notify player if signatures are missing
		if (signatures.Count < requiredSignatures)
		{
			resultPacket.Result = PetitionTurns.NeedMoreSignatures;
			_session.SendPacket(resultPacket);

			return;
		}
        // Proceed with guild/arena team creation

        // Delete charter item
        _session.Player.DestroyItem(item.BagSlot, item.Slot, true);

		// Create guild
		Guild guild = new();

		if (!guild.Create(_session.Player, name))
			return;

		// Register guild and add guild master
		_guildManager.AddGuild(guild);

		Guild.SendCommandResult(_session, GuildCommandType.CreateGuild, GuildCommandError.Success, name);

		SQLTransaction trans = new();

		// Add members from signatures
		foreach (var signature in signatures)
			guild.AddMember(trans, signature.PlayerGuid);

		_characterDatabase.CommitTransaction(trans);

		_petitionManager.RemovePetition(packet.Item);

		// created
		Log.Logger.Debug($"Player {_session.Player.GetName()} ({_session.Player.GUID}) turning in petition {packet.Item}");

		resultPacket.Result = PetitionTurns.Ok;
		_session.SendPacket(resultPacket);
	}

	[WorldPacketHandler(ClientOpcodes.PetitionShowList)]
	void HandlePetitionShowList(PetitionShowList packet)
	{
		SendPetitionShowList(packet.PetitionUnit);
	}
}