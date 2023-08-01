// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Chat;
using Forged.MapServer.Conditions;
using Forged.MapServer.DataStorage.ClientReader;
using Forged.MapServer.DataStorage.Structs.S;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Spells;
using Framework.Constants;
using Framework.Database;
using Serilog;

namespace Forged.MapServer.Globals.Caching;

public class TrainerCache : IObjectCache
{
    private readonly BattlePetData _battlePetData;
    private readonly ConditionManager _conditionManager;
    private readonly DB6Storage<SkillLineRecord> _skillLineRecords;
    private readonly SpellManager _spellManager;
    private readonly Dictionary<uint, Trainer> _trainers = new();
    private readonly WorldDatabase _worldDatabase;

    public TrainerCache(WorldDatabase worldDatabase, SpellManager spellManager, BattlePetData battlePetData, ConditionManager conditionManager,
                        DB6Storage<SkillLineRecord> skillLineRecords)
    {
        _worldDatabase = worldDatabase;
        _spellManager = spellManager;
        _battlePetData = battlePetData;
        _conditionManager = conditionManager;
        _skillLineRecords = skillLineRecords;
    }

    public Trainer GetTrainer(uint trainerId)
    {
        return _trainers.LookupByKey(trainerId);
    }

    public void Load()
    {
        var oldMSTime = Time.MSTime;

        // For reload case
        _trainers.Clear();

        MultiMap<uint, TrainerSpell> spellsByTrainer = new();
        var trainerSpellsResult = _worldDatabase.Query("SELECT TrainerId, SpellId, MoneyCost, ReqSkillLine, ReqSkillRank, ReqAbility1, ReqAbility2, ReqAbility3, ReqLevel FROM trainer_spell");

        if (!trainerSpellsResult.IsEmpty())
            do
            {
                TrainerSpell spell = new();
                var trainerId = trainerSpellsResult.Read<uint>(0);
                spell.SpellId = trainerSpellsResult.Read<uint>(1);
                spell.MoneyCost = trainerSpellsResult.Read<uint>(2);
                spell.ReqSkillLine = trainerSpellsResult.Read<uint>(3);
                spell.ReqSkillRank = trainerSpellsResult.Read<uint>(4);
                spell.ReqAbility[0] = trainerSpellsResult.Read<uint>(5);
                spell.ReqAbility[1] = trainerSpellsResult.Read<uint>(6);
                spell.ReqAbility[2] = trainerSpellsResult.Read<uint>(7);
                spell.ReqLevel = trainerSpellsResult.Read<byte>(8);

                var spellInfo = _spellManager.GetSpellInfo(spell.SpellId);

                if (spellInfo == null)
                {
                    Log.Logger.Error($"Table `trainer_spell` references non-existing spell (SpellId: {spell.SpellId}) for TrainerId {trainerId}, ignoring");

                    continue;
                }

                if (spell.ReqSkillLine != 0 && !_skillLineRecords.ContainsKey(spell.ReqSkillLine))
                {
                    Log.Logger.Error($"Table `trainer_spell` references non-existing skill (ReqSkillLine: {spell.ReqSkillLine}) for TrainerId {trainerId} and SpellId {spell.SpellId}, ignoring");

                    continue;
                }

                var allReqValid = true;

                for (var i = 0; i < spell.ReqAbility.Count; ++i)
                {
                    var requiredSpell = spell.ReqAbility[i];

                    if (requiredSpell != 0 && !_spellManager.HasSpellInfo(requiredSpell))
                    {
                        Log.Logger.Error($"Table `trainer_spell` references non-existing spell (ReqAbility {i + 1}: {requiredSpell}) for TrainerId {trainerId} and SpellId {spell.SpellId}, ignoring");
                        allReqValid = false;
                    }
                }

                if (!allReqValid)
                    continue;

                spellsByTrainer.Add(trainerId, spell);
            } while (trainerSpellsResult.NextRow());

        var trainersResult = _worldDatabase.Query("SELECT Id, Type, Greeting FROM trainer");

        if (!trainersResult.IsEmpty())
            do
            {
                var trainerId = trainersResult.Read<uint>(0);
                var trainerType = (TrainerType)trainersResult.Read<byte>(1);
                var greeting = trainersResult.Read<string>(2);
                List<TrainerSpell> spells = new();

                if (spellsByTrainer.TryGetValue(trainerId, out var spellList))
                {
                    spells = spellList;
                    spellsByTrainer.Remove(trainerId);
                }

                _trainers.Add(trainerId, new Trainer(trainerId, trainerType, greeting, spells, _conditionManager, _battlePetData, _spellManager));
            } while (trainersResult.NextRow());

        foreach (var unusedSpells in spellsByTrainer.KeyValueList)
            Log.Logger.Error($"Table `trainer_spell` references non-existing trainer (TrainerId: {unusedSpells.Key}) for SpellId {unusedSpells.Value.SpellId}, ignoring");

        var trainerLocalesResult = _worldDatabase.Query("SELECT Id, locale, Greeting_lang FROM trainer_locale");

        if (!trainerLocalesResult.IsEmpty())
            do
            {
                var trainerId = trainerLocalesResult.Read<uint>(0);
                var localeName = trainerLocalesResult.Read<string>(1);

                var locale = localeName.ToEnum<Locale>();

                if (!SharedConst.IsValidLocale(locale) || locale == Locale.enUS)
                    continue;

                if (_trainers.TryGetValue(trainerId, out var trainer))
                    trainer.AddGreetingLocale(locale, trainerLocalesResult.Read<string>(2));
                else
                    Log.Logger.Error($"Table `trainer_locale` references non-existing trainer (TrainerId: {trainerId}) for locale {localeName}, ignoring");
            } while (trainerLocalesResult.NextRow());

        Log.Logger.Information($"Loaded {_trainers.Count} Trainers in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
    }
}