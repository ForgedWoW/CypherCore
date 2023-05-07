// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.BattlePets;
using Forged.MapServer.Globals;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.BattlePet;
using Forged.MapServer.Server;
using Framework.Constants;
using Game.Common.Handlers;
// ReSharper disable UnusedMember.Local

namespace Forged.MapServer.OpCodeHandlers;

public class BattlePetHandler : IWorldSessionHandler
{
    private readonly WorldSession _session;
    private readonly BattlePetMgr _battlePetMgr;

    public BattlePetHandler(WorldSession session, BattlePetMgr battlePetMgr)
    {
        _session = session;
        _battlePetMgr = battlePetMgr;
    }

    [WorldPacketHandler(ClientOpcodes.BattlePetDeletePet)]
    private void HandleBattlePetDeletePet(BattlePetDeletePet battlePetDeletePet)
    {
        _battlePetMgr.RemovePet(battlePetDeletePet.PetGuid);
    }

    [WorldPacketHandler(ClientOpcodes.BattlePetModifyName)]
    private void HandleBattlePetModifyName(BattlePetModifyName battlePetModifyName)
    {
        _battlePetMgr.ModifyName(battlePetModifyName.PetGuid, battlePetModifyName.Name, battlePetModifyName.DeclinedNames);
    }

    [WorldPacketHandler(ClientOpcodes.BattlePetSetBattleSlot)]
    private void HandleBattlePetSetBattleSlot(BattlePetSetBattleSlot battlePetSetBattleSlot)
    {
        var pet = _battlePetMgr.GetPet(battlePetSetBattleSlot.PetGuid);

        if (pet == null)
            return;

        var slot = _battlePetMgr.GetSlot((BattlePetSlots)battlePetSetBattleSlot.Slot);

        if (slot != null)
            slot.Pet = pet.PacketInfo;
    }

    [WorldPacketHandler(ClientOpcodes.BattlePetSetFlags)]
    private void HandleBattlePetSetFlags(BattlePetSetFlags battlePetSetFlags)
    {
        if (!_battlePetMgr.HasJournalLock)
            return;

        var pet = _battlePetMgr.GetPet(battlePetSetFlags.PetGuid);

        if (pet == null)
            return;

        if (battlePetSetFlags.ControlType == FlagsControlType.Apply)
            pet.PacketInfo.Flags |= (ushort)battlePetSetFlags.Flags;
        else
            pet.PacketInfo.Flags &= (ushort)~battlePetSetFlags.Flags;

        if (pet.SaveInfo != BattlePetSaveInfo.New)
            pet.SaveInfo = BattlePetSaveInfo.Changed;
    }

    [WorldPacketHandler(ClientOpcodes.BattlePetSummon, Processing = PacketProcessing.Inplace)]
    private void HandleBattlePetSummon(BattlePetSummon battlePetSummon)
    {
        if (_session.Player.SummonedBattlePetGUID != battlePetSummon.PetGuid)
            _battlePetMgr.SummonPet(battlePetSummon.PetGuid);
        else
            _battlePetMgr.DismissPet();
    }

    [WorldPacketHandler(ClientOpcodes.BattlePetUpdateNotify)]
    private void HandleBattlePetUpdateNotify(BattlePetUpdateNotify battlePetUpdateNotify)
    {
        _battlePetMgr.UpdateBattlePetData(battlePetUpdateNotify.PetGuid);
    }

    [WorldPacketHandler(ClientOpcodes.CageBattlePet)]
    private void HandleCageBattlePet(CageBattlePet cageBattlePet)
    {
        _battlePetMgr.CageBattlePet(cageBattlePet.PetGuid);
    }

    [WorldPacketHandler(ClientOpcodes.QueryBattlePetName)]
    private void HandleQueryBattlePetName(QueryBattlePetName queryBattlePetName)
    {
        QueryBattlePetNameResponse response = new()
        {
            BattlePetID = queryBattlePetName.BattlePetID
        };

        var summonedBattlePet = ObjectAccessor.GetCreatureOrPetOrVehicle(_session.Player, queryBattlePetName.UnitGUID);

        if (summonedBattlePet is not { IsSummon: true })
        {
            _session.SendPacket(response);

            return;
        }

        response.CreatureID = summonedBattlePet.Entry;
        response.Timestamp = summonedBattlePet.BattlePetCompanionNameTimestamp;

        var petOwner = summonedBattlePet.ToTempSummon().SummonerUnit;

        if (!petOwner.IsPlayer)
        {
            _session.SendPacket(response);

            return;
        }

        var battlePet = petOwner.AsPlayer.Session.BattlePetMgr.GetPet(queryBattlePetName.BattlePetID);

        if (battlePet == null)
        {
            _session.SendPacket(response);

            return;
        }

        response.Name = battlePet.PacketInfo.Name;

        if (battlePet.DeclinedName != null)
        {
            response.HasDeclined = true;
            response.DeclinedNames = battlePet.DeclinedName;
        }

        response.Allow = !response.Name.IsEmpty();

        _session.SendPacket(response);
    }
}