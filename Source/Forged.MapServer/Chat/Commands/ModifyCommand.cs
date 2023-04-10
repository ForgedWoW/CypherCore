// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Cache;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Objects.Update;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Networking.Packets.Spell;
using Forged.MapServer.Phasing;
using Forged.MapServer.Reputation;
using Framework.Constants;
using Framework.IO;
using Serilog;

namespace Forged.MapServer.Chat.Commands;

[CommandGroup("modify")]
internal class ModifyCommand
{
    private static bool CheckModifyResources(CommandHandler handler, Player target, ref int res, ref int resmax, byte multiplier = 1)
    {
        res *= multiplier;
        resmax *= multiplier;

        resmax = resmax switch
        {
            0 => res,
            _ => resmax
        };

        if (res < 1 || resmax < 1 || resmax < res)
        {
            handler.SendSysMessage(CypherStrings.BadValue);

            return false;
        }

        if (!target)
        {
            handler.SendSysMessage(CypherStrings.NoCharSelected);

            return false;
        }

        if (handler.HasLowerSecurity(target, ObjectGuid.Empty))
            return false;

        return true;
    }

    private static bool CheckModifySpeed(StringArguments args, CommandHandler handler, Unit target, out float speed, float minimumBound, float maximumBound, bool checkInFlight = true)
    {
        speed = 0f;

        if (args.Empty())
            return false;

        speed = args.NextSingle();

        if (speed > maximumBound || speed < minimumBound)
        {
            handler.SendSysMessage(CypherStrings.BadValue);

            return false;
        }

        if (!target)
        {
            handler.SendSysMessage(CypherStrings.NoCharSelected);

            return false;
        }

        var player = target.AsPlayer;

        if (player)
        {
            // check online security
            if (handler.HasLowerSecurity(player, ObjectGuid.Empty))
                return false;

            if (player.IsInFlight && checkInFlight)
            {
                handler.SendSysMessage(CypherStrings.CharInFlight, handler.GetNameLink(player));

                return false;
            }
        }

        return true;
    }

    [CommandNonGroup("demorph", RBACPermissions.CommandDemorph)]
    private static bool HandleDeMorphCommand(CommandHandler handler)
    {
        var target = handler.SelectedUnit;

        if (!target)
            target = handler.Session.Player;

        // check online security
        else if (target.IsTypeId(TypeId.Player) && handler.HasLowerSecurity(target.AsPlayer, ObjectGuid.Empty))
            return false;

        target.DeMorph();

        return true;
    }

    [Command("currency", RBACPermissions.CommandModifyCurrency)]
    private static bool HandleModifyCurrencyCommand(CommandHandler handler, StringArguments args)
    {
        if (args.Empty())
            return false;

        var target = handler.SelectedPlayerOrSelf;

        if (!target)
        {
            handler.SendSysMessage(CypherStrings.PlayerNotFound);

            return false;
        }

        var currencyId = args.NextUInt32();

        if (!handler.CliDB.CurrencyTypesStorage.ContainsKey(currencyId))
            return false;

        var amount = args.NextUInt32();

        if (amount == 0)
            return false;

        target.ModifyCurrency(currencyId, (int)amount);

        return true;
    }

    [Command("drunk", RBACPermissions.CommandModifyDrunk)]
    private static bool HandleModifyDrunkCommand(CommandHandler handler, StringArguments args)
    {
        if (args.Empty())
            return false;

        var drunklevel = args.NextByte();

        drunklevel = drunklevel switch
        {
            > 100 => 100,
            _     => drunklevel
        };

        var target = handler.SelectedPlayerOrSelf;

        if (target)
            target.SetDrunkValue(drunklevel);

        return true;
    }

    [Command("energy", RBACPermissions.CommandModifyEnergy)]
    private static bool HandleModifyEnergyCommand(CommandHandler handler, int energy)
    {
        var target = handler.SelectedPlayerOrSelf;
        byte energyMultiplier = 10;
        var maxEnergy = energy;

        if (CheckModifyResources(handler, target, ref energy, ref maxEnergy, energyMultiplier))
        {
            NotifyModification(handler, target, CypherStrings.YouChangeEnergy, CypherStrings.YoursEnergyChanged, energy / energyMultiplier, maxEnergy / energyMultiplier);
            target.SetMaxPower(PowerType.Energy, maxEnergy);
            target.SetPower(PowerType.Energy, energy);

            return true;
        }

        return false;
    }

    [Command("faction", RBACPermissions.CommandModifyFaction)]
    private static bool HandleModifyFactionCommand(CommandHandler handler, StringArguments args)
    {
        var pfactionid = handler.ExtractKeyFromLink(args, "Hfaction");

        var target = handler.SelectedCreature;

        if (!target)
        {
            handler.SendSysMessage(CypherStrings.SelectCreature);

            return false;
        }

        if (!uint.TryParse(pfactionid, out var targetFaction))
        {
            var factionid = target.Faction;
            uint flag = target.UnitData.Flags;
            var npcflag = (ulong)target.UnitData.NpcFlags[0] << 32 | target.UnitData.NpcFlags[1];
            uint dyflag = target.ObjectData.DynamicFlags;
            handler.SendSysMessage(CypherStrings.CurrentFaction, target.GUID.ToString(), factionid, flag, npcflag, dyflag);

            return true;
        }

        if (!uint.TryParse(args.NextString(), out var unitDataFlags))
            unitDataFlags = target.UnitData.Flags;

        if (!ulong.TryParse(args.NextString(), out var unitDataNpcFlag))
            unitDataNpcFlag = (ulong)target.UnitData.NpcFlags[0] << 32 | target.UnitData.NpcFlags[1];

        if (!uint.TryParse(args.NextString(), out var objectDataDynamicFlags))
            objectDataDynamicFlags = target.ObjectData.DynamicFlags;

        if (!handler.CliDB.FactionTemplateStorage.ContainsKey(targetFaction))
        {
            handler.SendSysMessage(CypherStrings.WrongFaction, targetFaction);

            return false;
        }

        handler.SendSysMessage(CypherStrings.YouChangeFaction, target.GUID.ToString(), targetFaction, unitDataFlags, unitDataNpcFlag, objectDataDynamicFlags);

        target.Faction = targetFaction;
        target.ReplaceAllUnitFlags((UnitFlags)unitDataFlags);
        target.ReplaceAllNpcFlags((NPCFlags)(unitDataNpcFlag & 0xFFFFFFFF));
        target.ReplaceAllNpcFlags2((NPCFlags2)(unitDataNpcFlag >> 32));
        target.ReplaceAllDynamicFlags((UnitDynFlags)objectDataDynamicFlags);

        return true;
    }

    [Command("gender", RBACPermissions.CommandModifyGender)]
    private static bool HandleModifyGenderCommand(CommandHandler handler, StringArguments args)
    {
        if (args.Empty())
            return false;

        var target = handler.SelectedPlayerOrSelf;

        if (!target)
        {
            handler.SendSysMessage(CypherStrings.PlayerNotFound);

            return false;
        }

        var info = handler.ObjectManager.GetPlayerInfo(target.Race, target.Class);

        if (info == null)
            return false;

        var genderStr = args.NextString();
        Gender gender;

        if (genderStr == "male") // MALE
        {
            if (target.Gender == Gender.Male)
                return true;

            gender = Gender.Male;
        }
        else if (genderStr == "female") // FEMALE
        {
            if (target.Gender == Gender.Female)
                return true;

            gender = Gender.Female;
        }
        else
        {
            handler.SendSysMessage(CypherStrings.MustMaleOrFemale);

            return false;
        }

        // Set gender
        target.
            // Set gender
            Gender = gender;

        target.NativeGender = gender;

        // Change display ID
        target.InitDisplayIds();

        target.RestoreDisplayId();
        handler.ClassFactory.Resolve<CharacterCache>().UpdateCharacterGender(target.GUID, (byte)gender);

        // Generate random customizations
        List<ChrCustomizationChoice> customizations = new();

        var options = handler.ClassFactory.Resolve<DB2Manager>().GetCustomiztionOptions(target.Race, gender);
        var worldSession = target.Session;

        foreach (var option in options)
        {
            var optionReq = handler.CliDB.ChrCustomizationReqStorage.LookupByKey(option.ChrCustomizationReqID);

            if (optionReq != null && !worldSession.Player.MeetsChrCustomizationReq(optionReq, target.Class, false, customizations))
                continue;

            // Loop over the options until the first one fits
            var choicesForOption = handler.ClassFactory.Resolve<DB2Manager>().GetCustomiztionChoices(option.Id);

            foreach (var choiceForOption in choicesForOption)
            {
                var choiceReq = handler.CliDB.ChrCustomizationReqStorage.LookupByKey(choiceForOption.ChrCustomizationReqID);

                if (choiceReq != null && !worldSession.Player.MeetsChrCustomizationReq(choiceReq, target.Class, false, customizations))
                    continue;

                var choiceEntry = choicesForOption[0];

                ChrCustomizationChoice choice = new()
                {
                    ChrCustomizationOptionID = option.Id,
                    ChrCustomizationChoiceID = choiceEntry.Id
                };

                customizations.Add(choice);

                break;
            }
        }

        target.SetCustomizations(customizations);

        handler.SendSysMessage(CypherStrings.YouChangeGender, handler.GetNameLink(target), gender);

        if (handler.NeedReportToTarget(target))
            target.SendSysMessage(CypherStrings.YourGenderChanged, gender, handler.NameLink);

        return true;
    }

    [Command("honor", RBACPermissions.CommandModifyHonor)]
    private static bool HandleModifyHonorCommand(CommandHandler handler, StringArguments args)
    {
        if (args.Empty())
            return false;

        var target = handler.SelectedPlayerOrSelf;

        if (!target)
        {
            handler.SendSysMessage(CypherStrings.PlayerNotFound);

            return false;
        }

        // check online security
        if (handler.HasLowerSecurity(target, ObjectGuid.Empty))
            return false;

        var amount = args.NextInt32();

        //target.ModifyCurrency(CurrencyTypes.HonorPoints, amount, true, true);
        handler.SendSysMessage("NOT IMPLEMENTED: {0} honor NOT added.", amount);

        //handler.SendSysMessage(CypherStrings.CommandModifyHonor, handler.GetNameLink(target), target.GetCurrency((uint)CurrencyTypes.HonorPoints));
        return true;
    }

    [Command("hp", RBACPermissions.CommandModifyHp)]
    private static bool HandleModifyHpCommand(CommandHandler handler, int hp)
    {
        var target = handler.SelectedPlayerOrSelf;
        var maxHp = hp;

        if (CheckModifyResources(handler, target, ref hp, ref maxHp))
        {
            NotifyModification(handler, target, CypherStrings.YouChangeHp, CypherStrings.YoursHpChanged, hp, maxHp);
            target.SetMaxHealth((uint)maxHp);
            target.SetHealth((uint)hp);

            return true;
        }

        return false;
    }

    [Command("mana", RBACPermissions.CommandModifyMana)]
    private static bool HandleModifyManaCommand(CommandHandler handler, int mana)
    {
        var target = handler.SelectedPlayerOrSelf;
        var maxMana = mana;

        if (CheckModifyResources(handler, target, ref mana, ref maxMana))
        {
            NotifyModification(handler, target, CypherStrings.YouChangeMana, CypherStrings.YoursManaChanged, mana, maxMana);
            target.SetMaxPower(PowerType.Mana, maxMana);
            target.SetPower(PowerType.Mana, mana);

            return true;
        }

        return false;
    }
    [Command("money", RBACPermissions.CommandModifyMoney)]
    private static bool HandleModifyMoneyCommand(CommandHandler handler, StringArguments args)
    {
        var target = handler.SelectedPlayerOrSelf;

        if (!target)
        {
            handler.SendSysMessage(CypherStrings.NoCharSelected);

            return false;
        }

        // check online security
        if (handler.HasLowerSecurity(target, ObjectGuid.Empty))
            return false;

        var moneyToAdd = args.NextInt64();
        var targetMoney = target.Money;

        if (moneyToAdd < 0)
        {
            var newmoney = (long)targetMoney + moneyToAdd;

            Log.Logger.Debug(handler.ObjectManager.GetCypherString(CypherStrings.CurrentMoney), targetMoney, moneyToAdd, newmoney);

            if (newmoney <= 0)
            {
                handler.SendSysMessage(CypherStrings.YouTakeAllMoney, handler.GetNameLink(target));

                if (handler.NeedReportToTarget(target))
                    target.SendSysMessage(CypherStrings.YoursAllMoneyGone, handler.NameLink);

                target.Money = 0;
            }
            else
            {
                var moneyToAddMsg = (ulong)(moneyToAdd * -1);

                newmoney = newmoney switch
                {
                    > (long)PlayerConst.MaxMoneyAmount => (long)PlayerConst.MaxMoneyAmount,
                    _                                  => newmoney
                };

                handler.SendSysMessage(CypherStrings.YouTakeMoney, moneyToAddMsg, handler.GetNameLink(target));

                if (handler.NeedReportToTarget(target))
                    target.SendSysMessage(CypherStrings.YoursMoneyTaken, handler.NameLink, moneyToAddMsg);

                target.Money = (ulong)newmoney;
            }
        }
        else
        {
            handler.SendSysMessage(CypherStrings.YouGiveMoney, moneyToAdd, handler.GetNameLink(target));

            if (handler.NeedReportToTarget(target))
                target.SendSysMessage(CypherStrings.YoursMoneyGiven, handler.NameLink, moneyToAdd);

            moneyToAdd = (ulong)moneyToAdd switch
            {
                >= PlayerConst.MaxMoneyAmount => Convert.ToInt64(PlayerConst.MaxMoneyAmount),
                _                             => moneyToAdd
            };

            moneyToAdd = (long)Math.Min((ulong)moneyToAdd, (PlayerConst.MaxMoneyAmount - targetMoney));

            target.ModifyMoney(moneyToAdd);
        }

        Log.Logger.Debug(handler.ObjectManager.GetCypherString(CypherStrings.NewMoney), targetMoney, moneyToAdd, target.Money);

        return true;
    }

    [CommandNonGroup("morph", RBACPermissions.CommandMorph)]
    private static bool HandleModifyMorphCommand(CommandHandler handler, StringArguments args)
    {
        if (args.Empty())
            return false;

        var displayID = args.NextUInt32();

        var target = handler.SelectedUnit;

        if (!target)
            target = handler.Session.Player;

        // check online security
        else if (target.IsTypeId(TypeId.Player) && handler.HasLowerSecurity(target.AsPlayer, ObjectGuid.Empty))
            return false;

        target.SetDisplayId(displayID);

        return true;
    }

    [Command("mount", RBACPermissions.CommandModifyMount)]
    private static bool HandleModifyMountCommand(CommandHandler handler, StringArguments args)
    {
        if (args.Empty())
            return false;

        if (!uint.TryParse(args.NextString(), out var mount))
            return false;

        if (!handler.CliDB.CreatureDisplayInfoStorage.HasRecord(mount))
        {
            handler.SendSysMessage(CypherStrings.NoMount);

            return false;
        }

        var target = handler.SelectedPlayerOrSelf;

        if (!target)
        {
            handler.SendSysMessage(CypherStrings.NoCharSelected);

            return false;
        }

        // check online security
        if (handler.HasLowerSecurity(target, ObjectGuid.Empty))
            return false;

        if (!CheckModifySpeed(args, handler, target, out var speed, 0.1f, 50.0f))
            return false;

        NotifyModification(handler, target, CypherStrings.YouGiveMount, CypherStrings.MountGived);
        target.Mount(mount);
        target.SetSpeedRate(UnitMoveType.Run, speed);
        target.SetSpeedRate(UnitMoveType.Flight, speed);

        return true;
    }

    [Command("phase", RBACPermissions.CommandModifyPhase)]
    private static bool HandleModifyPhaseCommand(CommandHandler handler, StringArguments args)
    {
        if (args.Empty())
            return false;

        var phaseId = args.NextUInt32();
        var visibleMapId = args.NextUInt32();

        if (phaseId != 0 && !handler.CliDB.PhaseStorage.ContainsKey(phaseId))
        {
            handler.SendSysMessage(CypherStrings.PhaseNotfound);

            return false;
        }

        var target = handler.SelectedUnit;

        if (visibleMapId != 0)
        {
            var visibleMap = handler.CliDB.MapStorage.LookupByKey(visibleMapId);

            if (visibleMap == null || visibleMap.ParentMapID != target.Location.MapId)
            {
                handler.SendSysMessage(CypherStrings.PhaseNotfound);

                return false;
            }

            if (!target.Location.PhaseShift.HasVisibleMapId(visibleMapId))
                PhasingHandler.AddVisibleMapId(target, visibleMapId);
            else
                PhasingHandler.RemoveVisibleMapId(target, visibleMapId);
        }

        if (phaseId != 0)
        {
            if (!target.Location.PhaseShift.HasPhase(phaseId))
                PhasingHandler.AddPhase(target, phaseId, true);
            else
                PhasingHandler.RemovePhase(target, phaseId, true);
        }

        return true;
    }

    [Command("power", RBACPermissions.CommandModifyPower)]
    private static bool HandleModifyPowerCommand(CommandHandler handler, StringArguments args)
    {
        if (args.Empty())
            return false;

        var target = handler.SelectedPlayerOrSelf;

        if (!target)
        {
            handler.SendSysMessage(CypherStrings.NoCharSelected);

            return false;
        }

        if (handler.HasLowerSecurity(target, ObjectGuid.Empty))
            return false;

        var powerTypeToken = args.NextString();

        if (powerTypeToken.IsEmpty())
            return false;

        var powerType = handler.ClassFactory.Resolve<DB2Manager>().GetPowerTypeByName(powerTypeToken);

        if (powerType == null)
        {
            handler.SendSysMessage(CypherStrings.InvalidPowerName);

            return false;
        }

        if (target.GetPowerIndex(powerType.PowerTypeEnum) == (int)PowerType.Max)
        {
            handler.SendSysMessage(CypherStrings.InvalidPowerName);

            return false;
        }

        var powerAmount = args.NextInt32();

        if (powerAmount < 1)
        {
            handler.SendSysMessage(CypherStrings.BadValue);

            return false;
        }

        NotifyModification(handler, target, CypherStrings.YouChangePower, CypherStrings.YourPowerChanged, powerType.NameGlobalStringTag, powerAmount, powerAmount);
        powerAmount *= powerType.DisplayModifier;
        target.SetMaxPower(powerType.PowerTypeEnum, powerAmount);
        target.SetPower(powerType.PowerTypeEnum, powerAmount);

        return true;
    }

    [Command("rage", RBACPermissions.CommandModifyRage)]
    private static bool HandleModifyRageCommand(CommandHandler handler, int rage)
    {
        var target = handler.SelectedPlayerOrSelf;
        byte rageMultiplier = 10;
        var maxRage = rage;

        if (CheckModifyResources(handler, target, ref rage, ref maxRage, rageMultiplier))
        {
            NotifyModification(handler, target, CypherStrings.YouChangeRage, CypherStrings.YoursRageChanged, rage / rageMultiplier, maxRage / rageMultiplier);
            target.SetMaxPower(PowerType.Rage, maxRage);
            target.SetPower(PowerType.Rage, rage);

            return true;
        }

        return false;
    }

    [Command("reputation", RBACPermissions.CommandModifyReputation)]
    private static bool HandleModifyRepCommand(CommandHandler handler, StringArguments args)
    {
        if (args.Empty())
            return false;

        var target = handler.SelectedPlayerOrSelf;

        if (!target)
        {
            handler.SendSysMessage(CypherStrings.PlayerNotFound);

            return false;
        }

        // check online security
        if (handler.HasLowerSecurity(target, ObjectGuid.Empty))
            return false;

        var factionTxt = handler.ExtractKeyFromLink(args, "Hfaction");

        if (string.IsNullOrEmpty(factionTxt))
            return false;

        if (!uint.TryParse(factionTxt, out var factionId))
            return false;

        var rankTxt = args.NextString();

        if (factionId == 0 || !int.TryParse(rankTxt, out var amount))
            return false;

        var factionEntry = handler.CliDB.FactionStorage.LookupByKey(factionId);

        if (factionEntry == null)
        {
            handler.SendSysMessage(CypherStrings.CommandFactionUnknown, factionId);

            return false;
        }

        if (factionEntry.ReputationIndex < 0)
        {
            handler.SendSysMessage(CypherStrings.CommandFactionNorepError, factionEntry.Name[handler.SessionDbcLocale], factionId);

            return false;
        }

        // try to find rank by name
        if ((amount == 0) && !(amount < 0) && !rankTxt.IsNumber())
        {
            var rankStr = rankTxt.ToLower();

            var i = 0;
            var r = 0;

            for (; i != ReputationMgr.ReputationRankThresholds.Length - 1; ++i, ++r)
            {
                var rank = handler.GetCypherString(ReputationMgr.ReputationRankStrIndex[r]);

                if (string.IsNullOrEmpty(rank))
                    continue;

                if (rank.Equals(rankStr, StringComparison.OrdinalIgnoreCase))
                    break;

                if (i == ReputationMgr.ReputationRankThresholds.Length - 1)
                {
                    handler.SendSysMessage(CypherStrings.CommandInvalidParam, rankTxt);

                    return false;
                }

                amount = ReputationMgr.ReputationRankThresholds[i];

                var deltaTxt = args.NextString();

                if (!string.IsNullOrEmpty(deltaTxt))
                {
                    var toNextRank = 0;
                    var nextThresholdIndex = i;
                    ++nextThresholdIndex;

                    if (nextThresholdIndex != ReputationMgr.ReputationRankThresholds.Length - 1)
                        toNextRank = nextThresholdIndex - i;

                    if (!int.TryParse(deltaTxt, out var delta) || delta < 0 || delta >= toNextRank)
                    {
                        handler.SendSysMessage(CypherStrings.CommandFactionDelta, Math.Max(0, toNextRank - 1));

                        return false;
                    }

                    amount += delta;
                }
            }
        }

        target.ReputationMgr.SetOneFactionReputation(factionEntry, amount, false);
        target.ReputationMgr.SendState(target.ReputationMgr.GetState(factionEntry));
        handler.SendSysMessage(CypherStrings.CommandModifyRep, factionEntry.Name[handler.SessionDbcLocale], factionId, handler.GetNameLink(target), target.ReputationMgr.GetReputation(factionEntry));

        return true;
    }

    [Command("runicpower", RBACPermissions.CommandModifyRunicpower)]
    private static bool HandleModifyRunicPowerCommand(CommandHandler handler, int rune)
    {
        var target = handler.SelectedPlayerOrSelf;
        byte runeMultiplier = 10;
        var maxRune = rune;

        if (CheckModifyResources(handler, target, ref rune, ref maxRune, runeMultiplier))
        {
            NotifyModification(handler, target, CypherStrings.YouChangeRunicPower, CypherStrings.YoursRunicPowerChanged, rune / runeMultiplier, maxRune / runeMultiplier);
            target.SetMaxPower(PowerType.RunicPower, maxRune);
            target.SetPower(PowerType.RunicPower, rune);

            return true;
        }

        return false;
    }
    [Command("scale", RBACPermissions.CommandModifyScale)]
    private static bool HandleModifyScaleCommand(CommandHandler handler, StringArguments args)
    {
        var target = handler.SelectedUnit;

        if (CheckModifySpeed(args, handler, target, out var scale, 0.1f, 10.0f, false))
        {
            NotifyModification(handler, target, CypherStrings.YouChangeSize, CypherStrings.YoursSizeChanged, scale);
            var creatureTarget = target.AsCreature;

            if (creatureTarget)
                creatureTarget.SetDisplayId(creatureTarget.DisplayId, scale);
            else
                target.ObjectScale = scale;

            return true;
        }

        return false;
    }

    [Command("spell", RBACPermissions.CommandModifySpell)]
    private static bool HandleModifySpellCommand(CommandHandler handler, StringArguments args)
    {
        if (args.Empty())
            return false;

        var spellflatid = args.NextByte();

        if (spellflatid == 0)
            return false;

        var op = args.NextByte();

        if (op == 0)
            return false;

        var val = args.NextUInt16();

        if (val == 0)
            return false;

        if (!ushort.TryParse(args.NextString(), out var mark))
            mark = 65535;

        var target = handler.SelectedPlayerOrSelf;

        if (!target)
        {
            handler.SendSysMessage(CypherStrings.NoCharSelected);

            return false;
        }

        // check online security
        if (handler.HasLowerSecurity(target, ObjectGuid.Empty))
            return false;

        handler.SendSysMessage(CypherStrings.YouChangeSpellflatid, spellflatid, val, mark, handler.GetNameLink(target));

        if (handler.NeedReportToTarget(target))
            target.SendSysMessage(CypherStrings.YoursSpellflatidChanged, handler.NameLink, spellflatid, val, mark);

        SetSpellModifier packet = new(ServerOpcodes.SetFlatSpellModifier);

        SpellModifierInfo spellMod = new()
        {
            ModIndex = op
        };

        SpellModifierData modData;
        modData.ClassIndex = spellflatid;
        modData.ModifierValue = val;
        spellMod.ModifierData.Add(modData);
        packet.Modifiers.Add(spellMod);
        target.SendPacket(packet);

        return true;
    }

    [Command("standstate", RBACPermissions.CommandModifyStandstate)]
    private static bool HandleModifyStandStateCommand(CommandHandler handler, StringArguments args)
    {
        if (args.Empty())
            return false;

        var animID = args.NextUInt32();
        handler.Session.Player.EmoteState = (Emote)animID;

        return true;
    }

    [Command("talentpoints", RBACPermissions.CommandModifyTalentpoints)]
    private static bool HandleModifyTalentCommand(CommandHandler handler)
    {
        return false;
    }
    [Command("xp", RBACPermissions.CommandModifyXp)]
    private static bool HandleModifyXPCommand(CommandHandler handler, StringArguments args)
    {
        if (args.Empty())
            return false;

        var xp = args.NextInt32();

        if (xp < 1)
        {
            handler.SendSysMessage(CypherStrings.BadValue);

            return false;
        }

        var target = handler.SelectedPlayerOrSelf;

        if (!target)
        {
            handler.SendSysMessage(CypherStrings.NoCharSelected);

            return false;
        }

        if (handler.HasLowerSecurity(target, ObjectGuid.Empty))
            return false;

        // we can run the command
        target.GiveXP((uint)xp, null);

        return true;
    }
    private static void NotifyModification(CommandHandler handler, Unit target, CypherStrings resourceMessage, CypherStrings resourceReportMessage, params object[] args)
    {
        var player = target.AsPlayer;

        if (player)
        {
            handler.SendSysMessage(resourceMessage,
                                   new object[]
                                   {
                                       handler.GetNameLink(player)
                                   }.Combine(args));

            if (handler.NeedReportToTarget(player))
                player.SendSysMessage(resourceReportMessage,
                                      new object[]
                                      {
                                          handler.NameLink
                                      }.Combine(args));
        }
    }
    [CommandGroup("speed")]
    private class ModifySpeed
    {
        [Command("all", RBACPermissions.CommandModifySpeedAll)]
        private static bool HandleModifyASpeedCommand(CommandHandler handler, StringArguments args)
        {
            var target = handler.SelectedPlayerOrSelf;

            if (CheckModifySpeed(args, handler, target, out var allSpeed, 0.1f, 50.0f))
            {
                NotifyModification(handler, target, CypherStrings.YouChangeAspeed, CypherStrings.YoursAspeedChanged, allSpeed);
                target.SetSpeedRate(UnitMoveType.Walk, allSpeed);
                target.SetSpeedRate(UnitMoveType.Run, allSpeed);
                target.SetSpeedRate(UnitMoveType.Swim, allSpeed);
                target.SetSpeedRate(UnitMoveType.Flight, allSpeed);

                return true;
            }

            return false;
        }

        [Command("backwalk", RBACPermissions.CommandModifySpeedBackwalk)]
        private static bool HandleModifyBWalkCommand(CommandHandler handler, StringArguments args)
        {
            var target = handler.SelectedPlayerOrSelf;

            if (CheckModifySpeed(args, handler, target, out var backSpeed, 0.1f, 50.0f))
            {
                NotifyModification(handler, target, CypherStrings.YouChangeBackSpeed, CypherStrings.YoursBackSpeedChanged, backSpeed);
                target.SetSpeedRate(UnitMoveType.RunBack, backSpeed);

                return true;
            }

            return false;
        }

        [Command("fly", RBACPermissions.CommandModifySpeedFly)]
        private static bool HandleModifyFlyCommand(CommandHandler handler, StringArguments args)
        {
            var target = handler.SelectedPlayerOrSelf;

            if (CheckModifySpeed(args, handler, target, out var flySpeed, 0.1f, 50.0f, false))
            {
                NotifyModification(handler, target, CypherStrings.YouChangeFlySpeed, CypherStrings.YoursFlySpeedChanged, flySpeed);
                target.SetSpeedRate(UnitMoveType.Flight, flySpeed);

                return true;
            }

            return false;
        }

        [Command("", RBACPermissions.CommandModifySpeed)]
        private static bool HandleModifySpeedCommand(CommandHandler handler, StringArguments args)
        {
            return HandleModifyASpeedCommand(handler, args);
        }
        [Command("swim", RBACPermissions.CommandModifySpeedSwim)]
        private static bool HandleModifySwimCommand(CommandHandler handler, StringArguments args)
        {
            var target = handler.SelectedPlayerOrSelf;

            if (CheckModifySpeed(args, handler, target, out var swimSpeed, 0.1f, 50.0f))
            {
                NotifyModification(handler, target, CypherStrings.YouChangeSwimSpeed, CypherStrings.YoursSwimSpeedChanged, swimSpeed);
                target.SetSpeedRate(UnitMoveType.Swim, swimSpeed);

                return true;
            }

            return false;
        }
        [Command("walk", RBACPermissions.CommandModifySpeedWalk)]
        private static bool HandleModifyWalkSpeedCommand(CommandHandler handler, StringArguments args)
        {
            var target = handler.SelectedPlayerOrSelf;

            if (CheckModifySpeed(args, handler, target, out var speed, 0.1f, 50.0f))
            {
                NotifyModification(handler, target, CypherStrings.YouChangeSpeed, CypherStrings.YoursSpeedChanged, speed);
                target.SetSpeedRate(UnitMoveType.Run, speed);

                return true;
            }

            return false;
        }
    }
}