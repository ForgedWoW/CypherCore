// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Game.Common.Networking;
using Game.Common.Networking.Packets.Spell;
using Game.Common.Networking.Packets.Talent;

namespace Game;

public partial class WorldSession
{
	[WorldPacketHandler(ClientOpcodes.LearnTalents, Processing = PacketProcessing.Inplace)]
	void HandleLearnTalents(LearnTalents packet)
	{
		LearnTalentFailed learnTalentFailed = new();
		var anythingLearned = false;

		foreach (uint talentId in packet.Talents)
		{
			var result = _player.LearnTalent(talentId, ref learnTalentFailed.SpellID);

			if (result != 0)
			{
				if (learnTalentFailed.Reason == 0)
					learnTalentFailed.Reason = (uint)result;

				learnTalentFailed.Talents.Add((ushort)talentId);
			}
			else
			{
				anythingLearned = true;
			}
		}

		if (learnTalentFailed.Reason != 0)
			SendPacket(learnTalentFailed);

		if (anythingLearned)
			Player.SendTalentsInfoData();
	}

	[WorldPacketHandler(ClientOpcodes.LearnPvpTalents, Processing = PacketProcessing.Inplace)]
	void HandleLearnPvpTalents(LearnPvpTalents packet)
	{
		LearnPvpTalentFailed learnPvpTalentFailed = new();
		var anythingLearned = false;

		foreach (var pvpTalent in packet.Talents)
		{
			var result = _player.LearnPvpTalent(pvpTalent.PvPTalentID, pvpTalent.Slot, ref learnPvpTalentFailed.SpellID);

			if (result != 0)
			{
				if (learnPvpTalentFailed.Reason == 0)
					learnPvpTalentFailed.Reason = (uint)result;

				learnPvpTalentFailed.Talents.Add(pvpTalent);
			}
			else
			{
				anythingLearned = true;
			}
		}

		if (learnPvpTalentFailed.Reason != 0)
			SendPacket(learnPvpTalentFailed);

		if (anythingLearned)
			_player.SendTalentsInfoData();
	}

	[WorldPacketHandler(ClientOpcodes.ConfirmRespecWipe)]
	void HandleConfirmRespecWipe(ConfirmRespecWipe confirmRespecWipe)
	{
		var unit = Player.GetNPCIfCanInteractWith(confirmRespecWipe.RespecMaster, NPCFlags.Trainer, NPCFlags2.None);

		if (unit == null)
		{
			Log.outDebug(LogFilter.Network, "WORLD: HandleTalentWipeConfirm - {0} not found or you can't interact with him.", confirmRespecWipe.RespecMaster.ToString());

			return;
		}

		if (confirmRespecWipe.RespecType != SpecResetType.Talents)
		{
			Log.outDebug(LogFilter.Network, "WORLD: HandleConfirmRespecWipe - reset type {0} is not implemented.", confirmRespecWipe.RespecType);

			return;
		}

		if (!unit.CanResetTalents(_player))
			return;

		// remove fake death
		if (Player.HasUnitState(UnitState.Died))
			Player.RemoveAurasByType(AuraType.FeignDeath);

		if (!Player.ResetTalents())
			return;

		Player.SendTalentsInfoData();
		unit.CastSpell(Player, 14867, true); //spell: "Untalent Visual Effect"
	}

	[WorldPacketHandler(ClientOpcodes.UnlearnSkill, Processing = PacketProcessing.Inplace)]
	void HandleUnlearnSkill(UnlearnSkill packet)
	{
		var rcEntry = Global.DB2Mgr.GetSkillRaceClassInfo(packet.SkillLine, Player.Race, Player.Class);

		if (rcEntry == null || !rcEntry.Flags.HasAnyFlag(SkillRaceClassInfoFlags.Unlearnable))
			return;

		Player.SetSkill(packet.SkillLine, 0, 0, 0);
	}

	[WorldPacketHandler(ClientOpcodes.TradeSkillSetFavorite, Processing = PacketProcessing.Inplace)]
	void HandleTradeSkillSetFavorite(TradeSkillSetFavorite tradeSkillSetFavorite)
	{
		if (!_player.HasSpell(tradeSkillSetFavorite.RecipeID))
			return;

		_player.SetSpellFavorite(tradeSkillSetFavorite.RecipeID, tradeSkillSetFavorite.IsFavorite);
	}
}