// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Chrono;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Framework.Constants;
using Serilog;

namespace Forged.MapServer.BattleGrounds.Zones;

internal class BgWarsongGluch : Battleground
{
    private const uint ExploitTeleportLocationAlliance = 3784;
    private const uint ExploitTeleportLocationHorde = 3785;

    private readonly int[] _flagsDropTimer = new int[2];

    private readonly WsgFlagState[] _flagState = new WsgFlagState[2];

    // for checking Id state
    private readonly int[] _flagsTimer = new int[2];

    private readonly uint[][] _honor =
    {
        new uint[]
        {
            20, 40, 40
        }, // normal honor
        new uint[]
        {
            60, 40, 80
        } // holiday
    };

    private readonly ObjectGuid[] _mDroppedFlagGUID = new ObjectGuid[2];
    private readonly ObjectGuid[] _mFlagKeepers = new ObjectGuid[2]; // 0 - alliance, 1 - horde
    private bool _bothFlagsKept;
    private byte _flagDebuffState;
    private int _flagSpellForceTimer;
    private uint _lastFlagCaptureTeam; // Winner is based on this if score is equal

    private uint _mHonorEndKills;
    private uint _mHonorWinKills;

    private uint _mReputationCapture;
    // 0 - no debuffs, 1 - focused assault, 2 - brutal assault

    public BgWarsongGluch(BattlegroundTemplate battlegroundTemplate) : base(battlegroundTemplate)
    {
        BgObjects = new ObjectGuid[WsgObjectTypes.MAX];
        BgCreatures = new ObjectGuid[WsgCreatureTypes.MAX];

        StartMessageIds[BattlegroundConst.EventIdSecond] = WsgBroadcastTexts.START_ONE_MINUTE;
        StartMessageIds[BattlegroundConst.EventIdThird] = WsgBroadcastTexts.START_HALF_MINUTE;
        StartMessageIds[BattlegroundConst.EventIdFourth] = WsgBroadcastTexts.BATTLE_HAS_BEGUN;
    }

    public override void AddPlayer(Player player)
    {
        var isInBattleground = IsPlayerInBattleground(player.GUID);
        base.AddPlayer(player);

        if (!isInBattleground)
            PlayerScores[player.GUID] = new BattlegroundWgScore(player.GUID, player.GetBgTeam());
    }

    public override void EndBattleground(TeamFaction winner)
    {
        // Win reward
        if (winner == TeamFaction.Alliance)
            RewardHonorToTeam(GetBonusHonorFromKill(_mHonorWinKills), TeamFaction.Alliance);

        if (winner == TeamFaction.Horde)
            RewardHonorToTeam(GetBonusHonorFromKill(_mHonorWinKills), TeamFaction.Horde);

        // Complete map_end rewards (even if no team wins)
        RewardHonorToTeam(GetBonusHonorFromKill(_mHonorEndKills), TeamFaction.Alliance);
        RewardHonorToTeam(GetBonusHonorFromKill(_mHonorEndKills), TeamFaction.Horde);

        base.EndBattleground(winner);
    }

    public override void EventPlayerClickedOnFlag(Player player, GameObject targetObj)
    {
        if (Status != BattlegroundStatus.InProgress)
            return;

        var team = GetPlayerTeam(player.GUID);

        //alliance Id picked up from base
        if (team == TeamFaction.Horde && GetFlagState(TeamFaction.Alliance) == WsgFlagState.OnBase && BgObjects[WsgObjectTypes.A_FLAG] == targetObj.GUID)
        {
            SendBroadcastText(WsgBroadcastTexts.ALLIANCE_FLAG_PICKED_UP, ChatMsg.BgSystemHorde, player);
            PlaySoundToAll(WsgSound.ALLIANCE_FLAG_PICKED_UP);
            SpawnBGObject(WsgObjectTypes.A_FLAG, BattlegroundConst.RespawnOneDay);
            SetAllianceFlagPicker(player.GUID);
            _flagState[TeamIds.Alliance] = WsgFlagState.OnPlayer;
            //update world state to show correct Id carrier
            UpdateFlagState(TeamFaction.Horde, WsgFlagState.OnPlayer);
            player.CastSpell(player, WsgSpellId.SILVERWING_FLAG, true);
            player.StartCriteriaTimer(CriteriaStartEvent.BeSpellTarget, WsgSpellId.SILVERWING_FLAG_PICKED);

            if (_flagState[1] == WsgFlagState.OnPlayer)
                _bothFlagsKept = true;

            if (_flagDebuffState == 1)
                player.CastSpell(player, WsgSpellId.FOCUSED_ASSAULT, true);
            else if (_flagDebuffState == 2)
                player.CastSpell(player, WsgSpellId.BRUTAL_ASSAULT, true);
        }

        //horde Id picked up from base
        if (team == TeamFaction.Alliance && GetFlagState(TeamFaction.Horde) == WsgFlagState.OnBase && BgObjects[WsgObjectTypes.H_FLAG] == targetObj.GUID)
        {
            SendBroadcastText(WsgBroadcastTexts.HORDE_FLAG_PICKED_UP, ChatMsg.BgSystemAlliance, player);
            PlaySoundToAll(WsgSound.HORDE_FLAG_PICKED_UP);
            SpawnBGObject(WsgObjectTypes.H_FLAG, BattlegroundConst.RespawnOneDay);
            SetHordeFlagPicker(player.GUID);
            _flagState[TeamIds.Horde] = WsgFlagState.OnPlayer;
            //update world state to show correct Id carrier
            UpdateFlagState(TeamFaction.Alliance, WsgFlagState.OnPlayer);
            player.CastSpell(player, WsgSpellId.WARSONG_FLAG, true);
            player.StartCriteriaTimer(CriteriaStartEvent.BeSpellTarget, WsgSpellId.WARSONG_FLAG_PICKED);

            if (_flagState[0] == WsgFlagState.OnPlayer)
                _bothFlagsKept = true;

            if (_flagDebuffState == 1)
                player.CastSpell(player, WsgSpellId.FOCUSED_ASSAULT, true);
            else if (_flagDebuffState == 2)
                player.CastSpell(player, WsgSpellId.BRUTAL_ASSAULT, true);
        }

        //Alliance Id on ground(not in base) (returned or picked up again from ground!)
        if (GetFlagState(TeamFaction.Alliance) == WsgFlagState.OnGround && player.Location.IsWithinDistInMap(targetObj, 10) && targetObj.Template.entry == WsgObjectEntry.A_FLAG_GROUND)
        {
            if (team == TeamFaction.Alliance)
            {
                SendBroadcastText(WsgBroadcastTexts.ALLIANCE_FLAG_RETURNED, ChatMsg.BgSystemAlliance, player);
                UpdateFlagState(TeamFaction.Horde, WsgFlagState.WaitRespawn);
                RespawnFlag(TeamFaction.Alliance, false);
                SpawnBGObject(WsgObjectTypes.A_FLAG, BattlegroundConst.RespawnImmediately);
                PlaySoundToAll(WsgSound.FLAG_RETURNED);
                UpdatePlayerScore(player, ScoreType.FlagReturns, 1);
                _bothFlagsKept = false;

                HandleFlagRoomCapturePoint(TeamIds.Horde); // Check Horde Id if it is in capture zone; if so, capture it
            }
            else
            {
                SendBroadcastText(WsgBroadcastTexts.ALLIANCE_FLAG_PICKED_UP, ChatMsg.BgSystemHorde, player);
                PlaySoundToAll(WsgSound.ALLIANCE_FLAG_PICKED_UP);
                SpawnBGObject(WsgObjectTypes.A_FLAG, BattlegroundConst.RespawnOneDay);
                SetAllianceFlagPicker(player.GUID);
                player.CastSpell(player, WsgSpellId.SILVERWING_FLAG, true);
                _flagState[TeamIds.Alliance] = WsgFlagState.OnPlayer;
                UpdateFlagState(TeamFaction.Horde, WsgFlagState.OnPlayer);

                if (_flagDebuffState == 1)
                    player.CastSpell(player, WsgSpellId.FOCUSED_ASSAULT, true);
                else if (_flagDebuffState == 2)
                    player.CastSpell(player, WsgSpellId.BRUTAL_ASSAULT, true);
            }
            //called in HandleGameObjectUseOpcode:
            //target_obj.Delete();
        }

        //Horde Id on ground(not in base) (returned or picked up again)
        if (GetFlagState(TeamFaction.Horde) == WsgFlagState.OnGround && player.Location.IsWithinDistInMap(targetObj, 10) && targetObj.Template.entry == WsgObjectEntry.H_FLAG_GROUND)
        {
            if (team == TeamFaction.Horde)
            {
                SendBroadcastText(WsgBroadcastTexts.HORDE_FLAG_RETURNED, ChatMsg.BgSystemHorde, player);
                UpdateFlagState(TeamFaction.Alliance, WsgFlagState.WaitRespawn);
                RespawnFlag(TeamFaction.Horde, false);
                SpawnBGObject(WsgObjectTypes.H_FLAG, BattlegroundConst.RespawnImmediately);
                PlaySoundToAll(WsgSound.FLAG_RETURNED);
                UpdatePlayerScore(player, ScoreType.FlagReturns, 1);
                _bothFlagsKept = false;

                HandleFlagRoomCapturePoint(TeamIds.Alliance); // Check Alliance Id if it is in capture zone; if so, capture it
            }
            else
            {
                SendBroadcastText(WsgBroadcastTexts.HORDE_FLAG_PICKED_UP, ChatMsg.BgSystemAlliance, player);
                PlaySoundToAll(WsgSound.HORDE_FLAG_PICKED_UP);
                SpawnBGObject(WsgObjectTypes.H_FLAG, BattlegroundConst.RespawnOneDay);
                SetHordeFlagPicker(player.GUID);
                player.CastSpell(player, WsgSpellId.WARSONG_FLAG, true);
                _flagState[TeamIds.Horde] = WsgFlagState.OnPlayer;
                UpdateFlagState(TeamFaction.Alliance, WsgFlagState.OnPlayer);

                if (_flagDebuffState == 1)
                    player.CastSpell(player, WsgSpellId.FOCUSED_ASSAULT, true);
                else if (_flagDebuffState == 2)
                    player.CastSpell(player, WsgSpellId.BRUTAL_ASSAULT, true);
            }
            //called in HandleGameObjectUseOpcode:
            //target_obj.Delete();
        }

        player.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.PvPActive);
    }

    public override void EventPlayerDroppedFlag(Player player)
    {
        var team = GetPlayerTeam(player.GUID);

        if (Status != BattlegroundStatus.InProgress)
        {
            // if not running, do not cast things at the dropper player (prevent spawning the "dropped" Id), neither send unnecessary messages
            // just take off the aura
            if (team == TeamFaction.Alliance)
            {
                if (!IsHordeFlagPickedup())
                    return;

                if (GetFlagPickerGUID(TeamIds.Horde) == player.GUID)
                {
                    SetHordeFlagPicker(ObjectGuid.Empty);
                    player.RemoveAura(WsgSpellId.WARSONG_FLAG);
                }
            }
            else
            {
                if (!IsAllianceFlagPickedup())
                    return;

                if (GetFlagPickerGUID(TeamIds.Alliance) == player.GUID)
                {
                    SetAllianceFlagPicker(ObjectGuid.Empty);
                    player.RemoveAura(WsgSpellId.SILVERWING_FLAG);
                }
            }

            return;
        }

        var set = false;

        if (team == TeamFaction.Alliance)
        {
            if (!IsHordeFlagPickedup())
                return;

            if (GetFlagPickerGUID(TeamIds.Horde) == player.GUID)
            {
                SetHordeFlagPicker(ObjectGuid.Empty);
                player.RemoveAura(WsgSpellId.WARSONG_FLAG);

                if (_flagDebuffState == 1)
                    player.RemoveAura(WsgSpellId.FOCUSED_ASSAULT);
                else if (_flagDebuffState == 2)
                    player.RemoveAura(WsgSpellId.BRUTAL_ASSAULT);

                _flagState[TeamIds.Horde] = WsgFlagState.OnGround;
                player.CastSpell(player, WsgSpellId.WARSONG_FLAG_DROPPED, true);
                set = true;
            }
        }
        else
        {
            if (!IsAllianceFlagPickedup())
                return;

            if (GetFlagPickerGUID(TeamIds.Alliance) == player.GUID)
            {
                SetAllianceFlagPicker(ObjectGuid.Empty);
                player.RemoveAura(WsgSpellId.SILVERWING_FLAG);

                if (_flagDebuffState == 1)
                    player.RemoveAura(WsgSpellId.FOCUSED_ASSAULT);
                else if (_flagDebuffState == 2)
                    player.RemoveAura(WsgSpellId.BRUTAL_ASSAULT);

                _flagState[TeamIds.Alliance] = WsgFlagState.OnGround;
                player.CastSpell(player, WsgSpellId.SILVERWING_FLAG_DROPPED, true);
                set = true;
            }
        }

        if (set)
        {
            player.CastSpell(player, BattlegroundConst.SpellRecentlyDroppedFlag, true);
            UpdateFlagState(team, WsgFlagState.OnGround);

            if (team == TeamFaction.Alliance)
                SendBroadcastText(WsgBroadcastTexts.HORDE_FLAG_DROPPED, ChatMsg.BgSystemHorde, player);
            else
                SendBroadcastText(WsgBroadcastTexts.ALLIANCE_FLAG_DROPPED, ChatMsg.BgSystemAlliance, player);

            _flagsDropTimer[GetTeamIndexByTeamId(GetOtherTeam(team))] = WsgTimerOrScore.FLAG_DROP_TIME;
        }
    }

    public override WorldSafeLocsEntry GetClosestGraveYard(Player player)
    {
        //if status in progress, it returns main graveyards with spiritguides
        //else it will return the graveyard in the flagroom - this is especially good
        //if a player dies in preparation phase - then the player can't cheat
        //and teleport to the graveyard outside the flagroom
        //and start running around, while the doors are still closed
        if (GetPlayerTeam(player.GUID) == TeamFaction.Alliance)
        {
            if (Status == BattlegroundStatus.InProgress)
                return Global.ObjectMgr.GetWorldSafeLoc(WsgGraveyards.MAIN_ALLIANCE);
            else
                return Global.ObjectMgr.GetWorldSafeLoc(WsgGraveyards.FLAG_ROOM_ALLIANCE);
        }
        else
        {
            if (Status == BattlegroundStatus.InProgress)
                return Global.ObjectMgr.GetWorldSafeLoc(WsgGraveyards.MAIN_HORDE);
            else
                return Global.ObjectMgr.GetWorldSafeLoc(WsgGraveyards.FLAG_ROOM_HORDE);
        }
    }

    public override WorldSafeLocsEntry GetExploitTeleportLocation(TeamFaction team)
    {
        return Global.ObjectMgr.GetWorldSafeLoc(team == TeamFaction.Alliance ? ExploitTeleportLocationAlliance : ExploitTeleportLocationHorde);
    }

    public override ObjectGuid GetFlagPickerGUID(int team = -1)
    {
        if (team is TeamIds.Alliance or TeamIds.Horde)
            return _mFlagKeepers[team];

        return ObjectGuid.Empty;
    }

    public override TeamFaction GetPrematureWinner()
    {
        if (GetTeamScore(TeamIds.Alliance) > GetTeamScore(TeamIds.Horde))
            return TeamFaction.Alliance;
        else if (GetTeamScore(TeamIds.Horde) > GetTeamScore(TeamIds.Alliance))
            return TeamFaction.Horde;

        return base.GetPrematureWinner();
    }

    public override void HandleAreaTrigger(Player player, uint trigger, bool entered)
    {
        //uint SpellId = 0;
        //uint64 buff_guid = 0;
        switch (trigger)
        {
            case 8965: // Horde Start
            case 8966: // Alliance Start
                if (Status == BattlegroundStatus.WaitJoin && !entered)
                    TeleportPlayerToExploitLocation(player);

                break;
            case 3686: // Alliance elixir of speed spawn. Trigger not working, because located inside other areatrigger, can be replaced by IsWithinDist(object, dist) in Battleground.Update().
                //buff_guid = BgObjects[BG_WS_OBJECT_SPEEDBUFF_1];
                break;
            case 3687: // Horde elixir of speed spawn. Trigger not working, because located inside other areatrigger, can be replaced by IsWithinDist(object, dist) in Battleground.Update().
                //buff_guid = BgObjects[BG_WS_OBJECT_SPEEDBUFF_2];
                break;
            case 3706: // Alliance elixir of regeneration spawn
                //buff_guid = BgObjects[BG_WS_OBJECT_REGENBUFF_1];
                break;
            case 3708: // Horde elixir of regeneration spawn
                //buff_guid = BgObjects[BG_WS_OBJECT_REGENBUFF_2];
                break;
            case 3707: // Alliance elixir of berserk spawn
                //buff_guid = BgObjects[BG_WS_OBJECT_BERSERKBUFF_1];
                break;
            case 3709: // Horde elixir of berserk spawn
                //buff_guid = BgObjects[BG_WS_OBJECT_BERSERKBUFF_2];
                break;
            case 3646: // Alliance Flag spawn
                if (_flagState[TeamIds.Horde] != 0 && _flagState[TeamIds.Alliance] == 0)
                    if (GetFlagPickerGUID(TeamIds.Horde) == player.GUID)
                        EventPlayerCapturedFlag(player);

                break;
            case 3647: // Horde Flag spawn
                if (_flagState[TeamIds.Alliance] != 0 && _flagState[TeamIds.Horde] == 0)
                    if (GetFlagPickerGUID(TeamIds.Alliance) == player.GUID)
                        EventPlayerCapturedFlag(player);

                break;
            case 3649: // unk1
            case 3688: // unk2
            case 4628: // unk3
            case 4629: // unk4
                break;
            default:
                base.HandleAreaTrigger(player, trigger, entered);

                break;
        }

        //if (buff_guid)
        //    HandleTriggerBuff(buff_guid, player);
    }

    public override void HandleKillPlayer(Player victim, Player killer)
    {
        if (Status != BattlegroundStatus.InProgress)
            return;

        EventPlayerDroppedFlag(victim);

        base.HandleKillPlayer(victim, killer);
    }

    public override void PostUpdateImpl(uint diff)
    {
        if (Status == BattlegroundStatus.InProgress)
        {
            if (ElapsedTime >= 17 * Time.MINUTE * Time.IN_MILLISECONDS)
            {
                if (GetTeamScore(TeamIds.Alliance) == 0)
                {
                    if (GetTeamScore(TeamIds.Horde) == 0) // No one scored - result is tie
                        EndBattleground(TeamFaction.Other);
                    else // Horde has more points and thus wins
                        EndBattleground(TeamFaction.Horde);
                }
                else if (GetTeamScore(TeamIds.Horde) == 0)
                    EndBattleground(TeamFaction.Alliance);                              // Alliance has > 0, Horde has 0, alliance wins
                else if (GetTeamScore(TeamIds.Horde) == GetTeamScore(TeamIds.Alliance)) // Team score equal, winner is team that scored the last Id
                    EndBattleground((TeamFaction)_lastFlagCaptureTeam);
                else if (GetTeamScore(TeamIds.Horde) > GetTeamScore(TeamIds.Alliance)) // Last but not least, check who has the higher score
                    EndBattleground(TeamFaction.Horde);
                else
                    EndBattleground(TeamFaction.Alliance);
            }

            if (_flagState[TeamIds.Alliance] == WsgFlagState.WaitRespawn)
            {
                _flagsTimer[TeamIds.Alliance] -= (int)diff;

                if (_flagsTimer[TeamIds.Alliance] < 0)
                {
                    _flagsTimer[TeamIds.Alliance] = 0;
                    RespawnFlag(TeamFaction.Alliance, true);
                }
            }

            if (_flagState[TeamIds.Alliance] == WsgFlagState.OnGround)
            {
                _flagsDropTimer[TeamIds.Alliance] -= (int)diff;

                if (_flagsDropTimer[TeamIds.Alliance] < 0)
                {
                    _flagsDropTimer[TeamIds.Alliance] = 0;
                    RespawnFlagAfterDrop(TeamFaction.Alliance);
                    _bothFlagsKept = false;
                }
            }

            if (_flagState[TeamIds.Horde] == WsgFlagState.WaitRespawn)
            {
                _flagsTimer[TeamIds.Horde] -= (int)diff;

                if (_flagsTimer[TeamIds.Horde] < 0)
                {
                    _flagsTimer[TeamIds.Horde] = 0;
                    RespawnFlag(TeamFaction.Horde, true);
                }
            }

            if (_flagState[TeamIds.Horde] == WsgFlagState.OnGround)
            {
                _flagsDropTimer[TeamIds.Horde] -= (int)diff;

                if (_flagsDropTimer[TeamIds.Horde] < 0)
                {
                    _flagsDropTimer[TeamIds.Horde] = 0;
                    RespawnFlagAfterDrop(TeamFaction.Horde);
                    _bothFlagsKept = false;
                }
            }

            if (_bothFlagsKept)
            {
                _flagSpellForceTimer += (int)diff;

                if (_flagDebuffState == 0 && _flagSpellForceTimer >= 10 * Time.MINUTE * Time.IN_MILLISECONDS) //10 minutes
                {
                    // Apply Stage 1 (Focused Assault)
                    var player = Global.ObjAccessor.FindPlayer(_mFlagKeepers[0]);

                    if (player)
                        player.CastSpell(player, WsgSpellId.FOCUSED_ASSAULT, true);

                    player = Global.ObjAccessor.FindPlayer(_mFlagKeepers[1]);

                    if (player)
                        player.CastSpell(player, WsgSpellId.FOCUSED_ASSAULT, true);

                    _flagDebuffState = 1;
                }
                else if (_flagDebuffState == 1 && _flagSpellForceTimer >= 900000) //15 minutes
                {
                    // Apply Stage 2 (Brutal Assault)
                    var player = Global.ObjAccessor.FindPlayer(_mFlagKeepers[0]);

                    if (player)
                    {
                        player.RemoveAura(WsgSpellId.FOCUSED_ASSAULT);
                        player.CastSpell(player, WsgSpellId.BRUTAL_ASSAULT, true);
                    }

                    player = Global.ObjAccessor.FindPlayer(_mFlagKeepers[1]);

                    if (player)
                    {
                        player.RemoveAura(WsgSpellId.FOCUSED_ASSAULT);
                        player.CastSpell(player, WsgSpellId.BRUTAL_ASSAULT, true);
                    }

                    _flagDebuffState = 2;
                }
            }
            else if ((_flagState[TeamIds.Alliance] == WsgFlagState.OnBase || _flagState[TeamIds.Alliance] == WsgFlagState.WaitRespawn) &&
                     (_flagState[TeamIds.Horde] == WsgFlagState.OnBase || _flagState[TeamIds.Horde] == WsgFlagState.WaitRespawn))
            {
                // Both flags are in base or awaiting respawn.
                // Remove assault debuffs, reset timers

                var player = Global.ObjAccessor.FindPlayer(_mFlagKeepers[0]);

                if (player)
                {
                    player.RemoveAura(WsgSpellId.FOCUSED_ASSAULT);
                    player.RemoveAura(WsgSpellId.BRUTAL_ASSAULT);
                }

                player = Global.ObjAccessor.FindPlayer(_mFlagKeepers[1]);

                if (player)
                {
                    player.RemoveAura(WsgSpellId.FOCUSED_ASSAULT);
                    player.RemoveAura(WsgSpellId.BRUTAL_ASSAULT);
                }

                _flagSpellForceTimer = 0; //reset timer.
                _flagDebuffState = 0;
            }
        }
    }

    public override void RemovePlayer(Player player, ObjectGuid guid, TeamFaction team)
    {
        // sometimes Id aura not removed :(
        if (IsAllianceFlagPickedup() && _mFlagKeepers[TeamIds.Alliance] == guid)
        {
            if (!player)
            {
                Log.Logger.Error("BattlegroundWS: Removing offline player who has the FLAG!!");
                SetAllianceFlagPicker(ObjectGuid.Empty);
                RespawnFlag(TeamFaction.Alliance, false);
            }
            else
                EventPlayerDroppedFlag(player);
        }

        if (IsHordeFlagPickedup() && _mFlagKeepers[TeamIds.Horde] == guid)
        {
            if (!player)
            {
                Log.Logger.Error("BattlegroundWS: Removing offline player who has the FLAG!!");
                SetHordeFlagPicker(ObjectGuid.Empty);
                RespawnFlag(TeamFaction.Horde, false);
            }
            else
                EventPlayerDroppedFlag(player);
        }
    }

    public override void Reset()
    {
        //call parent's class reset
        base.Reset();

        _mFlagKeepers[TeamIds.Alliance].Clear();
        _mFlagKeepers[TeamIds.Horde].Clear();
        _mDroppedFlagGUID[TeamIds.Alliance] = ObjectGuid.Empty;
        _mDroppedFlagGUID[TeamIds.Horde] = ObjectGuid.Empty;
        _flagState[TeamIds.Alliance] = WsgFlagState.OnBase;
        _flagState[TeamIds.Horde] = WsgFlagState.OnBase;
        MTeamScores[TeamIds.Alliance] = 0;
        MTeamScores[TeamIds.Horde] = 0;

        if (Global.BattlegroundMgr.IsBGWeekend(GetTypeID()))
        {
            _mReputationCapture = 45;
            _mHonorWinKills = 3;
            _mHonorEndKills = 4;
        }
        else
        {
            _mReputationCapture = 35;
            _mHonorWinKills = 1;
            _mHonorEndKills = 2;
        }

        _lastFlagCaptureTeam = 0;
        _bothFlagsKept = false;
        _flagDebuffState = 0;
        _flagSpellForceTimer = 0;
        _flagsDropTimer[TeamIds.Alliance] = 0;
        _flagsDropTimer[TeamIds.Horde] = 0;
        _flagsTimer[TeamIds.Alliance] = 0;
        _flagsTimer[TeamIds.Horde] = 0;
    }

    public override void SetDroppedFlagGUID(ObjectGuid guid, int team = -1)
    {
        if (team is TeamIds.Alliance or TeamIds.Horde)
            _mDroppedFlagGUID[team] = guid;
    }

    public override bool SetupBattleground()
    {
        var result = true;
        result &= AddObject(WsgObjectTypes.A_FLAG, WsgObjectEntry.A_FLAG, 1540.423f, 1481.325f, 351.8284f, 3.089233f, 0, 0, 0.9996573f, 0.02617699f, WsgTimerOrScore.FLAG_RESPAWN_TIME / 1000);
        result &= AddObject(WsgObjectTypes.H_FLAG, WsgObjectEntry.H_FLAG, 916.0226f, 1434.405f, 345.413f, 0.01745329f, 0, 0, 0.008726535f, 0.9999619f, WsgTimerOrScore.FLAG_RESPAWN_TIME / 1000);

        if (!result)
        {
            Log.Logger.Error("BgWarsongGluch: Failed to spawn Id object!");

            return false;
        }

        // buffs
        result &= AddObject(WsgObjectTypes.SPEEDBUFF1, BuffEntries[0], 1449.93f, 1470.71f, 342.6346f, -1.64061f, 0, 0, 0.7313537f, -0.6819983f, BattlegroundConst.BuffRespawnTime);
        result &= AddObject(WsgObjectTypes.SPEEDBUFF2, BuffEntries[0], 1005.171f, 1447.946f, 335.9032f, 1.64061f, 0, 0, 0.7313537f, 0.6819984f, BattlegroundConst.BuffRespawnTime);
        result &= AddObject(WsgObjectTypes.REGENBUFF1, BuffEntries[1], 1317.506f, 1550.851f, 313.2344f, -0.2617996f, 0, 0, 0.1305263f, -0.9914448f, BattlegroundConst.BuffRespawnTime);
        result &= AddObject(WsgObjectTypes.REGENBUFF2, BuffEntries[1], 1110.451f, 1353.656f, 316.5181f, -0.6806787f, 0, 0, 0.333807f, -0.9426414f, BattlegroundConst.BuffRespawnTime);
        result &= AddObject(WsgObjectTypes.BERSERKBUFF1, BuffEntries[2], 1320.09f, 1378.79f, 314.7532f, 1.186824f, 0, 0, 0.5591929f, 0.8290376f, BattlegroundConst.BuffRespawnTime);
        result &= AddObject(WsgObjectTypes.BERSERKBUFF2, BuffEntries[2], 1139.688f, 1560.288f, 306.8432f, -2.443461f, 0, 0, 0.9396926f, -0.3420201f, BattlegroundConst.BuffRespawnTime);

        if (!result)
        {
            Log.Logger.Error("BgWarsongGluch: Failed to spawn buff object!");

            return false;
        }

        // alliance gates
        result &= AddObject(WsgObjectTypes.DOOR_A1, WsgObjectEntry.DOOR_A1, 1503.335f, 1493.466f, 352.1888f, 3.115414f, 0, 0, 0.9999143f, 0.01308903f);
        result &= AddObject(WsgObjectTypes.DOOR_A2, WsgObjectEntry.DOOR_A2, 1492.478f, 1457.912f, 342.9689f, 3.115414f, 0, 0, 0.9999143f, 0.01308903f);
        result &= AddObject(WsgObjectTypes.DOOR_A3, WsgObjectEntry.DOOR_A3, 1468.503f, 1494.357f, 351.8618f, 3.115414f, 0, 0, 0.9999143f, 0.01308903f);
        result &= AddObject(WsgObjectTypes.DOOR_A4, WsgObjectEntry.DOOR_A4, 1471.555f, 1458.778f, 362.6332f, 3.115414f, 0, 0, 0.9999143f, 0.01308903f);
        result &= AddObject(WsgObjectTypes.DOOR_A5, WsgObjectEntry.DOOR_A5, 1492.347f, 1458.34f, 342.3712f, -0.03490669f, 0, 0, 0.01745246f, -0.9998477f);
        result &= AddObject(WsgObjectTypes.DOOR_A6, WsgObjectEntry.DOOR_A6, 1503.466f, 1493.367f, 351.7352f, -0.03490669f, 0, 0, 0.01745246f, -0.9998477f);
        // horde gates
        result &= AddObject(WsgObjectTypes.DOOR_H1, WsgObjectEntry.DOOR_H1, 949.1663f, 1423.772f, 345.6241f, -0.5756807f, -0.01673368f, -0.004956111f, -0.2839723f, 0.9586737f);
        result &= AddObject(WsgObjectTypes.DOOR_H2, WsgObjectEntry.DOOR_H2, 953.0507f, 1459.842f, 340.6526f, -1.99662f, -0.1971825f, 0.1575096f, -0.8239487f, 0.5073641f);
        result &= AddObject(WsgObjectTypes.DOOR_H3, WsgObjectEntry.DOOR_H3, 949.9523f, 1422.751f, 344.9273f, 0.0f, 0, 0, 0, 1);
        result &= AddObject(WsgObjectTypes.DOOR_H4, WsgObjectEntry.DOOR_H4, 950.7952f, 1459.583f, 342.1523f, 0.05235988f, 0, 0, 0.02617695f, 0.9996573f);

        if (!result)
        {
            Log.Logger.Error("BgWarsongGluch: Failed to spawn door object Battleground not created!");

            return false;
        }

        var sg = Global.ObjectMgr.GetWorldSafeLoc(WsgGraveyards.MAIN_ALLIANCE);

        if (sg == null || !AddSpiritGuide(WsgCreatureTypes.SPIRIT_MAIN_ALLIANCE, sg.Loc.X, sg.Loc.Y, sg.Loc.Z, 3.124139f, TeamIds.Alliance))
        {
            Log.Logger.Error("BgWarsongGluch: Failed to spawn Alliance spirit guide! Battleground not created!");

            return false;
        }

        sg = Global.ObjectMgr.GetWorldSafeLoc(WsgGraveyards.MAIN_HORDE);

        if (sg == null || !AddSpiritGuide(WsgCreatureTypes.SPIRIT_MAIN_HORDE, sg.Loc.X, sg.Loc.Y, sg.Loc.Z, 3.193953f, TeamIds.Horde))
        {
            Log.Logger.Error("BgWarsongGluch: Failed to spawn Horde spirit guide! Battleground not created!");

            return false;
        }

        return true;
    }

    public override void StartingEventCloseDoors()
    {
        for (var i = WsgObjectTypes.DOOR_A1; i <= WsgObjectTypes.DOOR_H4; ++i)
        {
            DoorClose(i);
            SpawnBGObject(i, BattlegroundConst.RespawnImmediately);
        }

        for (var i = WsgObjectTypes.A_FLAG; i <= WsgObjectTypes.BERSERKBUFF2; ++i)
            SpawnBGObject(i, BattlegroundConst.RespawnOneDay);
    }

    public override void StartingEventOpenDoors()
    {
        for (var i = WsgObjectTypes.DOOR_A1; i <= WsgObjectTypes.DOOR_A6; ++i)
            DoorOpen(i);

        for (var i = WsgObjectTypes.DOOR_H1; i <= WsgObjectTypes.DOOR_H4; ++i)
            DoorOpen(i);

        for (var i = WsgObjectTypes.A_FLAG; i <= WsgObjectTypes.BERSERKBUFF2; ++i)
            SpawnBGObject(i, BattlegroundConst.RespawnImmediately);

        SpawnBGObject(WsgObjectTypes.DOOR_A5, BattlegroundConst.RespawnOneDay);
        SpawnBGObject(WsgObjectTypes.DOOR_A6, BattlegroundConst.RespawnOneDay);
        SpawnBGObject(WsgObjectTypes.DOOR_H3, BattlegroundConst.RespawnOneDay);
        SpawnBGObject(WsgObjectTypes.DOOR_H4, BattlegroundConst.RespawnOneDay);

        UpdateWorldState(WsgWorldStates.STATE_TIMER_ACTIVE, 1);
        UpdateWorldState(WsgWorldStates.STATE_TIMER, (int)(GameTime.CurrentTime + 15 * Time.MINUTE));

        // players joining later are not eligibles
        TriggerGameEvent(8563);
    }

    public override bool UpdatePlayerScore(Player player, ScoreType type, uint value, bool doAddHonor = true)
    {
        if (!base.UpdatePlayerScore(player, type, value, doAddHonor))
            return false;

        switch (type)
        {
            case ScoreType.FlagCaptures: // flags captured
                player.UpdateCriteria(CriteriaType.TrackedWorldStateUIModified, WsObjectives.CAPTURE_FLAG);

                break;
            case ScoreType.FlagReturns: // flags returned
                player.UpdateCriteria(CriteriaType.TrackedWorldStateUIModified, WsObjectives.RETURN_FLAG);

                break;
        }

        return true;
    }

    private void AddPoint(TeamFaction team, uint points = 1)
    {
        MTeamScores[GetTeamIndexByTeamId(team)] += points;
    }

    private void EventPlayerCapturedFlag(Player player)
    {
        if (Status != BattlegroundStatus.InProgress)
            return;

        TeamFaction winner = 0;

        player.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.PvPActive);
        var team = GetPlayerTeam(player.GUID);

        if (team == TeamFaction.Alliance)
        {
            if (!IsHordeFlagPickedup())
                return;

            SetHordeFlagPicker(ObjectGuid.Empty); // must be before aura remove to prevent 2 events (drop+capture) at the same time
            // horde Id in base (but not respawned yet)
            _flagState[TeamIds.Horde] = WsgFlagState.WaitRespawn;
            // Drop Horde Flag from Player
            player.RemoveAura(WsgSpellId.WARSONG_FLAG);

            if (_flagDebuffState == 1)
                player.RemoveAura(WsgSpellId.FOCUSED_ASSAULT);
            else if (_flagDebuffState == 2)
                player.RemoveAura(WsgSpellId.BRUTAL_ASSAULT);

            if (GetTeamScore(TeamIds.Alliance) < WsgTimerOrScore.MAX_TEAM_SCORE)
                AddPoint(TeamFaction.Alliance);

            PlaySoundToAll(WsgSound.FLAG_CAPTURED_ALLIANCE);
            RewardReputationToTeam(890, _mReputationCapture, TeamFaction.Alliance);
        }
        else
        {
            if (!IsAllianceFlagPickedup())
                return;

            SetAllianceFlagPicker(ObjectGuid.Empty); // must be before aura remove to prevent 2 events (drop+capture) at the same time
            // alliance Id in base (but not respawned yet)
            _flagState[TeamIds.Alliance] = WsgFlagState.WaitRespawn;
            // Drop Alliance Flag from Player
            player.RemoveAura(WsgSpellId.SILVERWING_FLAG);

            if (_flagDebuffState == 1)
                player.RemoveAura(WsgSpellId.FOCUSED_ASSAULT);
            else if (_flagDebuffState == 2)
                player.RemoveAura(WsgSpellId.BRUTAL_ASSAULT);

            if (GetTeamScore(TeamIds.Horde) < WsgTimerOrScore.MAX_TEAM_SCORE)
                AddPoint(TeamFaction.Horde);

            PlaySoundToAll(WsgSound.FLAG_CAPTURED_HORDE);
            RewardReputationToTeam(889, _mReputationCapture, TeamFaction.Horde);
        }

        //for Id capture is reward 2 honorable kills
        RewardHonorToTeam(GetBonusHonorFromKill(2), team);

        SpawnBGObject(WsgObjectTypes.H_FLAG, WsgTimerOrScore.FLAG_RESPAWN_TIME);
        SpawnBGObject(WsgObjectTypes.A_FLAG, WsgTimerOrScore.FLAG_RESPAWN_TIME);

        if (team == TeamFaction.Alliance)
            SendBroadcastText(WsgBroadcastTexts.CAPTURED_HORDE_FLAG, ChatMsg.BgSystemAlliance, player);
        else
            SendBroadcastText(WsgBroadcastTexts.CAPTURED_ALLIANCE_FLAG, ChatMsg.BgSystemHorde, player);

        UpdateFlagState(team, WsgFlagState.WaitRespawn); // Id state none
        UpdateTeamScore(GetTeamIndexByTeamId(team));
        // only Id capture should be updated
        UpdatePlayerScore(player, ScoreType.FlagCaptures, 1); // +1 Id captures

        // update last Id capture to be used if teamscore is equal
        SetLastFlagCapture(team);

        if (GetTeamScore(TeamIds.Alliance) == WsgTimerOrScore.MAX_TEAM_SCORE)
            winner = TeamFaction.Alliance;

        if (GetTeamScore(TeamIds.Horde) == WsgTimerOrScore.MAX_TEAM_SCORE)
            winner = TeamFaction.Horde;

        if (winner != 0)
        {
            UpdateWorldState(WsgWorldStates.FLAG_STATE_ALLIANCE, 1);
            UpdateWorldState(WsgWorldStates.FLAG_STATE_HORDE, 1);
            UpdateWorldState(WsgWorldStates.STATE_TIMER_ACTIVE, 0);

            RewardHonorToTeam(_honor[(int)MHonorMode][(int)WsgRewards.Win], winner);
            EndBattleground(winner);
        }
        else
            _flagsTimer[GetTeamIndexByTeamId(team)] = WsgTimerOrScore.FLAG_RESPAWN_TIME;
    }

    private ObjectGuid GetDroppedFlagGUID(TeamFaction team)
    {
        return _mDroppedFlagGUID[GetTeamIndexByTeamId(team)];
    }

    private WsgFlagState GetFlagState(TeamFaction team)
    {
        return _flagState[GetTeamIndexByTeamId(team)];
    }

    private void HandleFlagRoomCapturePoint(int team)
    {
        var flagCarrier = Global.ObjAccessor.GetPlayer(BgMap, GetFlagPickerGUID(team));
        var areaTrigger = team == TeamIds.Alliance ? 3647 : 3646u;

        if (flagCarrier != null && flagCarrier.IsInAreaTriggerRadius(CliDB.AreaTriggerStorage.LookupByKey(areaTrigger)))
            EventPlayerCapturedFlag(flagCarrier);
    }

    private bool IsAllianceFlagPickedup()
    {
        return !_mFlagKeepers[TeamIds.Alliance].IsEmpty;
    }

    private bool IsHordeFlagPickedup()
    {
        return !_mFlagKeepers[TeamIds.Horde].IsEmpty;
    }

    private void RespawnFlag(TeamFaction team, bool captured)
    {
        if (team == TeamFaction.Alliance)
        {
            Log.Logger.Debug("Respawn Alliance Id");
            _flagState[TeamIds.Alliance] = WsgFlagState.OnBase;
        }
        else
        {
            Log.Logger.Debug("Respawn Horde Id");
            _flagState[TeamIds.Horde] = WsgFlagState.OnBase;
        }

        if (captured)
        {
            //when map_update will be allowed for Battlegrounds this code will be useless
            SpawnBGObject(WsgObjectTypes.H_FLAG, BattlegroundConst.RespawnImmediately);
            SpawnBGObject(WsgObjectTypes.A_FLAG, BattlegroundConst.RespawnImmediately);
            SendBroadcastText(WsgBroadcastTexts.FLAGS_PLACED, ChatMsg.BgSystemNeutral);
            PlaySoundToAll(WsgSound.FLAGS_RESPAWNED); // Id respawned sound...
        }

        _bothFlagsKept = false;
    }

    private void RespawnFlagAfterDrop(TeamFaction team)
    {
        if (Status != BattlegroundStatus.InProgress)
            return;

        RespawnFlag(team, false);

        if (team == TeamFaction.Alliance)
            SpawnBGObject(WsgObjectTypes.A_FLAG, BattlegroundConst.RespawnImmediately);
        else
            SpawnBGObject(WsgObjectTypes.H_FLAG, BattlegroundConst.RespawnImmediately);

        SendBroadcastText(WsgBroadcastTexts.FLAGS_PLACED, ChatMsg.BgSystemNeutral);
        PlaySoundToAll(WsgSound.FLAGS_RESPAWNED);

        var obj = BgMap.GetGameObject(GetDroppedFlagGUID(team));

        if (obj)
            obj.Delete();
        else
            Log.Logger.Error("unknown droped Id ({0})", GetDroppedFlagGUID(team).ToString());

        SetDroppedFlagGUID(ObjectGuid.Empty, GetTeamIndexByTeamId(team));
        _bothFlagsKept = false;
        // Check opposing Id if it is in capture zone; if so, capture it
        HandleFlagRoomCapturePoint(team == TeamFaction.Alliance ? TeamIds.Horde : TeamIds.Alliance);
    }

    private void SetAllianceFlagPicker(ObjectGuid guid)
    {
        _mFlagKeepers[TeamIds.Alliance] = guid;
    }

    private void SetHordeFlagPicker(ObjectGuid guid)
    {
        _mFlagKeepers[TeamIds.Horde] = guid;
    }

    private void SetLastFlagCapture(TeamFaction team)
    {
        _lastFlagCaptureTeam = (uint)team;
    }

    private void UpdateFlagState(TeamFaction team, WsgFlagState value)
    {
        int TransformValueToOtherTeamControlWorldState(WsgFlagState value)
        {
            return value switch
            {
                WsgFlagState.OnBase      => 1,
                WsgFlagState.OnGround    => 1,
                WsgFlagState.WaitRespawn => 1,
                WsgFlagState.OnPlayer    => 2,
                _                        => 0
            };
        }

        ;

        if (team == TeamFaction.Horde)
        {
            UpdateWorldState(WsgWorldStates.FLAG_STATE_ALLIANCE, (int)value);
            UpdateWorldState(WsgWorldStates.FLAG_CONTROL_HORDE, TransformValueToOtherTeamControlWorldState(value));
        }
        else
        {
            UpdateWorldState(WsgWorldStates.FLAG_STATE_HORDE, (int)value);
            UpdateWorldState(WsgWorldStates.FLAG_CONTROL_ALLIANCE, TransformValueToOtherTeamControlWorldState(value));
        }
    }

    private void UpdateTeamScore(int team)
    {
        if (team == TeamIds.Alliance)
            UpdateWorldState(WsgWorldStates.FLAG_CAPTURES_ALLIANCE, (int)GetTeamScore(team));
        else
            UpdateWorldState(WsgWorldStates.FLAG_CAPTURES_HORDE, (int)GetTeamScore(team));
    }
}