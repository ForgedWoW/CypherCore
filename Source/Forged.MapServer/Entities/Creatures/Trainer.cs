// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Chat;
using Forged.MapServer.Conditions;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Networking.Packets.NPC;
using Forged.MapServer.Spells;
using Framework.Constants;
using Serilog;

namespace Forged.MapServer.Entities.Creatures;

public class Trainer
{
    private readonly BattlePetData _battlePet;
    private readonly ConditionManager _conditionManager;
    private readonly string[] _greeting = new string[(int)Locale.Total];
    private readonly uint _id;
    private readonly SpellManager _spellManager;
    private readonly List<TrainerSpell> _spells;
    private readonly TrainerType _type;

    public Trainer(uint id, TrainerType type, string greeting, List<TrainerSpell> spells, ConditionManager conditionManager, BattlePetData battlePet, SpellManager spellManager)
    {
        _id = id;
        _type = type;
        _spells = spells;
        _conditionManager = conditionManager;
        _battlePet = battlePet;
        _spellManager = spellManager;

        _greeting[(int)Locale.enUS] = greeting;
    }

    public void AddGreetingLocale(Locale locale, string greeting)
    {
        _greeting[(int)locale] = greeting;
    }

    public void SendSpells(Creature npc, Player player, Locale locale)
    {
        var reputationDiscount = player.GetReputationPriceDiscount(npc);

        TrainerList trainerList = new()
        {
            TrainerGUID = npc.GUID,
            TrainerType = (int)_type,
            TrainerID = (int)_id,
            Greeting = GetGreeting(locale)
        };

        foreach (var trainerSpell in _spells.Where(trainerSpell => player.IsSpellFitByClassAndRace(trainerSpell.SpellId)))
        {
            if (!_conditionManager.IsObjectMeetingTrainerSpellConditions(_id, trainerSpell.SpellId, player))
            {
                Log.Logger.Debug($"SendSpells: conditions not met for trainer id {_id} spell {trainerSpell.SpellId} player '{player.GetName()}' ({player.GUID})");

                continue;
            }

            TrainerListSpell trainerListSpell = new()
            {
                SpellID = trainerSpell.SpellId,
                MoneyCost = (uint)(trainerSpell.MoneyCost * reputationDiscount),
                ReqSkillLine = trainerSpell.ReqSkillLine,
                ReqSkillRank = trainerSpell.ReqSkillRank,
                ReqAbility = trainerSpell.ReqAbility.ToArray(),
                Usable = GetSpellState(player, trainerSpell),
                ReqLevel = trainerSpell.ReqLevel
            };

            trainerList.Spells.Add(trainerListSpell);
        }

        player.SendPacket(trainerList);
    }

    public void TeachSpell(Creature npc, Player player, uint spellId)
    {
        var trainerSpell = GetSpell(spellId);

        if (trainerSpell == null || !CanTeachSpell(player, trainerSpell))
        {
            SendTeachFailure(npc, player, spellId, TrainerFailReason.Unavailable);

            return;
        }

        var sendSpellVisual = true;
        var speciesEntry = _battlePet.GetBattlePetSpeciesBySpell(trainerSpell.SpellId);

        if (speciesEntry != null)
        {
            if (player.Session.BattlePetMgr.HasMaxPetCount(speciesEntry, player.GUID))
                // Don't send any error to client (intended)
                return;

            sendSpellVisual = false;
        }

        var reputationDiscount = player.GetReputationPriceDiscount(npc);
        var moneyCost = (long)(trainerSpell.MoneyCost * reputationDiscount);

        if (!player.HasEnoughMoney(moneyCost))
        {
            SendTeachFailure(npc, player, spellId, TrainerFailReason.NotEnoughMoney);

            return;
        }

        player.ModifyMoney(-moneyCost);

        if (sendSpellVisual)
        {
            npc.WorldObjectCombat.SendPlaySpellVisualKit(179, 0, 0);    // 53 SpellCastDirected
            player.WorldObjectCombat.SendPlaySpellVisualKit(362, 1, 0); // 113 EmoteSalute
        }

        // learn explicitly or cast explicitly
        if (_spellManager.GetSpellInfo(trainerSpell.SpellId).HasEffect(SpellEffectName.LearnSpell))
            player.SpellFactory.CastSpell(player, trainerSpell.SpellId, true);
        else
        {
            var dependent = false;

            if (speciesEntry != null)
            {
                player.Session.BattlePetMgr.AddPet(speciesEntry.Id, _battlePet.SelectPetDisplay(speciesEntry), _battlePet.RollPetBreed(speciesEntry.Id), _battlePet.GetDefaultPetQuality(speciesEntry.Id));
                // If the spell summons a battle pet, we fake that it has been learned and the battle pet is added
                // marking as dependent prevents saving the spell to database (intended)
                dependent = true;
            }

            player.LearnSpell(trainerSpell.SpellId, dependent);
        }
    }

    private bool CanTeachSpell(Player player, TrainerSpell trainerSpell)
    {
        var state = GetSpellState(player, trainerSpell);

        if (state != TrainerSpellState.Available)
            return false;

        var trainerSpellInfo = _spellManager.GetSpellInfo(trainerSpell.SpellId);

        if (trainerSpellInfo.IsPrimaryProfessionFirstRank && player.FreePrimaryProfessionPoints == 0)
            return false;

        foreach (var effect in trainerSpellInfo.Effects)
        {
            if (!effect.IsEffectName(SpellEffectName.LearnSpell))
                continue;

            var learnedSpellInfo = _spellManager.GetSpellInfo(effect.TriggerSpell);

            if (learnedSpellInfo is { IsPrimaryProfessionFirstRank: true } && player.FreePrimaryProfessionPoints == 0)
                return false;
        }

        return true;
    }

    private string GetGreeting(Locale locale)
    {
        return _greeting[(int)locale].IsEmpty() ? _greeting[(int)Locale.enUS] : _greeting[(int)locale];
    }

    private TrainerSpell GetSpell(uint spellId)
    {
        return _spells.Find(trainerSpell => trainerSpell.SpellId == spellId);
    }

    private TrainerSpellState GetSpellState(Player player, TrainerSpell trainerSpell)
    {
        if (player.HasSpell(trainerSpell.SpellId))
            return TrainerSpellState.Known;

        // check race/class requirement
        if (!player.IsSpellFitByClassAndRace(trainerSpell.SpellId))
            return TrainerSpellState.Unavailable;

        // check skill requirement
        if (trainerSpell.ReqSkillLine != 0 && player.GetBaseSkillValue((SkillType)trainerSpell.ReqSkillLine) < trainerSpell.ReqSkillRank)
            return TrainerSpellState.Unavailable;

        if (trainerSpell.ReqAbility.Any(reqAbility => reqAbility != 0 && !player.HasSpell(reqAbility)))
            return TrainerSpellState.Unavailable;

        // check level requirement
        if (player.Level < trainerSpell.ReqLevel)
            return TrainerSpellState.Unavailable;

        // check ranks
        var hasLearnSpellEffect = false;
        var knowsAllLearnedSpells = true;

        foreach (var spellEffectInfo in _spellManager.GetSpellInfo(trainerSpell.SpellId).Effects.Where(spellEffectInfo => spellEffectInfo.IsEffectName(SpellEffectName.LearnSpell)))
        {
            hasLearnSpellEffect = true;

            if (!player.HasSpell(spellEffectInfo.TriggerSpell))
                knowsAllLearnedSpells = false;
        }

        if (hasLearnSpellEffect && knowsAllLearnedSpells)
            return TrainerSpellState.Known;

        return TrainerSpellState.Available;
    }

    private void SendTeachFailure(Creature npc, Player player, uint spellId, TrainerFailReason reason)
    {
        TrainerBuyFailed trainerBuyFailed = new()
        {
            TrainerGUID = npc.GUID,
            SpellID = spellId,
            TrainerFailedReason = reason
        };

        player.SendPacket(trainerBuyFailed);
    }
}