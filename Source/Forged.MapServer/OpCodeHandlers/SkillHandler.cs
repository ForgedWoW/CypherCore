// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Spell;
using Forged.MapServer.Networking.Packets.Talent;
using Forged.MapServer.Server;
using Framework.Constants;
using Game.Common.Handlers;
using Serilog;

// ReSharper disable UnusedMember.Local

namespace Forged.MapServer.OpCodeHandlers;

public class SkillHandler : IWorldSessionHandler
{
    private readonly DB2Manager _db2Manager;
    private readonly WorldSession _session;

    public SkillHandler(WorldSession session, DB2Manager db2Manager)
    {
        _session = session;
        _db2Manager = db2Manager;
    }

    [WorldPacketHandler(ClientOpcodes.ConfirmRespecWipe)]
    private void HandleConfirmRespecWipe(ConfirmRespecWipe confirmRespecWipe)
    {
        var unit = _session.Player.GetNPCIfCanInteractWith(confirmRespecWipe.RespecMaster, NPCFlags.Trainer, NPCFlags2.None);

        if (unit == null)
        {
            Log.Logger.Debug("WORLD: HandleTalentWipeConfirm - {0} not found or you can't interact with him.", confirmRespecWipe.RespecMaster.ToString());

            return;
        }

        if (confirmRespecWipe.RespecType != SpecResetType.Talents)
        {
            Log.Logger.Debug("WORLD: HandleConfirmRespecWipe - reset type {0} is not implemented.", confirmRespecWipe.RespecType);

            return;
        }

        if (!unit.CanResetTalents(_session.Player))
            return;

        // remove fake death
        if (_session.Player.HasUnitState(UnitState.Died))
            _session.Player.RemoveAurasByType(AuraType.FeignDeath);

        if (!_session.Player.ResetTalents())
            return;

        _session.Player.SendTalentsInfoData();
        unit.SpellFactory.CastSpell(14867, true); //spell: "Untalent Visual Effect"
    }

    [WorldPacketHandler(ClientOpcodes.LearnPvpTalents, Processing = PacketProcessing.Inplace)]
    private void HandleLearnPvpTalents(LearnPvpTalents packet)
    {
        LearnPvpTalentFailed learnPvpTalentFailed = new();
        var anythingLearned = false;

        foreach (var pvpTalent in packet.Talents)
        {
            var result = _session.Player.LearnPvpTalent(pvpTalent.PvPTalentID, pvpTalent.Slot, ref learnPvpTalentFailed.SpellID);

            if (result != 0)
            {
                if (learnPvpTalentFailed.Reason == 0)
                    learnPvpTalentFailed.Reason = (uint)result;

                learnPvpTalentFailed.Talents.Add(pvpTalent);
            }
            else
                anythingLearned = true;
        }

        if (learnPvpTalentFailed.Reason != 0)
            _session.SendPacket(learnPvpTalentFailed);

        if (anythingLearned)
            _session.Player.SendTalentsInfoData();
    }

    [WorldPacketHandler(ClientOpcodes.LearnTalents, Processing = PacketProcessing.Inplace)]
    private void HandleLearnTalents(LearnTalents packet)
    {
        LearnTalentFailed learnTalentFailed = new();
        var anythingLearned = false;

        foreach (uint talentId in packet.Talents)
        {
            var result = _session.Player.LearnTalent(talentId, ref learnTalentFailed.SpellID);

            if (result != 0)
            {
                if (learnTalentFailed.Reason == 0)
                    learnTalentFailed.Reason = (uint)result;

                learnTalentFailed.Talents.Add((ushort)talentId);
            }
            else
                anythingLearned = true;
        }

        if (learnTalentFailed.Reason != 0)
            _session.SendPacket(learnTalentFailed);

        if (anythingLearned)
            _session.Player.SendTalentsInfoData();
    }

    [WorldPacketHandler(ClientOpcodes.TradeSkillSetFavorite, Processing = PacketProcessing.Inplace)]
    private void HandleTradeSkillSetFavorite(TradeSkillSetFavorite tradeSkillSetFavorite)
    {
        if (!_session.Player.HasSpell(tradeSkillSetFavorite.RecipeID))
            return;

        _session.Player.SetSpellFavorite(tradeSkillSetFavorite.RecipeID, tradeSkillSetFavorite.IsFavorite);
    }

    [WorldPacketHandler(ClientOpcodes.UnlearnSkill, Processing = PacketProcessing.Inplace)]
    private void HandleUnlearnSkill(UnlearnSkill packet)
    {
        var rcEntry = _db2Manager.GetSkillRaceClassInfo(packet.SkillLine, _session.Player.Race, _session.Player.Class);

        if (rcEntry == null || !rcEntry.Flags.HasAnyFlag(SkillRaceClassInfoFlags.Unlearnable))
            return;

        _session.Player.SetSkill(packet.SkillLine, 0, 0, 0);
    }
}