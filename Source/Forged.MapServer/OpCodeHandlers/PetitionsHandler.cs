// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Cache;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Forged.MapServer.Globals.Caching;
using Forged.MapServer.Guilds;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Petition;
using Forged.MapServer.Server;
using Framework.Constants;
using Framework.Database;
using Game.Common;
using Game.Common.Handlers;
using Microsoft.Extensions.Configuration;
using Serilog;
// ReSharper disable UnusedMember.Local

namespace Forged.MapServer.OpCodeHandlers;

public class PetitionsHandler : IWorldSessionHandler
{
    private readonly ClassFactory _classFactory;
    private readonly WorldSession _session;
	private readonly PetitionManager _petitionManager;
    private readonly IConfiguration _config;
    private readonly GuildManager _guildManager;
    private readonly GameObjectManager _gameObjectManager;
	private readonly CharacterCache _characterCache;
	private readonly ObjectAccessor _objectAccessor;
	private readonly CharacterDatabase _characterDatabase;
    private readonly ItemTemplateCache _itemTemplateCache;

    public PetitionsHandler(ClassFactory classFactory, WorldSession worldSession, PetitionManager petitionManager,
                            IConfiguration config, GuildManager guildManager, GameObjectManager gameObjectManager, CharacterCache characterCache,
                            ObjectAccessor objectAccessor, CharacterDatabase characterDatabase, ItemTemplateCache itemTemplateCache)
    {
		_classFactory = classFactory;
		_session = worldSession;
		_petitionManager = petitionManager;
		_config = config;
		_guildManager = guildManager;
		_gameObjectManager = gameObjectManager;
		_characterCache = characterCache;
		_objectAccessor = objectAccessor;
		_characterDatabase = characterDatabase;
        _itemTemplateCache = itemTemplateCache;
    }

    public void SendPetitionQuery(ObjectGuid petitionGuid)
	{
		QueryPetitionResponse responsePacket = new()
        {
            PetitionID = (uint)petitionGuid.Counter // PetitionID (in Trinity always same as GUID_LOPART(petition guid))
        };

        var petition = _petitionManager.GetPetition(petitionGuid);

		if (petition == null)
		{
			responsePacket.Allow = false;
			_session.SendPacket(responsePacket);
			Log.Logger.Debug($"CMSG_PETITION_Select failed for petition ({petitionGuid})");

			return;
		}

		var reqSignatures = _config.GetValue("MinPetitionSigns", 4u);
		
        responsePacket.Allow = true;
		responsePacket.Info = new PetitionInfo()
        {
            PetitionID = (int)petitionGuid.Counter,
            Petitioner = petition.OwnerGuid,
            MinSignatures = reqSignatures,
            MaxSignatures = reqSignatures,
            Title = petition.PetitionName
        };

        _session.SendPacket(responsePacket);
	}

	public void SendPetitionShowList(ObjectGuid guid)
	{
		var creature = _session.Player.GetNPCIfCanInteractWith(guid, NPCFlags.Petitioner, NPCFlags2.None);

		if (creature == null)
		{
			Log.Logger.Debug("WORLD: HandlePetitionShowListOpcode - {0} not found or you can't interact with him.", guid.ToString());

			return;
		}

		WorldPacket data = new(ServerOpcodes.PetitionShowList);
		data.WritePackedGuid(guid); // npc guid
        _session.SendPacket(new ServerPetitionShowList()
        {
            Unit = guid,
            Price = _config.GetValue("Guild.CharterCost", 1000u)
        });
	}

	[WorldPacketHandler(ClientOpcodes.PetitionBuy)]
	void HandlePetitionBuy(PetitionBuy packet)
	{
		// prevent cheating
		var creature = _session.Player.GetNPCIfCanInteractWith(packet.Unit, NPCFlags.Petitioner, NPCFlags2.None);

		if (creature == null)
		{
			Log.Logger.Debug("WORLD: HandlePetitionBuyOpcode - {0} not found or you can't interact with him.", packet.Unit.ToString());

			return;
		}

		// remove fake death
		if (_session.Player.HasUnitState(UnitState.Died))
			_session.Player.RemoveAurasByType(AuraType.FeignDeath);

		var charterItemID = GuildConst.CharterItemId;
		var cost = _config.GetValue("Guild.CharterCost", 1000u);

        // do not let if already in guild.
        if (_session.Player.GuildId != 0)
			return;

		if (_guildManager.GetGuildByName(packet.Title) != null)
		{
			Guild.SendCommandResult(_session, GuildCommandType.CreateGuild, GuildCommandError.NameExists_S, packet.Title);

			return;
		}

		if (_gameObjectManager.IsReservedName(packet.Title) || !_gameObjectManager.IsValidCharterName(packet.Title))
		{
			Guild.SendCommandResult(_session, GuildCommandType.CreateGuild, GuildCommandError.NameInvalid, packet.Title);

			return;
		}

		var pProto = _itemTemplateCache.GetItemTemplate(charterItemID);

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

		if (charter == null)
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
		ServerPetitionShowSignatures signaturesPacket = new()
        {
            Item = petition.PetitionGuid,
            Owner = petition.OwnerGuid,
            OwnerAccountID = ObjectGuid.Create(HighGuid.WowAccount, _characterCache.GetCharacterAccountIdByGuid(petition.OwnerGuid)),
            PetitionID = (int)petition.PetitionGuid.Counter
        };

        foreach (var signature in petition.Signatures)
		{
			ServerPetitionShowSignatures.PetitionSignature signaturePkt = new()
            {
                Signer = signature.PlayerGuid,
                Choice = 0
            };

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

		if (item == null)
			return;

		var petition = _petitionManager.GetPetition(packet.PetitionGuid);

		if (petition == null)
		{
			Log.Logger.Debug($"CMSG_PETITION_QUERY failed for petition {packet.PetitionGuid}");

			return;
		}

		if (_guildManager.GetGuildByName(packet.NewGuildName) != null)
		{
			Guild.SendCommandResult(_session, GuildCommandType.CreateGuild, GuildCommandError.NameExists_S, packet.NewGuildName);

			return;
		}

		if (_gameObjectManager.IsReservedName(packet.NewGuildName) || !_gameObjectManager.IsValidCharterName(packet.NewGuildName))
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
		if (!_config.GetValue("AllowTwoSide.Interaction.Guild", false) && _session.Player.Team != _characterCache.GetCharacterTeamByGuid(ownerGuid))
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

		if (owner1 != null)
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
		_session.Player owner = Global.ObjAccessor.FindConnectedPlayer(petition.ownerGuid);
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

		if (player == null)
			return;

		var petition = _petitionManager.GetPetition(packet.ItemGUID);

		if (petition == null)
			return;

		if (!_config.GetValue("AllowTwoSide.Interaction.Guild", false) && _session.Player.Team != player.Team)
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

		if (item == null)
			return;

		var petition = _petitionManager.GetPetition(packet.Item);

		if (petition == null)
		{
			Log.Logger.Error("_session.Player {0} ({1}) tried to turn in petition ({2}) that is not present in the database", _session.Player.GetName(), _session.Player.GUID.ToString(), packet.Item.ToString());

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
		if (_guildManager.GetGuildByName(name) != null)
		{
			Guild.SendCommandResult(_session, GuildCommandType.CreateGuild, GuildCommandError.NameExists_S, name);

			return;
		}

		var signatures = petition.Signatures; // we need a copy, Guild::AddMember invalidates petition
		var requiredSignatures = _config.GetValue("MinPetitionSigns", 4);

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
		var guild = _classFactory.Resolve<Guild>();

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
		Log.Logger.Debug($"_session.Player {_session.Player.GetName()} ({_session.Player.GUID}) turning in petition {packet.Item}");

		resultPacket.Result = PetitionTurns.Ok;
		_session.SendPacket(resultPacket);
	}

	[WorldPacketHandler(ClientOpcodes.PetitionShowList)]
	void HandlePetitionShowList(PetitionShowList packet)
	{
		SendPetitionShowList(packet.PetitionUnit);
	}
}