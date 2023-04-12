// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.BattleGrounds;
using Forged.MapServer.Chrono;
using Forged.MapServer.DataStorage.Structs.R;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Objects.Update;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Miscellaneous;
using Forged.MapServer.Networking.Packets.BattleGround;
using Forged.MapServer.Networking.Packets.Combat;
using Forged.MapServer.OutdoorPVP;
using Framework.Constants;
using Framework.Database;
using Framework.Util;
using Serilog;

namespace Forged.MapServer.Entities.Players;

public partial class Player
{
    //BGs
    public Battleground Battleground
    {
        get
        {
            if (BattlegroundId == 0)
                return null;

            return BattlegroundManager.GetBattleground(BattlegroundId, _bgData.BgTypeId);
        }
    }

    public WorldLocation BattlegroundEntryPoint => _bgData.JoinPos;
    public uint BattlegroundId => _bgData.BgInstanceId;
    public BattlegroundTypeId BattlegroundTypeId => _bgData.BgTypeId;
    public bool CanCaptureTowerPoint => (!HasStealthAura &&      // not stealthed
                                         !HasInvisibilityAura && // not invisible
                                         IsAlive);

    public bool HasFreeBattlegroundQueueId
    {
        get
        {
            for (byte i = 0; i < SharedConst.MaxPlayerBGQueues; ++i)
                if (_battlegroundQueueIdRecs[i].BgQueueTypeId == default)
                    return true;

            return false;
        }
    }

    public uint HonorLevel => PlayerData.HonorLevel;

    public bool InBattleground => _bgData.BgInstanceId != 0;
    public bool IsMaxHonorLevel => HonorLevel == PlayerConst.MaxHonorLevel;

    public bool IsUsingPvpItemLevels { get; private set; }
    // live player

    public void ActivatePvpItemLevels(bool activate)
    {
        IsUsingPvpItemLevels = activate;
    }

    public uint AddBattlegroundQueueId(BattlegroundQueueTypeId val)
    {
        for (byte i = 0; i < SharedConst.MaxPlayerBGQueues; ++i)
            if (_battlegroundQueueIdRecs[i].BgQueueTypeId == default || _battlegroundQueueIdRecs[i].BgQueueTypeId == val)
            {
                _battlegroundQueueIdRecs[i].BgQueueTypeId = val;
                _battlegroundQueueIdRecs[i].InvitedToInstance = 0;
                _battlegroundQueueIdRecs[i].JoinTime = (uint)GameTime.CurrentTime;
                _battlegroundQueueIdRecs[i].Mercenary = HasAura(BattlegroundConst.SpellMercenaryContractHorde) || HasAura(BattlegroundConst.SpellMercenaryContractAlliance);

                return i;
            }

        return SharedConst.MaxPlayerBGQueues;
    }

    public void AddHonorXp(uint xp)
    {
        uint currentHonorXp = ActivePlayerData.Honor;
        uint nextHonorLevelXp = ActivePlayerData.HonorNextLevel;
        var newHonorXp = currentHonorXp + xp;
        var honorLevel = HonorLevel;

        if (xp < 1 || Level < PlayerConst.LevelMinHonor || IsMaxHonorLevel)
            return;

        while (newHonorXp >= nextHonorLevelXp)
        {
            newHonorXp -= nextHonorLevelXp;

            if (honorLevel < PlayerConst.MaxHonorLevel)
                SetHonorLevel((byte)(honorLevel + 1));

            honorLevel = HonorLevel;
            nextHonorLevelXp = ActivePlayerData.HonorNextLevel;
        }

        SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.Honor), IsMaxHonorLevel ? 0 : newHonorXp);
    }

    public bool CanJoinToBattleground(Battleground bg)
    {
        var perm = RBACPermissions.JoinNormalBg;

        if (bg.IsArena())
            perm = RBACPermissions.JoinArenas;
        else if (bg.IsRandom())
            perm = RBACPermissions.JoinRandomBg;

        return Session.HasPermission(perm);
    }

    public bool CanUseBattlegroundObject(GameObject gameobject)
    {
        // It is possible to call this method with a null pointer, only skipping faction check.
        if (gameobject)
        {
            var playerFaction = WorldObjectCombat.GetFactionTemplateEntry();
            var faction = CliDB.FactionTemplateStorage.LookupByKey(gameobject.Faction);

            if (playerFaction != null && faction != null && !playerFaction.IsFriendlyTo(faction))
                return false;
        }

        // BUG: sometimes when player clicks on Id in AB - client won't send gameobject_use, only gameobject_report_use packet
        // Note: Mount, stealth and invisibility will be removed when used
        return (!IsTotalImmune &&                                       // Damage immune
                !HasAura(BattlegroundConst.SpellRecentlyDroppedFlag) && // Still has recently held Id debuff
                IsAlive);                                               // Alive
    }

    public void ClearAfkReports()
    {
        _bgData.BgAfkReporter.Clear();
    }

    public void EnablePvpRules(bool dueToCombat = false)
    {
        if (HasPvpRulesEnabled())
            return;

        if (!HasSpell(195710))       // Honorable Medallion
            SpellFactory.CastSpell(this, 208682); // Learn Gladiator's Medallion

        SpellFactory.CastSpell(this, PlayerConst.SpellPvpRulesEnabled);

        if (!dueToCombat)
        {
            var aura = GetAura(PlayerConst.SpellPvpRulesEnabled);

            if (aura != null)
            {
                aura.SetMaxDuration(-1);
                aura.SetDuration(-1);
            }
        }

        UpdateItemLevelAreaBasedScaling();
    }

    public uint GetArenaPersonalRating(byte slot)
    {
        var pvpInfo = GetPvpInfoForBracket(slot);

        if (pvpInfo != null)
            return pvpInfo.Rating;

        return 0;
    }

    public uint GetArenaTeamId(byte slot)
    {
        return 0;
    }

    public uint GetArenaTeamIdInvited()
    {
        return _arenaTeamIdInvited;
    }

    public uint GetBattlegroundQueueIndex(BattlegroundQueueTypeId bgQueueTypeId)
    {
        for (byte i = 0; i < SharedConst.MaxPlayerBGQueues; ++i)
            if (_battlegroundQueueIdRecs[i].BgQueueTypeId == bgQueueTypeId)
                return i;

        return SharedConst.MaxPlayerBGQueues;
    }

    public uint GetBattlegroundQueueJoinTime(BattlegroundQueueTypeId bgQueueTypeId)
    {
        for (byte i = 0; i < SharedConst.MaxPlayerBGQueues; ++i)
            if (_battlegroundQueueIdRecs[i].BgQueueTypeId == bgQueueTypeId)
                return _battlegroundQueueIdRecs[i].JoinTime;

        return 0;
    }

    public BattlegroundQueueTypeId GetBattlegroundQueueTypeId(uint index)
    {
        return index < SharedConst.MaxPlayerBGQueues ? _battlegroundQueueIdRecs[index].BgQueueTypeId : default;
    }

    public bool GetBgAccessByLevel(BattlegroundTypeId bgTypeId)
    {
        // get a template bg instead of running one
        var bg = BattlegroundManager.GetBattlegroundTemplate(bgTypeId);

        if (!bg)
            return false;

        // limit check leel to dbc compatible level range
        var level = Level;

        if (level > Configuration.GetDefaultValue("MaxPlayerLevel", SharedConst.DefaultMaxLevel))
            level = (uint)Configuration.GetDefaultValue("MaxPlayerLevel", SharedConst.DefaultMaxLevel);

        return level >= bg.GetMinLevel() && level <= bg.GetMaxLevel();
    }

    public TeamFaction GetBgTeam()
    {
        return _bgData.BgTeam != 0 ? (TeamFaction)_bgData.BgTeam : Team;
    }

    public OutdoorPvP GetOutdoorPvP()
    {
        return OutdoorPvPManager.GetOutdoorPvPToZoneId(Location.Map, Location.Zone);
    }

    public PVPInfo GetPvpInfoForBracket(byte bracket)
    {
        var index = ActivePlayerData.PvpInfo.FindIndexIf(pvpInfo => { return pvpInfo.Bracket == bracket && !pvpInfo.Disqualified; });

        if (index >= 0)
            return ActivePlayerData.PvpInfo[index];

        return null;
    }

    public uint[] GetPvpTalentMap(byte spec)
    {
        return _specializationInfo.PvpTalents[spec];
    }

    public bool GetRandomWinner()
    {
        return _isBgRandomWinner;
    }

    public uint GetRbgPersonalRating()
    {
        return GetArenaPersonalRating(3);
    }

    public bool InBattlegroundQueue(bool ignoreArena = false)
    {
        for (byte i = 0; i < SharedConst.MaxPlayerBGQueues; ++i)
            if (_battlegroundQueueIdRecs[i].BgQueueTypeId != default && (!ignoreArena || _battlegroundQueueIdRecs[i].BgQueueTypeId.BattlemasterListId != (ushort)BattlegroundTypeId.AA))
                return true;

        return false;
    }

    public bool InBattlegroundQueueForBattlegroundQueueType(BattlegroundQueueTypeId bgQueueTypeId)
    {
        return GetBattlegroundQueueIndex(bgQueueTypeId) < SharedConst.MaxPlayerBGQueues;
    }

    public bool IsDeserter()
    {
        return HasAura(26013);
    }

    public bool IsInvitedForBattlegroundInstance(uint instanceId)
    {
        for (byte i = 0; i < SharedConst.MaxPlayerBGQueues; ++i)
            if (_battlegroundQueueIdRecs[i].InvitedToInstance == instanceId)
                return true;

        return false;
    }

    public bool IsInvitedForBattlegroundQueueType(BattlegroundQueueTypeId bgQueueTypeId)
    {
        for (byte i = 0; i < SharedConst.MaxPlayerBGQueues; ++i)
            if (_battlegroundQueueIdRecs[i].BgQueueTypeId == bgQueueTypeId)
                return _battlegroundQueueIdRecs[i].InvitedToInstance != 0;

        return false;
    }

    public bool IsMercenaryForBattlegroundQueueType(BattlegroundQueueTypeId bgQueueTypeId)
    {
        for (byte i = 0; i < SharedConst.MaxPlayerBGQueues; ++i)
            if (_battlegroundQueueIdRecs[i].BgQueueTypeId == bgQueueTypeId)
                return _battlegroundQueueIdRecs[i].Mercenary;

        return false;
    }

    //OutdoorPVP
    public bool IsOutdoorPvPActive()
    {
        return IsAlive && !HasInvisibilityAura && !HasStealthAura && IsPvP && !HasUnitMovementFlag(MovementFlag.Flying) && !IsInFlight;
    }

    public void LeaveBattleground(bool teleportToEntryPoint = true)
    {
        var bg = Battleground;

        if (bg)
        {
            bg.RemovePlayerAtLeave(GUID, teleportToEntryPoint, true);

            // call after remove to be sure that player resurrected for correct cast
            if (bg.IsBattleground() && !IsGameMaster && Configuration.GetDefaultValue("Battleground.CastDeserter", true))
                if (bg.GetStatus() == BattlegroundStatus.InProgress || bg.GetStatus() == BattlegroundStatus.WaitJoin)
                {
                    //lets check if player was teleported from BG and schedule delayed Deserter spell cast
                    if (IsBeingTeleportedFar)
                    {
                        ScheduleDelayedOperation(PlayerDelayedOperations.SpellCastDeserter);

                        return;
                    }

                    SpellFactory.CastSpell(this, 26013, true); // Deserter
                }
        }
    }

    public void RemoveBattlegroundQueueId(BattlegroundQueueTypeId val)
    {
        for (byte i = 0; i < SharedConst.MaxPlayerBGQueues; ++i)
            if (_battlegroundQueueIdRecs[i].BgQueueTypeId == val)
            {
                _battlegroundQueueIdRecs[i].BgQueueTypeId = default;
                _battlegroundQueueIdRecs[i].InvitedToInstance = 0;
                _battlegroundQueueIdRecs[i].JoinTime = 0;
                _battlegroundQueueIdRecs[i].Mercenary = false;

                return;
            }
    }

    /// <summary>
    ///     This player has been blamed to be inactive in a Battleground
    /// </summary>
    /// <param name="reporter"> </param>
    public void ReportedAfkBy(Player reporter)
    {
        ReportPvPPlayerAFKResult reportAfkResult = new()
        {
            Offender = GUID
        };

        var bg = Battleground;

        // Battleground also must be in progress!
        if (!bg || bg != reporter.Battleground || EffectiveTeam != reporter.EffectiveTeam || bg.GetStatus() != BattlegroundStatus.InProgress)
        {
            reporter.SendPacket(reportAfkResult);

            return;
        }

        // check if player has 'Idle' or 'Inactive' debuff
        if (!_bgData.BgAfkReporter.Contains(reporter.GUID) && !HasAura(43680) && !HasAura(43681) && reporter.CanReportAfkDueToLimit())
        {
            _bgData.BgAfkReporter.Add(reporter.GUID);

            // by default 3 players have to complain to apply debuff
            if (_bgData.BgAfkReporter.Count >= Configuration.GetDefaultValue("Battleground.ReportAFK", 3))
            {
                // cast 'Idle' spell
                SpellFactory.CastSpell(this, 43680, true);
                _bgData.BgAfkReporter.Clear();
                reportAfkResult.NumBlackMarksOnOffender = (byte)_bgData.BgAfkReporter.Count;
                reportAfkResult.NumPlayersIHaveReported = reporter._bgData.BgAfkReportedCount;
                reportAfkResult.Result = ReportPvPPlayerAFKResult.ResultCode.Success;
            }
        }

        reporter.SendPacket(reportAfkResult);
    }

    public void ResetHonorStats()
    {
        SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.TodayHonorableKills), (ushort)0);
        SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.YesterdayHonorableKills), (ushort)0);
        SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.LifetimeHonorableKills), 0u);
    }

    public bool RewardHonor(Unit victim, uint groupsize, int honor = -1, bool pvptoken = false)
    {
        // do not reward honor in arenas, but enable onkill spellproc
        if (InArena)
        {
            if (!victim || victim == this || !victim.IsTypeId(TypeId.Player))
                return false;

            return GetBgTeam() != victim.AsPlayer.GetBgTeam();
        }

        // 'Inactive' this aura prevents the player from gaining honor points and BattlegroundTokenizer
        if (HasAura(BattlegroundConst.SpellAuraPlayerInactive))
            return false;

        var victimGuid = ObjectGuid.Empty;
        uint victimRank = 0;

        // need call before fields update to have chance move yesterday data to appropriate fields before today data change.
        UpdateHonorFields();

        // do not reward honor in arenas, but return true to enable onkill spellproc
        if (InBattleground && Battleground && Battleground.IsArena())
            return true;

        // Promote to float for calculations
        float honorF = honor;

        if (honorF <= 0)
        {
            if (!victim || victim == this || victim.HasAuraType(AuraType.NoPvpCredit))
                return false;

            victimGuid = victim.GUID;
            var plrVictim = victim.AsPlayer;

            if (plrVictim)
            {
                if (EffectiveTeam == plrVictim.EffectiveTeam && !WorldMgr.IsFFAPvPRealm)
                    return false;

                var kLevel = (byte)Level;
                var kGrey = (byte)Formulas.GetGrayLevel(kLevel);
                var vLevel = (byte)victim.GetLevelForTarget(this);

                if (vLevel <= kGrey)
                    return false;

                // PLAYER_CHOSEN_TITLE VALUES DESCRIPTION
                //  [0]      Just name
                //  [1..14]  Alliance honor titles and player name
                //  [15..28] Horde honor titles and player name
                //  [29..38] Other title and player name
                //  [39+]    Nothing
                // this is all wrong, should be going off PvpTitle, not PlayerTitle
                uint victimTitle = plrVictim.PlayerData.PlayerTitle;

                // Get Killer titles, CharTitlesEntry.bit_index
                // Ranks:
                //  title[1..14]  . rank[5..18]
                //  title[15..28] . rank[5..18]
                //  title[other]  . 0
                if (victimTitle == 0)
                    victimGuid.Clear(); // Don't show HK: <rank> message, only log.
                else if (victimTitle < 15)
                    victimRank = victimTitle + 4;
                else if (victimTitle < 29)
                    victimRank = victimTitle - 14 + 4;
                else
                    victimGuid.Clear(); // Don't show HK: <rank> message, only log.

                honorF = (float)Math.Ceiling(Formulas.HKHonorAtLevelF(kLevel) * (vLevel - kGrey) / (kLevel - kGrey));

                // count the number of playerkills in one day
                ApplyModUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.TodayHonorableKills), (ushort)1, true);
                // and those in a lifetime
                ApplyModUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.LifetimeHonorableKills), 1u, true);
                UpdateCriteria(CriteriaType.HonorableKills);
                UpdateCriteria(CriteriaType.DeliverKillingBlowToClass, (uint)victim.Class);
                UpdateCriteria(CriteriaType.DeliverKillingBlowToRace, (uint)victim.Race);
                UpdateCriteria(CriteriaType.PVPKillInArea, Location.Area);
                UpdateCriteria(CriteriaType.EarnHonorableKill, 1, 0, 0, victim);
                UpdateCriteria(CriteriaType.KillPlayer, 1, 0, 0, victim);
            }
            else
            {
                if (!victim.AsCreature.IsRacialLeader)
                    return false;

                honorF = 100.0f; // ??? need more info
                victimRank = 19; // HK: Leader
            }
        }

        if (victim != null)
        {
            if (groupsize > 1)
                honorF /= groupsize;

            // apply honor multiplier from aura (not stacking-get highest)
            MathFunctions.AddPct(ref honorF, GetMaxPositiveAuraModifier(AuraType.ModHonorGainPct));
            honorF += (float)RestMgr.GetRestBonusFor(RestTypes.Honor, (uint)honorF);
        }

        honorF *= Configuration.GetDefaultValue("Rate.Honor", 1.0f);
        // Back to int now
        honor = (int)honorF;

        // honor - for show honor points in log
        // victim_guid - for show victim name in log
        // victim_rank [1..4]  HK: <dishonored rank>
        // victim_rank [5..19] HK: <alliance\horde rank>
        // victim_rank [0, 20+] HK: <>
        PvPCredit data = new()
        {
            Honor = honor,
            OriginalHonor = honor,
            Target = victimGuid,
            Rank = victimRank
        };

        SendPacket(data);

        AddHonorXp((uint)honor);

        if (InBattleground && honor > 0)
        {
            var bg = Battleground;

            bg?.UpdatePlayerScore(this, ScoreType.BonusHonor, (uint)honor, false); //false: prevent looping
        }

        if (!Configuration.GetDefaultValue("PvPToken.Enable", false) || !pvptoken)
            return true;

        if (victim != null && (!victim || victim == this || victim.HasAuraType(AuraType.NoPvpCredit)))
            return true;

        if (victim == null || !victim.IsTypeId(TypeId.Player))
            return true;

        // Check if allowed to receive it in current map
        var mapType = Configuration.GetDefaultValue("PvPToken.MapAllowType", 4);

        if ((mapType == 1 && !InBattleground && !IsFFAPvP) || (mapType == 2 && !IsFFAPvP) || (mapType == 3 && !InBattleground))
            return true;

        var itemId = Configuration.GetDefaultValue("PvPToken.ItemID", 29434u);
        var count = Configuration.GetDefaultValue("PvPToken.ItemCount", 1u);

        if (AddItem(itemId, count))
            SendSysMessage("You have been awarded a token for slaying another player.");

        return true;
    }

    public void SendPvpRewards()
    {
        //WorldPacket packet(SMSG_REQUEST_PVP_REWARDS_RESPONSE, 24);
        //SendPacket(packet);
    }

    public void SetArenaTeamIdInvited(uint arenaTeamId)
    {
        _arenaTeamIdInvited = arenaTeamId;
    }

    //Arenas
    public void SetArenaTeamInfoField(byte slot, ArenaTeamInfoType type, uint value) { }

    public void SetBattlegroundEntryPoint()
    {
        // Taxi path store
        if (!Taxi.Empty())
        {
            _bgData.MountSpell = 0;
            _bgData.TaxiPath[0] = Taxi.GetTaxiSource();
            _bgData.TaxiPath[1] = Taxi.GetTaxiDestination();

            // On taxi we don't need check for dungeon
            _bgData.JoinPos = new WorldLocation(Location.MapId, Location.X, Location.Y, Location.Z, Location.Orientation);
        }
        else
        {
            _bgData.ClearTaxiPath();

            // Mount spell id storing
            if (IsMounted)
            {
                var auras = GetAuraEffectsByType(AuraType.Mounted);

                if (!auras.Empty())
                    _bgData.MountSpell = auras[0].Id;
            }
            else
            {
                _bgData.MountSpell = 0;
            }

            // If map is dungeon find linked graveyard
            if (Location.Map.IsDungeon)
            {
                var entry = ObjectManager.GetClosestGraveYard(Location, Team, this);

                if (entry != null)
                    _bgData.JoinPos = entry.Location;
                else
                    Log.Logger.Error("SetBattlegroundEntryPoint: Dungeon map {0} has no linked graveyard, setting home location as entry point.", Location.MapId);
            }
            // If new entry point is not BG or arena set it
            else if (!Location.Map.IsBattlegroundOrArena)
            {
                _bgData.JoinPos = new WorldLocation(Location.MapId, Location.X, Location.Y, Location.Z, Location.Orientation);
            }
        }

        if (_bgData.JoinPos.MapId == 0xFFFFFFFF) // In error cases use homebind position
            _bgData.JoinPos = new WorldLocation(Homebind);
    }

    public void SetBattlegroundId(uint val, BattlegroundTypeId bgTypeId)
    {
        _bgData.BgInstanceId = val;
        _bgData.BgTypeId = bgTypeId;
    }

    public void SetBgTeam(TeamFaction team)
    {
        _bgData.BgTeam = (uint)team;
        SetArenaFaction((byte)(team == TeamFaction.Alliance ? 1 : 0));
    }

    public void SetInArenaTeam(uint arenaTeamId, byte slot, byte type)
    {
        SetArenaTeamInfoField(slot, ArenaTeamInfoType.Id, arenaTeamId);
        SetArenaTeamInfoField(slot, ArenaTeamInfoType.Type, type);
    }

    public void SetInviteForBattlegroundQueueType(BattlegroundQueueTypeId bgQueueTypeId, uint instanceId)
    {
        for (byte i = 0; i < SharedConst.MaxPlayerBGQueues; ++i)
            if (_battlegroundQueueIdRecs[i].BgQueueTypeId == bgQueueTypeId)
                _battlegroundQueueIdRecs[i].InvitedToInstance = instanceId;
    }

    public void SetRandomWinner(bool isWinner)
    {
        _isBgRandomWinner = isWinner;

        if (_isBgRandomWinner)
        {
            var stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_BATTLEGROUND_RANDOM);
            stmt.AddValue(0, GUID.Counter);
            CharacterDatabase.Execute(stmt);
        }
    }

    //PvP
    public void UpdateHonorFields()
    {
        // called when rewarding honor and at each save
        var now = GameTime.CurrentTime;
        var today = (GameTime.CurrentTime / Time.DAY) * Time.DAY;

        if (_lastHonorUpdateTime < today)
        {
            var yesterday = today - Time.DAY;

            // update yesterday's contribution
            if (_lastHonorUpdateTime >= yesterday)
                // this is the first update today, reset today's contribution
                SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.YesterdayHonorableKills), ActivePlayerData.TodayHonorableKills);
            else
                // no honor/kills yesterday or today, reset
                SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.YesterdayHonorableKills), (ushort)0);

            SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.TodayHonorableKills), (ushort)0);
        }

        _lastHonorUpdateTime = now;
    }
    private void _InitHonorLevelOnLoadFromDB(uint honor, uint honorLevel)
    {
        SetUpdateFieldValue(Values.ModifyValue(PlayerData).ModifyValue(PlayerData.HonorLevel), honorLevel);
        UpdateHonorNextLevel();

        AddHonorXp(honor);
    }

    private bool CanReportAfkDueToLimit()
    {
        // a player can complain about 15 people per 5 minutes
        if (_bgData.BgAfkReportedCount++ >= 15)
            return false;

        return true;
    }

    private void DisablePvpRules()
    {
        // Don't disable pvp rules when in pvp zone.
        if (IsInAreaThatActivatesPvpTalents())
            return;

        if (!CombatManager.HasPvPCombat())
        {
            RemoveAura(PlayerConst.SpellPvpRulesEnabled);
            UpdateItemLevelAreaBasedScaling();
        }
        else
        {
            var aura = GetAura(PlayerConst.SpellPvpRulesEnabled);

            aura?.SetDuration(aura.SpellInfo.MaxDuration);
        }
    }

    private bool HasPvpRulesEnabled()
    {
        return HasAura(PlayerConst.SpellPvpRulesEnabled);
    }

    private bool IsAreaThatActivatesPvpTalents(uint areaId)
    {
        if (InBattleground)
            return true;

        if (CliDB.AreaTableStorage.TryGetValue(areaId, out var area))
            do
            {
                if (area.IsSanctuary())
                    return false;

                if (area.HasFlag(AreaFlags.Arena))
                    return true;

                if (BattleFieldManager.IsWorldPvpArea(area.Id))
                    return true;

                area = CliDB.AreaTableStorage.LookupByKey(area.ParentAreaID);
            } while (area != null);

        return false;
    }

    private bool IsInAreaThatActivatesPvpTalents()
    {
        return IsAreaThatActivatesPvpTalents(Location.Area);
    }

    private void RewardPlayerWithRewardPack(uint rewardPackId)
    {
        RewardPlayerWithRewardPack(CliDB.RewardPackStorage.LookupByKey(rewardPackId));
    }

    private void RewardPlayerWithRewardPack(RewardPackRecord rewardPackEntry)
    {
        if (rewardPackEntry == null)
            return;

        if (CliDB.CharTitlesStorage.TryGetValue(rewardPackEntry.CharTitleID, out var charTitlesEntry))
            SetTitle(charTitlesEntry);

        ModifyMoney(rewardPackEntry.Money);

        var rewardCurrencyTypes = DB2Manager.GetRewardPackCurrencyTypesByRewardID(rewardPackEntry.Id);

        foreach (var currency in rewardCurrencyTypes)
            AddCurrency(currency.CurrencyTypeID, (uint)currency.Quantity /* TODO: CurrencyGainSource */);

        var rewardPackXItems = DB2Manager.GetRewardPackItemsByRewardID(rewardPackEntry.Id);

        foreach (var rewardPackXItem in rewardPackXItems)
            AddItem(rewardPackXItem.ItemID, rewardPackXItem.ItemQuantity);
    }

    private void SetHonorLevel(byte level)
    {
        var oldHonorLevel = (byte)HonorLevel;

        if (level == oldHonorLevel)
            return;

        SetUpdateFieldValue(Values.ModifyValue(PlayerData).ModifyValue(PlayerData.HonorLevel), level);
        UpdateHonorNextLevel();

        UpdateCriteria(CriteriaType.HonorLevelIncrease);
    }

    private void SetMercenaryForBattlegroundQueueType(BattlegroundQueueTypeId bgQueueTypeId, bool mercenary)
    {
        for (byte i = 0; i < SharedConst.MaxPlayerBGQueues; ++i)
            if (_battlegroundQueueIdRecs[i].BgQueueTypeId == bgQueueTypeId)
                _battlegroundQueueIdRecs[i].Mercenary = mercenary;
    }

    private void UpdateHonorNextLevel()
    {
        // 5500 at honor level 1
        // no idea what between here
        // 8800 at honor level ~14 (never goes above 8800)
        SetUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.HonorNextLevel), 8800u);
    }
}