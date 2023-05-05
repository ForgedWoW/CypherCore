// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.DataStorage.ClientReader;
using Forged.MapServer.DataStorage.Structs.B;
using Forged.MapServer.DataStorage.Structs.F;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Forged.MapServer.Guilds;
using Forged.MapServer.Miscellaneous;
using Forged.MapServer.Text;
using Forged.MapServer.World;
using Framework.Constants;
using Framework.Database;
using Game.Common;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.BattleGrounds.Zones;

internal class BgEyeofStorm : Battleground
{
    private readonly byte[] _mCurrentPointPlayersCount = new byte[2 * EotSPoints.POINTS_MAX];
    private readonly uint[] _mHonorScoreTics = new uint[2];
    private readonly BattlegroundPointCaptureStatus[] _mLastPointCaptureStatus = new BattlegroundPointCaptureStatus[EotSPoints.POINTS_MAX];
    private readonly List<ObjectGuid>[] _mPlayersNearPoint = new List<ObjectGuid>[EotSPoints.POINTS_MAX + 1];
    private readonly EotSProgressBarConsts[] _mPointBarStatus = new EotSProgressBarConsts[EotSPoints.POINTS_MAX];
    private readonly TeamFaction[] _mPointOwnedByTeam = new TeamFaction[EotSPoints.POINTS_MAX];
    private readonly EotSPointState[] _mPointState = new EotSPointState[EotSPoints.POINTS_MAX];
    private readonly uint[] _mPointsTrigger = new uint[EotSPoints.POINTS_MAX];
    private readonly uint[] _mTeamPointsCount = new uint[2];
    private ObjectGuid _mDroppedFlagGUID;
    private uint _mFlagCapturedBgObjectType;

    private ObjectGuid _mFlagKeeper; // keepers guid

    // type that should be despawned when Id is captured
    private EotSFlagState _mFlagState; // for checking Id state

    private int _mFlagsTimer;
    private uint _mHonorTics;
    private int _mPointAddingTimer;
    private int _mTowerCapCheckTimer;

    public BgEyeofStorm(BattlegroundTemplate battlegroundTemplate, WorldManager worldManager, BattlegroundManager battlegroundManager, ObjectAccessor objectAccessor, GameObjectManager objectManager,
                        CreatureFactory creatureFactory, GameObjectFactory gameObjectFactory, ClassFactory classFactory, IConfiguration configuration, CharacterDatabase characterDatabase,
                        GuildManager guildManager, Formulas formulas, PlayerComputators playerComputators, DB6Storage<FactionRecord> factionStorage, DB6Storage<BroadcastTextRecord> broadcastTextRecords,
                        CreatureTextManager creatureTextManager, WorldStateManager worldStateManager) :
        base(battlegroundTemplate, worldManager, battlegroundManager, objectAccessor, objectManager, creatureFactory, gameObjectFactory, classFactory, configuration, characterDatabase,
             guildManager, formulas, playerComputators, factionStorage, broadcastTextRecords, creatureTextManager, worldStateManager)
    {
        MBuffChange = true;
        BgObjects = new ObjectGuid[EotSObjectTypes.MAX];
        BgCreatures = new ObjectGuid[EotSCreaturesTypes.MAX];
        _mPointsTrigger[EotSPoints.FEL_REAVER] = EotSPointsTrigger.FEL_REAVER_BUFF;
        _mPointsTrigger[EotSPoints.BLOOD_ELF] = EotSPointsTrigger.BLOOD_ELF_BUFF;
        _mPointsTrigger[EotSPoints.DRAENEI_RUINS] = EotSPointsTrigger.DRAENEI_RUINS_BUFF;
        _mPointsTrigger[EotSPoints.MAGE_TOWER] = EotSPointsTrigger.MAGE_TOWER_BUFF;
        _mHonorScoreTics[TeamIds.Alliance] = 0;
        _mHonorScoreTics[TeamIds.Horde] = 0;
        _mTeamPointsCount[TeamIds.Alliance] = 0;
        _mTeamPointsCount[TeamIds.Horde] = 0;
        _mFlagKeeper.Clear();
        _mDroppedFlagGUID.Clear();
        _mFlagCapturedBgObjectType = 0;
        _mFlagState = EotSFlagState.OnBase;
        _mFlagsTimer = 0;
        _mTowerCapCheckTimer = 0;
        _mPointAddingTimer = 0;
        _mHonorTics = 0;

        for (byte i = 0; i < EotSPoints.POINTS_MAX; ++i)
        {
            _mPointOwnedByTeam[i] = TeamFaction.Other;
            _mPointState[i] = EotSPointState.Uncontrolled;
            _mPointBarStatus[i] = EotSProgressBarConsts.ProgressBarStateMiddle;
            _mLastPointCaptureStatus[i] = BattlegroundPointCaptureStatus.Neutral;
        }

        for (byte i = 0; i < EotSPoints.POINTS_MAX + 1; ++i)
            _mPlayersNearPoint[i] = new List<ObjectGuid>();

        for (byte i = 0; i < 2 * EotSPoints.POINTS_MAX; ++i)
            _mCurrentPointPlayersCount[i] = 0;
    }

    public override void AddPlayer(Player player)
    {
        var isInBattleground = IsPlayerInBattleground(player.GUID);
        base.AddPlayer(player);

        if (!isInBattleground)
            PlayerScores[player.GUID] = new BgEyeOfStormScore(player.GUID, player.GetBgTeam());

        _mPlayersNearPoint[EotSPoints.POINTS_MAX].Add(player.GUID);
    }

    public override void EndBattleground(TeamFaction winner)
    {
        // Win reward
        if (winner == TeamFaction.Alliance)
            RewardHonorToTeam(GetBonusHonorFromKill(1), TeamFaction.Alliance);

        if (winner == TeamFaction.Horde)
            RewardHonorToTeam(GetBonusHonorFromKill(1), TeamFaction.Horde);

        // Complete map reward
        RewardHonorToTeam(GetBonusHonorFromKill(1), TeamFaction.Alliance);
        RewardHonorToTeam(GetBonusHonorFromKill(1), TeamFaction.Horde);

        base.EndBattleground(winner);
    }

    public override void EventPlayerClickedOnFlag(Player player, GameObject targetObj)
    {
        if (Status != BattlegroundStatus.InProgress || IsFlagPickedup() || !player.Location.IsWithinDistInMap(targetObj, 10))
            return;

        if (GetPlayerTeam(player.GUID) == TeamFaction.Alliance)
        {
            UpdateWorldState(EotSWorldStateIds.NETHERSTORM_FLAG_STATE_ALLIANCE, (int)EotSFlagState.OnPlayer);
            PlaySoundToAll(EotSSoundIds.FLAG_PICKED_UP_ALLIANCE);
        }
        else
        {
            UpdateWorldState(EotSWorldStateIds.NETHERSTORM_FLAG_STATE_HORDE, (int)EotSFlagState.OnPlayer);
            PlaySoundToAll(EotSSoundIds.FLAG_PICKED_UP_HORDE);
        }

        if (_mFlagState == EotSFlagState.OnBase)
            UpdateWorldState(EotSWorldStateIds.NETHERSTORM_FLAG, 0);

        _mFlagState = EotSFlagState.OnPlayer;

        SpawnBGObject(EotSObjectTypes.FLAG_NETHERSTORM, BattlegroundConst.RESPAWN_ONE_DAY);
        SetFlagPicker(player.GUID);
        //get Id aura on player
        player.SpellFactory.CastSpell(player, EotSMisc.SPELL_NETHERSTORM_FLAG, true);
        player.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.PvPActive);

        SendBroadcastText(EotSBroadcastTexts.TAKEN_FLAG, GetPlayerTeam(player.GUID) == TeamFaction.Alliance ? ChatMsg.BgSystemAlliance : ChatMsg.BgSystemHorde, player);
    }

    public override void EventPlayerDroppedFlag(Player player)
    {
        if (Status != BattlegroundStatus.InProgress)
        {
            // if not running, do not cast things at the dropper player, neither send unnecessary messages
            // just take off the aura
            if (IsFlagPickedup() && GetFlagPickerGUID() == player.GUID)
            {
                SetFlagPicker(ObjectGuid.Empty);
                player.RemoveAura(EotSMisc.SPELL_NETHERSTORM_FLAG);
            }

            return;
        }

        if (!IsFlagPickedup())
            return;

        if (GetFlagPickerGUID() != player.GUID)
            return;

        SetFlagPicker(ObjectGuid.Empty);
        player.RemoveAura(EotSMisc.SPELL_NETHERSTORM_FLAG);
        _mFlagState = EotSFlagState.OnGround;
        _mFlagsTimer = EotSMisc.FLAG_RESPAWN_TIME;
        player.SpellFactory.CastSpell(player, BattlegroundConst.SPELL_RECENTLY_DROPPED_FLAG, true);
        player.SpellFactory.CastSpell(player, EotSMisc.SPELL_PLAYER_DROPPED_FLAG, true);
        //this does not work correctly :((it should remove Id carrier name)
        UpdateWorldState(EotSWorldStateIds.NETHERSTORM_FLAG_STATE_HORDE, (int)EotSFlagState.WaitRespawn);
        UpdateWorldState(EotSWorldStateIds.NETHERSTORM_FLAG_STATE_ALLIANCE, (int)EotSFlagState.WaitRespawn);

        SendBroadcastText(EotSBroadcastTexts.FLAG_DROPPED, GetPlayerTeam(player.GUID) == TeamFaction.Alliance ? ChatMsg.BgSystemAlliance : ChatMsg.BgSystemHorde);
    }

    public override WorldSafeLocsEntry GetClosestGraveYard(Player player)
    {
        uint gID;
        var team = GetPlayerTeam(player.GUID);

        switch (team)
        {
            case TeamFaction.Alliance:
                gID = EotSGaveyardIds.MAIN_ALLIANCE;

                break;

            case TeamFaction.Horde:
                gID = EotSGaveyardIds.MAIN_HORDE;

                break;

            default: return null;
        }

        var entry = ObjectManager.GetWorldSafeLoc(gID);
        var nearestEntry = entry;

        if (entry == null)
        {
            Log.Logger.Error("BattlegroundEY: The main team graveyard could not be found. The graveyard system will not be operational!");

            return null;
        }

        var plrX = player.Location.X;
        var plrY = player.Location.Y;
        var plrZ = player.Location.Z;

        var distance = (entry.Location.X - plrX) * (entry.Location.X - plrX) + (entry.Location.Y - plrY) * (entry.Location.Y - plrY) + (entry.Location.Z - plrZ) * (entry.Location.Z - plrZ);
        var nearestDistance = distance;

        for (byte i = 0; i < EotSPoints.POINTS_MAX; ++i)
            if (_mPointOwnedByTeam[i] == team && _mPointState[i] == EotSPointState.UnderControl)
            {
                entry = ObjectManager.GetWorldSafeLoc(EotSMisc.MCapturingPointTypes[i].GraveYardId);

                if (entry == null)
                    Log.Logger.Error("BattlegroundEY: Graveyard {0} could not be found.", EotSMisc.MCapturingPointTypes[i].GraveYardId);
                else
                {
                    distance = (entry.Location.X - plrX) * (entry.Location.X - plrX) + (entry.Location.Y - plrY) * (entry.Location.Y - plrY) + (entry.Location.Z - plrZ) * (entry.Location.Z - plrZ);

                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestEntry = entry;
                    }
                }
            }

        return nearestEntry;
    }

    public override WorldSafeLocsEntry GetExploitTeleportLocation(TeamFaction team)
    {
        return ObjectManager.GetWorldSafeLoc(team == TeamFaction.Alliance ? EotSMisc.EXPLOIT_TELEPORT_LOCATION_ALLIANCE : EotSMisc.EXPLOIT_TELEPORT_LOCATION_HORDE);
    }

    public override ObjectGuid GetFlagPickerGUID(int team = -1)
    {
        return _mFlagKeeper;
    }

    public override TeamFaction GetPrematureWinner()
    {
        if (GetTeamScore(TeamIds.Alliance) > GetTeamScore(TeamIds.Horde))
            return TeamFaction.Alliance;

        if (GetTeamScore(TeamIds.Horde) > GetTeamScore(TeamIds.Alliance))
            return TeamFaction.Horde;

        return base.GetPrematureWinner();
    }

    public override void HandleAreaTrigger(Player player, uint trigger, bool entered)
    {
        if (!player.IsAlive) //hack code, must be removed later
            return;

        switch (trigger)
        {
            case 4530: // Horde Start
            case 4531: // Alliance Start
                if (Status == BattlegroundStatus.WaitJoin && !entered)
                    TeleportPlayerToExploitLocation(player);

                break;

            case EotSPointsTrigger.BLOOD_ELF_POINT:
                if (_mPointState[EotSPoints.BLOOD_ELF] == EotSPointState.UnderControl && _mPointOwnedByTeam[EotSPoints.BLOOD_ELF] == GetPlayerTeam(player.GUID))
                    if (_mFlagState != 0 && GetFlagPickerGUID() == player.GUID)
                        EventPlayerCapturedFlag(player, EotSObjectTypes.FLAG_BLOOD_ELF);

                break;

            case EotSPointsTrigger.FEL_REAVER_POINT:
                if (_mPointState[EotSPoints.FEL_REAVER] == EotSPointState.UnderControl && _mPointOwnedByTeam[EotSPoints.FEL_REAVER] == GetPlayerTeam(player.GUID))
                    if (_mFlagState != 0 && GetFlagPickerGUID() == player.GUID)
                        EventPlayerCapturedFlag(player, EotSObjectTypes.FLAG_FEL_REAVER);

                break;

            case EotSPointsTrigger.MAGE_TOWER_POINT:
                if (_mPointState[EotSPoints.MAGE_TOWER] == EotSPointState.UnderControl && _mPointOwnedByTeam[EotSPoints.MAGE_TOWER] == GetPlayerTeam(player.GUID))
                    if (_mFlagState != 0 && GetFlagPickerGUID() == player.GUID)
                        EventPlayerCapturedFlag(player, EotSObjectTypes.FLAG_MAGE_TOWER);

                break;

            case EotSPointsTrigger.DRAENEI_RUINS_POINT:
                if (_mPointState[EotSPoints.DRAENEI_RUINS] == EotSPointState.UnderControl && _mPointOwnedByTeam[EotSPoints.DRAENEI_RUINS] == GetPlayerTeam(player.GUID))
                    if (_mFlagState != 0 && GetFlagPickerGUID() == player.GUID)
                        EventPlayerCapturedFlag(player, EotSObjectTypes.FLAG_DRAENEI_RUINS);

                break;

            case 4512:
            case 4515:
            case 4517:
            case 4519:
            case 4568:
            case 4569:
            case 4570:
            case 4571:
            case 5866:
                break;

            default:
                base.HandleAreaTrigger(player, trigger, entered);

                break;
        }
    }

    public override void HandleKillPlayer(Player player, Player killer)
    {
        if (Status != BattlegroundStatus.InProgress)
            return;

        base.HandleKillPlayer(player, killer);
        EventPlayerDroppedFlag(player);
    }

    public override void PostUpdateImpl(uint diff)
    {
        if (Status == BattlegroundStatus.InProgress)
        {
            _mPointAddingTimer -= (int)diff;

            if (_mPointAddingTimer <= 0)
            {
                _mPointAddingTimer = EotSMisc.F_POINTS_TICK_TIME;

                if (_mTeamPointsCount[TeamIds.Alliance] > 0)
                    AddPoints(TeamFaction.Alliance, EotSMisc.TickPoints[_mTeamPointsCount[TeamIds.Alliance] - 1]);

                if (_mTeamPointsCount[TeamIds.Horde] > 0)
                    AddPoints(TeamFaction.Horde, EotSMisc.TickPoints[_mTeamPointsCount[TeamIds.Horde] - 1]);
            }

            if (_mFlagState is EotSFlagState.WaitRespawn or EotSFlagState.OnGround)
            {
                _mFlagsTimer -= (int)diff;

                if (_mFlagsTimer < 0)
                {
                    _mFlagsTimer = 0;

                    if (_mFlagState == EotSFlagState.WaitRespawn)
                        RespawnFlag(true);
                    else
                        RespawnFlagAfterDrop();
                }
            }

            _mTowerCapCheckTimer -= (int)diff;

            if (_mTowerCapCheckTimer <= 0)
            {
                //check if player joined point
                /*I used this order of calls, because although we will check if one player is in gameobject's distance 2 times
                but we can count of players on current point in CheckSomeoneLeftPoint
                */
                CheckSomeoneJoinedPoint();
                //check if player left point
                CheckSomeoneLeftPo();
                UpdatePointStatuses();
                _mTowerCapCheckTimer = EotSMisc.F_POINTS_TICK_TIME;
            }
        }
    }

    public override void RemovePlayer(Player player, ObjectGuid guid, TeamFaction team)
    {
        // sometimes Id aura not removed :(
        for (var j = EotSPoints.POINTS_MAX; j >= 0; --j)
        {
            for (var i = 0; i < _mPlayersNearPoint[j].Count; ++i)
                if (_mPlayersNearPoint[j][i] == guid)
                    _mPlayersNearPoint[j].RemoveAt(i);
        }

        if (!IsFlagPickedup())
            return;

        if (_mFlagKeeper != guid)
            return;

        if (player != null)
            EventPlayerDroppedFlag(player);
        else
        {
            SetFlagPicker(ObjectGuid.Empty);
            RespawnFlag(true);
        }
    }

    public override void Reset()
    {
        //call parent's class reset
        base.Reset();

        MTeamScores[TeamIds.Alliance] = 0;
        MTeamScores[TeamIds.Horde] = 0;
        _mTeamPointsCount[TeamIds.Alliance] = 0;
        _mTeamPointsCount[TeamIds.Horde] = 0;
        _mHonorScoreTics[TeamIds.Alliance] = 0;
        _mHonorScoreTics[TeamIds.Horde] = 0;
        _mFlagState = EotSFlagState.OnBase;
        _mFlagCapturedBgObjectType = 0;
        _mFlagKeeper.Clear();
        _mDroppedFlagGUID.Clear();
        _mPointAddingTimer = 0;
        _mTowerCapCheckTimer = 0;
        var isBGWeekend = BattlegroundManager.IsBGWeekend(GetTypeID());
        _mHonorTics = isBGWeekend ? EotSMisc.EY_WEEKEND_HONOR_TICKS : EotSMisc.NOT_EY_WEEKEND_HONOR_TICKS;

        for (byte i = 0; i < EotSPoints.POINTS_MAX; ++i)
        {
            _mPointOwnedByTeam[i] = TeamFaction.Other;
            _mPointState[i] = EotSPointState.Uncontrolled;
            _mPointBarStatus[i] = EotSProgressBarConsts.ProgressBarStateMiddle;
            _mPlayersNearPoint[i].Clear();
        }

        _mPlayersNearPoint[EotSPoints.PLAYERS_OUT_OF_POINTS].Clear();
    }

    public override void SetDroppedFlagGUID(ObjectGuid guid, int teamID = -1)
    {
        _mDroppedFlagGUID = guid;
    }

    public override bool SetupBattleground()
    {
        // doors
        if (!AddObject(EotSObjectTypes.DOOR_A, EotSObjectIds.A_DOOR_EY_ENTRY, 2527.59716796875f, 1596.90625f, 1238.4544677734375f, 3.159139871597290039f, 0.173641681671142578f, 0.001514434814453125f, -0.98476982116699218f, 0.008638577535748481f) ||
            !AddObject(EotSObjectTypes.DOOR_H, EotSObjectIds.H_DOOR_EY_ENTRY, 1803.2066650390625f, 1539.486083984375f, 1238.4544677734375f, 3.13898324966430664f, 0.173647880554199218f, 0.0f, 0.984807014465332031f, 0.001244877814315259f)
            // banners (alliance)
            ||
            !AddObject(EotSObjectTypes.A_BANNER_FEL_REAVER_CENTER, EotSObjectIds.A_BANNER_EY_ENTRY, 2057.47265625f, 1735.109130859375f, 1188.065673828125f, 5.305802345275878906f, 0.0f, 0.0f, -0.46947097778320312f, 0.882947921752929687f, BattlegroundConst.RESPAWN_ONE_DAY) ||
            !AddObject(EotSObjectTypes.A_BANNER_FEL_REAVER_LEFT, EotSObjectIds.A_BANNER_EY_ENTRY, 2032.248291015625f, 1729.546875f, 1191.2296142578125f, 1.797688722610473632f, 0.0f, 0.0f, 0.7826080322265625f, 0.622514784336090087f, BattlegroundConst.RESPAWN_ONE_DAY) ||
            !AddObject(EotSObjectTypes.A_BANNER_FEL_REAVER_RIGHT, EotSObjectIds.A_BANNER_EY_ENTRY, 2092.338623046875f, 1775.4739990234375f, 1187.504150390625f, 5.811946868896484375f, 0.0f, 0.0f, -0.2334451675415039f, 0.972369968891143798f, BattlegroundConst.RESPAWN_ONE_DAY) ||
            !AddObject(EotSObjectTypes.A_BANNER_BLOOD_ELF_CENTER, EotSObjectIds.A_BANNER_EY_ENTRY, 2047.1910400390625f, 1349.1927490234375f, 1189.0032958984375f, 4.660029888153076171f, 0.0f, 0.0f, -0.72537422180175781f, 0.688354730606079101f, BattlegroundConst.RESPAWN_ONE_DAY) ||
            !AddObject(EotSObjectTypes.A_BANNER_BLOOD_ELF_LEFT, EotSObjectIds.A_BANNER_EY_ENTRY, 2074.319580078125f, 1385.779541015625f, 1194.7203369140625f, 0.488691210746765136f, 0.0f, 0.0f, 0.241921424865722656f, 0.970295846462249755f, BattlegroundConst.RESPAWN_ONE_DAY) ||
            !AddObject(EotSObjectTypes.A_BANNER_BLOOD_ELF_RIGHT, EotSObjectIds.A_BANNER_EY_ENTRY, 2025.125f, 1386.123291015625f, 1192.7354736328125f, 2.391098499298095703f, 0.0f, 0.0f, 0.930417060852050781f, 0.366502493619918823f, BattlegroundConst.RESPAWN_ONE_DAY) ||
            !AddObject(EotSObjectTypes.A_BANNER_DRAENEI_RUINS_CENTER, EotSObjectIds.A_BANNER_EY_ENTRY, 2276.796875f, 1400.407958984375f, 1196.333740234375f, 2.44346022605895996f, 0.0f, 0.0f, 0.939692497253417968f, 0.34202045202255249f, BattlegroundConst.RESPAWN_ONE_DAY) ||
            !AddObject(EotSObjectTypes.A_BANNER_DRAENEI_RUINS_LEFT, EotSObjectIds.A_BANNER_EY_ENTRY, 2305.776123046875f, 1404.5572509765625f, 1199.384765625f, 1.745326757431030273f, 0.0f, 0.0f, 0.766043663024902343f, 0.642788589000701904f, BattlegroundConst.RESPAWN_ONE_DAY) ||
            !AddObject(EotSObjectTypes.A_BANNER_DRAENEI_RUINS_RIGHT, EotSObjectIds.A_BANNER_EY_ENTRY, 2245.395751953125f, 1366.4132080078125f, 1195.27880859375f, 2.216565132141113281f, 0.0f, 0.0f, 0.894933700561523437f, 0.44619917869567871f, BattlegroundConst.RESPAWN_ONE_DAY) ||
            !AddObject(EotSObjectTypes.A_BANNER_MAGE_TOWER_CENTER, EotSObjectIds.A_BANNER_EY_ENTRY, 2270.8359375f, 1784.080322265625f, 1186.757080078125f, 2.426007747650146484f, 0.0f, 0.0f, 0.936672210693359375f, 0.350207358598709106f, BattlegroundConst.RESPAWN_ONE_DAY) ||
            !AddObject(EotSObjectTypes.A_BANNER_MAGE_TOWER_LEFT, EotSObjectIds.A_BANNER_EY_ENTRY, 2269.126708984375f, 1737.703125f, 1186.8145751953125f, 0.994837164878845214f, 0.0f, 0.0f, 0.477158546447753906f, 0.878817260265350341f, BattlegroundConst.RESPAWN_ONE_DAY) ||
            !AddObject(EotSObjectTypes.A_BANNER_MAGE_TOWER_RIGHT, EotSObjectIds.A_BANNER_EY_ENTRY, 2300.85595703125f, 1741.24658203125f, 1187.793212890625f, 5.497788906097412109f, 0.0f, 0.0f, -0.38268280029296875f, 0.923879802227020263f, BattlegroundConst.RESPAWN_ONE_DAY)
            // banners (horde)
            ||
            !AddObject(EotSObjectTypes.H_BANNER_FEL_REAVER_CENTER, EotSObjectIds.H_BANNER_EY_ENTRY, 2057.45654296875f, 1735.07470703125f, 1187.9063720703125f, 5.35816192626953125f, 0.0f, 0.0f, -0.446197509765625f, 0.894934535026550292f, BattlegroundConst.RESPAWN_ONE_DAY) ||
            !AddObject(EotSObjectTypes.H_BANNER_FEL_REAVER_LEFT, EotSObjectIds.H_BANNER_EY_ENTRY, 2032.251708984375f, 1729.532958984375f, 1190.3251953125f, 1.867502212524414062f, 0.0f, 0.0f, 0.803856849670410156f, 0.594822824001312255f, BattlegroundConst.RESPAWN_ONE_DAY) ||
            !AddObject(EotSObjectTypes.H_BANNER_FEL_REAVER_RIGHT, EotSObjectIds.H_BANNER_EY_ENTRY, 2092.354248046875f, 1775.4583740234375f, 1187.079345703125f, 5.881760597229003906f, 0.0f, 0.0f, -0.19936752319335937f, 0.979924798011779785f, BattlegroundConst.RESPAWN_ONE_DAY) ||
            !AddObject(EotSObjectTypes.H_BANNER_BLOOD_ELF_CENTER, EotSObjectIds.H_BANNER_EY_ENTRY, 2047.1978759765625f, 1349.1875f, 1188.5650634765625f, 4.625123500823974609f, 0.0f, 0.0f, -0.73727703094482421f, 0.67559051513671875f, BattlegroundConst.RESPAWN_ONE_DAY) ||
            !AddObject(EotSObjectTypes.H_BANNER_BLOOD_ELF_LEFT, EotSObjectIds.H_BANNER_EY_ENTRY, 2074.3056640625f, 1385.7725830078125f, 1194.4686279296875f, 0.471238493919372558f, 0.0f, 0.0f, 0.233445167541503906f, 0.972369968891143798f, BattlegroundConst.RESPAWN_ONE_DAY) ||
            !AddObject(EotSObjectTypes.H_BANNER_BLOOD_ELF_RIGHT, EotSObjectIds.H_BANNER_EY_ENTRY, 2025.09375f, 1386.12158203125f, 1192.6536865234375f, 2.373644113540649414f, 0.0f, 0.0f, 0.927183151245117187f, 0.37460830807685852f, BattlegroundConst.RESPAWN_ONE_DAY) ||
            !AddObject(EotSObjectTypes.H_BANNER_DRAENEI_RUINS_CENTER, EotSObjectIds.H_BANNER_EY_ENTRY, 2276.798583984375f, 1400.4410400390625f, 1196.2200927734375f, 2.495818138122558593f, 0.0f, 0.0f, 0.948323249816894531f, 0.317305892705917358f, BattlegroundConst.RESPAWN_ONE_DAY) ||
            !AddObject(EotSObjectTypes.H_BANNER_DRAENEI_RUINS_LEFT, EotSObjectIds.H_BANNER_EY_ENTRY, 2305.763916015625f, 1404.5972900390625f, 1199.3333740234375f, 1.640606880187988281f, 0.0f, 0.0f, 0.731352806091308593f, 0.6819993257522583f, BattlegroundConst.RESPAWN_ONE_DAY) ||
            !AddObject(EotSObjectTypes.H_BANNER_DRAENEI_RUINS_RIGHT, EotSObjectIds.H_BANNER_EY_ENTRY, 2245.382080078125f, 1366.454833984375f, 1195.1815185546875f, 2.373644113540649414f, 0.0f, 0.0f, 0.927183151245117187f, 0.37460830807685852f, BattlegroundConst.RESPAWN_ONE_DAY) ||
            !AddObject(EotSObjectTypes.H_BANNER_MAGE_TOWER_CENTER, EotSObjectIds.H_BANNER_EY_ENTRY, 2270.869873046875f, 1784.0989990234375f, 1186.4384765625f, 2.356194972991943359f, 0.0f, 0.0f, 0.923879623413085937f, 0.382683247327804565f, BattlegroundConst.RESPAWN_ONE_DAY) ||
            !AddObject(EotSObjectTypes.H_BANNER_MAGE_TOWER_LEFT, EotSObjectIds.H_BANNER_EY_ENTRY, 2268.59716796875f, 1737.0191650390625f, 1186.75390625f, 0.942476630210876464f, 0.0f, 0.0f, 0.453989982604980468f, 0.891006767749786376f, BattlegroundConst.RESPAWN_ONE_DAY) ||
            !AddObject(EotSObjectTypes.H_BANNER_MAGE_TOWER_RIGHT, EotSObjectIds.H_BANNER_EY_ENTRY, 2301.01904296875f, 1741.4930419921875f, 1187.48974609375f, 5.375615119934082031f, 0.0f, 0.0f, -0.4383707046508789f, 0.898794233798980712f, BattlegroundConst.RESPAWN_ONE_DAY)
            // banners (natural)
            ||
            !AddObject(EotSObjectTypes.N_BANNER_FEL_REAVER_CENTER, EotSObjectIds.N_BANNER_EY_ENTRY, 2057.4931640625f, 1735.111083984375f, 1187.675537109375f, 5.340708732604980468f, 0.0f, 0.0f, -0.45398998260498046f, 0.891006767749786376f, BattlegroundConst.RESPAWN_ONE_DAY) ||
            !AddObject(EotSObjectTypes.N_BANNER_FEL_REAVER_LEFT, EotSObjectIds.N_BANNER_EY_ENTRY, 2032.2569580078125f, 1729.5572509765625f, 1191.0802001953125f, 1.797688722610473632f, 0.0f, 0.0f, 0.7826080322265625f, 0.622514784336090087f, BattlegroundConst.RESPAWN_ONE_DAY) ||
            !AddObject(EotSObjectTypes.N_BANNER_FEL_REAVER_RIGHT, EotSObjectIds.N_BANNER_EY_ENTRY, 2092.395751953125f, 1775.451416015625f, 1186.965576171875f, 5.89921426773071289f, 0.0f, 0.0f, -0.19080829620361328f, 0.981627285480499267f, BattlegroundConst.RESPAWN_ONE_DAY) ||
            !AddObject(EotSObjectTypes.N_BANNER_BLOOD_ELF_CENTER, EotSObjectIds.N_BANNER_EY_ENTRY, 2047.1875f, 1349.1944580078125f, 1188.5731201171875f, 4.642575740814208984f, 0.0f, 0.0f, -0.731353759765625f, 0.681998312473297119f, BattlegroundConst.RESPAWN_ONE_DAY) ||
            !AddObject(EotSObjectTypes.N_BANNER_BLOOD_ELF_LEFT, EotSObjectIds.N_BANNER_EY_ENTRY, 2074.3212890625f, 1385.76220703125f, 1194.362060546875f, 0.488691210746765136f, 0.0f, 0.0f, 0.241921424865722656f, 0.970295846462249755f, BattlegroundConst.RESPAWN_ONE_DAY) ||
            !AddObject(EotSObjectTypes.N_BANNER_BLOOD_ELF_RIGHT, EotSObjectIds.N_BANNER_EY_ENTRY, 2025.13720703125f, 1386.1336669921875f, 1192.5482177734375f, 2.391098499298095703f, 0.0f, 0.0f, 0.930417060852050781f, 0.366502493619918823f, BattlegroundConst.RESPAWN_ONE_DAY) ||
            !AddObject(EotSObjectTypes.N_BANNER_DRAENEI_RUINS_CENTER, EotSObjectIds.N_BANNER_EY_ENTRY, 2276.833251953125f, 1400.4375f, 1196.146728515625f, 2.478367090225219726f, 0.0f, 0.0f, 0.94551849365234375f, 0.325568377971649169f, BattlegroundConst.RESPAWN_ONE_DAY) ||
            !AddObject(EotSObjectTypes.N_BANNER_DRAENEI_RUINS_LEFT, EotSObjectIds.N_BANNER_EY_ENTRY, 2305.77783203125f, 1404.5364990234375f, 1199.246337890625f, 1.570795774459838867f, 0.0f, 0.0f, 0.707106590270996093f, 0.707106947898864746f, BattlegroundConst.RESPAWN_ONE_DAY) ||
            !AddObject(EotSObjectTypes.N_BANNER_DRAENEI_RUINS_RIGHT, EotSObjectIds.N_BANNER_EY_ENTRY, 2245.40966796875f, 1366.4410400390625f, 1195.1107177734375f, 2.356194972991943359f, 0.0f, 0.0f, 0.923879623413085937f, 0.382683247327804565f, BattlegroundConst.RESPAWN_ONE_DAY) ||
            !AddObject(EotSObjectTypes.N_BANNER_MAGE_TOWER_CENTER, EotSObjectIds.N_BANNER_EY_ENTRY, 2270.84033203125f, 1784.1197509765625f, 1186.1473388671875f, 2.303830623626708984f, 0.0f, 0.0f, 0.913544654846191406f, 0.406738430261611938f, BattlegroundConst.RESPAWN_ONE_DAY) ||
            !AddObject(EotSObjectTypes.N_BANNER_MAGE_TOWER_LEFT, EotSObjectIds.N_BANNER_EY_ENTRY, 2268.46533203125f, 1736.8385009765625f, 1186.742919921875f, 0.942476630210876464f, 0.0f, 0.0f, 0.453989982604980468f, 0.891006767749786376f, BattlegroundConst.RESPAWN_ONE_DAY) ||
            !AddObject(EotSObjectTypes.N_BANNER_MAGE_TOWER_RIGHT, EotSObjectIds.N_BANNER_EY_ENTRY, 2300.9931640625f, 1741.5504150390625f, 1187.10693359375f, 5.375615119934082031f, 0.0f, 0.0f, -0.4383707046508789f, 0.898794233798980712f, BattlegroundConst.RESPAWN_ONE_DAY)
            // flags
            ||
            !AddObject(EotSObjectTypes.FLAG_NETHERSTORM, EotSObjectIds.FLAG2_EY_ENTRY, 2174.444580078125f, 1569.421875f, 1159.852783203125f, 4.625123500823974609f, 0.0f, 0.0f, -0.73727703094482421f, 0.67559051513671875f, BattlegroundConst.RESPAWN_ONE_DAY) ||
            !AddObject(EotSObjectTypes.FLAG_FEL_REAVER, EotSObjectIds.FLAG1_EY_ENTRY, 2044.28f, 1729.68f, 1189.96f, -0.017453f, 0, 0, 0.008727f, -0.999962f, BattlegroundConst.RESPAWN_ONE_DAY) ||
            !AddObject(EotSObjectTypes.FLAG_BLOOD_ELF, EotSObjectIds.FLAG1_EY_ENTRY, 2048.83f, 1393.65f, 1194.49f, 0.20944f, 0, 0, 0.104528f, 0.994522f, BattlegroundConst.RESPAWN_ONE_DAY) ||
            !AddObject(EotSObjectTypes.FLAG_DRAENEI_RUINS, EotSObjectIds.FLAG1_EY_ENTRY, 2286.56f, 1402.36f, 1197.11f, 3.72381f, 0, 0, 0.957926f, -0.287016f, BattlegroundConst.RESPAWN_ONE_DAY) ||
            !AddObject(EotSObjectTypes.FLAG_MAGE_TOWER, EotSObjectIds.FLAG1_EY_ENTRY, 2284.48f, 1731.23f, 1189.99f, 2.89725f, 0, 0, 0.992546f, 0.121869f, BattlegroundConst.RESPAWN_ONE_DAY)
            // tower cap
            ||
            !AddObject(EotSObjectTypes.TOWER_CAP_FEL_REAVER, EotSObjectIds.FR_TOWER_CAP_EY_ENTRY, 2024.600708f, 1742.819580f, 1195.157715f, 2.443461f, 0, 0, 0.939693f, 0.342020f, BattlegroundConst.RESPAWN_ONE_DAY) ||
            !AddObject(EotSObjectTypes.TOWER_CAP_BLOOD_ELF, EotSObjectIds.BE_TOWER_CAP_EY_ENTRY, 2050.493164f, 1372.235962f, 1194.563477f, 1.710423f, 0, 0, 0.754710f, 0.656059f, BattlegroundConst.RESPAWN_ONE_DAY) ||
            !AddObject(EotSObjectTypes.TOWER_CAP_DRAENEI_RUINS, EotSObjectIds.DR_TOWER_CAP_EY_ENTRY, 2301.010498f, 1386.931641f, 1197.183472f, 1.570796f, 0, 0, 0.707107f, 0.707107f, BattlegroundConst.RESPAWN_ONE_DAY) ||
            !AddObject(EotSObjectTypes.TOWER_CAP_MAGE_TOWER, EotSObjectIds.HU_TOWER_CAP_EY_ENTRY, 2282.121582f, 1760.006958f, 1189.707153f, 1.919862f, 0, 0, 0.819152f, 0.573576f, BattlegroundConst.RESPAWN_ONE_DAY)
            // buffs
            ||
            !AddObject(EotSObjectTypes.SPEEDBUFF_FEL_REAVER, EotSObjectIds.SPEED_BUFF_FEL_REAVER_EY_ENTRY, 2046.462646484375f, 1749.1666259765625f, 1190.010498046875f, 5.410521507263183593f, 0.0f, 0.0f, -0.42261791229248046f, 0.906307935714721679f, BattlegroundConst.RESPAWN_ONE_DAY) ||
            !AddObject(EotSObjectTypes.REGENBUFF_FEL_REAVER, EotSObjectIds.RESTORATION_BUFF_FEL_REAVER_EY_ENTRY, 2046.462646484375f, 1749.1666259765625f, 1190.010498046875f, 5.410521507263183593f, 0.0f, 0.0f, -0.42261791229248046f, 0.906307935714721679f, BattlegroundConst.RESPAWN_ONE_DAY) ||
            !AddObject(EotSObjectTypes.BERSERKBUFF_FEL_REAVER, EotSObjectIds.BERSERK_BUFF_FEL_REAVER_EY_ENTRY, 2046.462646484375f, 1749.1666259765625f, 1190.010498046875f, 5.410521507263183593f, 0.0f, 0.0f, -0.42261791229248046f, 0.906307935714721679f, BattlegroundConst.RESPAWN_ONE_DAY) ||
            !AddObject(EotSObjectTypes.SPEEDBUFF_BLOOD_ELF, EotSObjectIds.SPEED_BUFF_BLOOD_ELF_EY_ENTRY, 2050.46826171875f, 1372.2020263671875f, 1194.5634765625f, 1.675513744354248046f, 0.0f, 0.0f, 0.743144035339355468f, 0.669131457805633544f, BattlegroundConst.RESPAWN_ONE_DAY) ||
            !AddObject(EotSObjectTypes.REGENBUFF_BLOOD_ELF, EotSObjectIds.RESTORATION_BUFF_BLOOD_ELF_EY_ENTRY, 2050.46826171875f, 1372.2020263671875f, 1194.5634765625f, 1.675513744354248046f, 0.0f, 0.0f, 0.743144035339355468f, 0.669131457805633544f, BattlegroundConst.RESPAWN_ONE_DAY) ||
            !AddObject(EotSObjectTypes.BERSERKBUFF_BLOOD_ELF, EotSObjectIds.BERSERK_BUFF_BLOOD_ELF_EY_ENTRY, 2050.46826171875f, 1372.2020263671875f, 1194.5634765625f, 1.675513744354248046f, 0.0f, 0.0f, 0.743144035339355468f, 0.669131457805633544f, BattlegroundConst.RESPAWN_ONE_DAY) ||
            !AddObject(EotSObjectTypes.SPEEDBUFF_DRAENEI_RUINS, EotSObjectIds.SPEED_BUFF_DRAENEI_RUINS_EY_ENTRY, 2302.4765625f, 1391.244873046875f, 1197.7364501953125f, 1.762782454490661621f, 0.0f, 0.0f, 0.771624565124511718f, 0.636078238487243652f, BattlegroundConst.RESPAWN_ONE_DAY) ||
            !AddObject(EotSObjectTypes.REGENBUFF_DRAENEI_RUINS, EotSObjectIds.RESTORATION_BUFF_DRAENEI_RUINS_EY_ENTRY, 2302.4765625f, 1391.244873046875f, 1197.7364501953125f, 1.762782454490661621f, 0.0f, 0.0f, 0.771624565124511718f, 0.636078238487243652f, BattlegroundConst.RESPAWN_ONE_DAY) ||
            !AddObject(EotSObjectTypes.BERSERKBUFF_DRAENEI_RUINS, EotSObjectIds.BERSERK_BUFF_DRAENEI_RUINS_EY_ENTRY, 2302.4765625f, 1391.244873046875f, 1197.7364501953125f, 1.762782454490661621f, 0.0f, 0.0f, 0.771624565124511718f, 0.636078238487243652f, BattlegroundConst.RESPAWN_ONE_DAY) ||
            !AddObject(EotSObjectTypes.SPEEDBUFF_MAGE_TOWER, EotSObjectIds.SPEED_BUFF_MAGE_TOWER_EY_ENTRY, 2283.7099609375f, 1748.8699951171875f, 1189.7071533203125f, 4.782202720642089843f, 0.0f, 0.0f, -0.68199825286865234f, 0.731353819370269775f, BattlegroundConst.RESPAWN_ONE_DAY) ||
            !AddObject(EotSObjectTypes.REGENBUFF_MAGE_TOWER, EotSObjectIds.RESTORATION_BUFF_MAGE_TOWER_EY_ENTRY, 2283.7099609375f, 1748.8699951171875f, 1189.7071533203125f, 4.782202720642089843f, 0.0f, 0.0f, -0.68199825286865234f, 0.731353819370269775f, BattlegroundConst.RESPAWN_ONE_DAY) ||
            !AddObject(EotSObjectTypes.BERSERKBUFF_MAGE_TOWER, EotSObjectIds.BERSERK_BUFF_MAGE_TOWER_EY_ENTRY, 2283.7099609375f, 1748.8699951171875f, 1189.7071533203125f, 4.782202720642089843f, 0.0f, 0.0f, -0.68199825286865234f, 0.731353819370269775f, BattlegroundConst.RESPAWN_ONE_DAY)
           )
        {
            Log.Logger.Error("BatteGroundEY: Failed to spawn some objects. The battleground was not created.");

            return false;
        }

        var sg = ObjectManager.GetWorldSafeLoc(EotSGaveyardIds.MAIN_ALLIANCE);

        if (sg == null || !AddSpiritGuide(EotSCreaturesTypes.SPIRIT_MAIN_ALLIANCE, sg.Location.X, sg.Location.Y, sg.Location.Z, 3.124139f, TeamIds.Alliance))
        {
            Log.Logger.Error("BatteGroundEY: Failed to spawn spirit guide. The battleground was not created.");

            return false;
        }

        sg = ObjectManager.GetWorldSafeLoc(EotSGaveyardIds.MAIN_HORDE);

        if (sg == null || !AddSpiritGuide(EotSCreaturesTypes.SPIRIT_MAIN_HORDE, sg.Location.X, sg.Location.Y, sg.Location.Z, 3.193953f, TeamIds.Horde))
        {
            Log.Logger.Error("BatteGroundEY: Failed to spawn spirit guide. The battleground was not created.");

            return false;
        }

        return true;
    }

    public override void StartingEventCloseDoors()
    {
        SpawnBGObject(EotSObjectTypes.DOOR_A, BattlegroundConst.RESPAWN_IMMEDIATELY);
        SpawnBGObject(EotSObjectTypes.DOOR_H, BattlegroundConst.RESPAWN_IMMEDIATELY);

        for (var i = EotSObjectTypes.A_BANNER_FEL_REAVER_CENTER; i < EotSObjectTypes.MAX; ++i)
            SpawnBGObject(i, BattlegroundConst.RESPAWN_ONE_DAY);
    }

    public override void StartingEventOpenDoors()
    {
        SpawnBGObject(EotSObjectTypes.DOOR_A, BattlegroundConst.RESPAWN_ONE_DAY);
        SpawnBGObject(EotSObjectTypes.DOOR_H, BattlegroundConst.RESPAWN_ONE_DAY);

        for (var i = EotSObjectTypes.N_BANNER_FEL_REAVER_LEFT; i <= EotSObjectTypes.FLAG_NETHERSTORM; ++i)
            SpawnBGObject(i, BattlegroundConst.RESPAWN_IMMEDIATELY);

        for (var i = 0; i < EotSPoints.POINTS_MAX; ++i)
        {
            //randomly spawn buff
            var buff = (byte)RandomHelper.URand(0, 2);
            SpawnBGObject(EotSObjectTypes.SPEEDBUFF_FEL_REAVER + buff + i * 3, BattlegroundConst.RESPAWN_IMMEDIATELY);
        }

        // Achievement: Flurry
        TriggerGameEvent(EotSMisc.EVENT_START_BATTLE);
    }

    public override bool UpdatePlayerScore(Player player, ScoreType type, uint value, bool doAddHonor = true)
    {
        if (!base.UpdatePlayerScore(player, type, value, doAddHonor))
            return false;

        switch (type)
        {
            case ScoreType.FlagCaptures:
                player.UpdateCriteria(CriteriaType.TrackedWorldStateUIModified, EotSMisc.OBJECTIVE_CAPTURE_FLAG);

                break;
        }

        return true;
    }

    private void AddPoints(TeamFaction team, uint points)
    {
        var teamIndex = GetTeamIndexByTeamId(team);
        MTeamScores[teamIndex] += points;
        _mHonorScoreTics[teamIndex] += points;

        if (_mHonorScoreTics[teamIndex] >= _mHonorTics)
        {
            RewardHonorToTeam(GetBonusHonorFromKill(1), team);
            _mHonorScoreTics[teamIndex] -= _mHonorTics;
        }

        UpdateTeamScore(teamIndex);
    }

    private void CheckSomeoneJoinedPoint()
    {
        for (byte i = 0; i < EotSPoints.POINTS_MAX; ++i)
        {
            var obj = BgMap.GetGameObject(BgObjects[EotSObjectTypes.TOWER_CAP_FEL_REAVER + i]);

            if (obj == null)
                continue;

            byte j = 0;

            while (j < _mPlayersNearPoint[EotSPoints.POINTS_MAX].Count)
            {
                var player = ObjectAccessor.FindPlayer(_mPlayersNearPoint[EotSPoints.POINTS_MAX][j]);

                if (player == null)
                {
                    Log.Logger.Error("BattlegroundEY:CheckSomeoneJoinedPoint: Player ({0}) could not be found!", _mPlayersNearPoint[EotSPoints.POINTS_MAX][j].ToString());
                    ++j;

                    continue;
                }

                if (player.CanCaptureTowerPoint && player.Location.IsWithinDistInMap(obj, (float)EotSProgressBarConsts.PointRadius))
                {
                    //player joined point!
                    //show progress bar
                    player.SendUpdateWorldState(EotSWorldStateIds.PROGRESS_BAR_PERCENT_GREY, (uint)EotSProgressBarConsts.ProgressBarPercentGrey);
                    player.SendUpdateWorldState(EotSWorldStateIds.PROGRESS_BAR_STATUS, (uint)_mPointBarStatus[i]);
                    player.SendUpdateWorldState(EotSWorldStateIds.PROGRESS_BAR_SHOW, (uint)EotSProgressBarConsts.ProgressBarShow);
                    //add player to point
                    _mPlayersNearPoint[i].Add(_mPlayersNearPoint[EotSPoints.POINTS_MAX][j]);
                    //remove player from "free space"
                    _mPlayersNearPoint[EotSPoints.POINTS_MAX].RemoveAt(j);
                }
                else
                    ++j;
            }
        }
    }

    private void CheckSomeoneLeftPo()
    {
        //reset current point counts
        for (byte i = 0; i < 2 * EotSPoints.POINTS_MAX; ++i)
            _mCurrentPointPlayersCount[i] = 0;

        for (byte i = 0; i < EotSPoints.POINTS_MAX; ++i)
        {
            var obj = BgMap.GetGameObject(BgObjects[EotSObjectTypes.TOWER_CAP_FEL_REAVER + i]);

            if (obj == null)
                continue;

            byte j = 0;

            while (j < _mPlayersNearPoint[i].Count)
            {
                var player = ObjectAccessor.FindPlayer(_mPlayersNearPoint[i][j]);

                if (player == null)
                {
                    Log.Logger.Error("BattlegroundEY:CheckSomeoneLeftPoint Player ({0}) could not be found!", _mPlayersNearPoint[i][j].ToString());
                    //move non-existing players to "free space" - this will cause many errors showing in log, but it is a very important bug
                    _mPlayersNearPoint[EotSPoints.POINTS_MAX].Add(_mPlayersNearPoint[i][j]);
                    _mPlayersNearPoint[i].RemoveAt(j);

                    continue;
                }

                if (!player.CanCaptureTowerPoint || !player.Location.IsWithinDistInMap(obj, (float)EotSProgressBarConsts.PointRadius))
                //move player out of point (add him to players that are out of points
                {
                    _mPlayersNearPoint[EotSPoints.POINTS_MAX].Add(_mPlayersNearPoint[i][j]);
                    _mPlayersNearPoint[i].RemoveAt(j);
                    player.SendUpdateWorldState(EotSWorldStateIds.PROGRESS_BAR_SHOW, (uint)EotSProgressBarConsts.ProgressBarDontShow);
                }
                else
                {
                    //player is neat Id, so update count:
                    _mCurrentPointPlayersCount[2 * i + GetTeamIndexByTeamId(GetPlayerTeam(player.GUID))]++;
                    ++j;
                }
            }
        }
    }

    private void EventPlayerCapturedFlag(Player player, uint bgObjectType)
    {
        if (Status != BattlegroundStatus.InProgress || GetFlagPickerGUID() != player.GUID)
            return;

        SetFlagPicker(ObjectGuid.Empty);
        _mFlagState = EotSFlagState.WaitRespawn;
        player.RemoveAura(EotSMisc.SPELL_NETHERSTORM_FLAG);

        player.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.PvPActive);

        var team = GetPlayerTeam(player.GUID);

        if (team == TeamFaction.Alliance)
        {
            SendBroadcastText(EotSBroadcastTexts.ALLIANCE_CAPTURED_FLAG, ChatMsg.BgSystemAlliance, player);
            PlaySoundToAll(EotSSoundIds.FLAG_CAPTURED_ALLIANCE);
        }
        else
        {
            SendBroadcastText(EotSBroadcastTexts.HORDE_CAPTURED_FLAG, ChatMsg.BgSystemHorde, player);
            PlaySoundToAll(EotSSoundIds.FLAG_CAPTURED_HORDE);
        }

        SpawnBGObject((int)bgObjectType, BattlegroundConst.RESPAWN_IMMEDIATELY);

        _mFlagsTimer = EotSMisc.FLAG_RESPAWN_TIME;
        _mFlagCapturedBgObjectType = bgObjectType;

        var teamID = GetTeamIndexByTeamId(team);

        if (_mTeamPointsCount[teamID] > 0)
            AddPoints(team, EotSMisc.FlagPoints[_mTeamPointsCount[teamID] - 1]);

        UpdateWorldState(EotSWorldStateIds.NETHERSTORM_FLAG_STATE_HORDE, (int)EotSFlagState.OnBase);
        UpdateWorldState(EotSWorldStateIds.NETHERSTORM_FLAG_STATE_ALLIANCE, (int)EotSFlagState.OnBase);

        UpdatePlayerScore(player, ScoreType.FlagCaptures, 1);
    }

    private void EventTeamCapturedPoint(Player player, int point)
    {
        if (Status != BattlegroundStatus.InProgress)
            return;

        var team = GetPlayerTeam(player.GUID);

        SpawnBGObject(EotSMisc.MCapturingPointTypes[point].DespawnNeutralObjectType, BattlegroundConst.RESPAWN_ONE_DAY);
        SpawnBGObject(EotSMisc.MCapturingPointTypes[point].DespawnNeutralObjectType + 1, BattlegroundConst.RESPAWN_ONE_DAY);
        SpawnBGObject(EotSMisc.MCapturingPointTypes[point].DespawnNeutralObjectType + 2, BattlegroundConst.RESPAWN_ONE_DAY);

        if (team == TeamFaction.Alliance)
        {
            _mTeamPointsCount[TeamIds.Alliance]++;
            SpawnBGObject(EotSMisc.MCapturingPointTypes[point].SpawnObjectTypeAlliance, BattlegroundConst.RESPAWN_IMMEDIATELY);
            SpawnBGObject(EotSMisc.MCapturingPointTypes[point].SpawnObjectTypeAlliance + 1, BattlegroundConst.RESPAWN_IMMEDIATELY);
            SpawnBGObject(EotSMisc.MCapturingPointTypes[point].SpawnObjectTypeAlliance + 2, BattlegroundConst.RESPAWN_IMMEDIATELY);
        }
        else
        {
            _mTeamPointsCount[TeamIds.Horde]++;
            SpawnBGObject(EotSMisc.MCapturingPointTypes[point].SpawnObjectTypeHorde, BattlegroundConst.RESPAWN_IMMEDIATELY);
            SpawnBGObject(EotSMisc.MCapturingPointTypes[point].SpawnObjectTypeHorde + 1, BattlegroundConst.RESPAWN_IMMEDIATELY);
            SpawnBGObject(EotSMisc.MCapturingPointTypes[point].SpawnObjectTypeHorde + 2, BattlegroundConst.RESPAWN_IMMEDIATELY);
        }

        //buff isn't respawned

        _mPointOwnedByTeam[point] = team;
        _mPointState[point] = EotSPointState.UnderControl;

        if (team == TeamFaction.Alliance)
            SendBroadcastText(EotSMisc.MCapturingPointTypes[point].MessageIdAlliance, ChatMsg.BgSystemAlliance, player);
        else
            SendBroadcastText(EotSMisc.MCapturingPointTypes[point].MessageIdHorde, ChatMsg.BgSystemHorde, player);

        if (!BgCreatures[point].IsEmpty)
            DelCreature(point);

        var sg = ObjectManager.GetWorldSafeLoc(EotSMisc.MCapturingPointTypes[point].GraveYardId);

        if (sg == null || !AddSpiritGuide(point, sg.Location.X, sg.Location.Y, sg.Location.Z, 3.124139f, GetTeamIndexByTeamId(team)))
            Log.Logger.Error("BatteGroundEY: Failed to spawn spirit guide. point: {0}, team: {1}, graveyard_id: {2}",
                             point,
                             team,
                             EotSMisc.MCapturingPointTypes[point].GraveYardId);

        //    SpawnBGCreature(Point, RESPAWN_IMMEDIATELY);

        UpdatePointsIcons(team, point);
        UpdatePointsCount(team);

        if (point >= EotSPoints.POINTS_MAX)
            return;

        var trigger = GetBGCreature(point + 6) ?? AddCreature(SharedConst.WorldTrigger, point + 6, EotSMisc.TriggerPositions[point], GetTeamIndexByTeamId(team)); //0-5 spirit guides

        //add bonus honor aura trigger creature when node is accupied
        //cast bonus aura (+50% honor in 25yards)
        //aura should only apply to players who have accupied the node, set correct faction for trigger
        if (trigger == null)
            return;

        trigger.Faction = team == TeamFaction.Alliance ? 84u : 83;
        trigger.SpellFactory.CastSpell(trigger, BattlegroundConst.SPELL_HONORABLE_DEFENDER25_Y);
    }

    private void EventTeamLostPoint(Player player, int point)
    {
        if (Status != BattlegroundStatus.InProgress)
            return;

        //Natural point
        var team = _mPointOwnedByTeam[point];

        if (team == 0)
            return;

        if (team == TeamFaction.Alliance)
        {
            _mTeamPointsCount[TeamIds.Alliance]--;
            SpawnBGObject(EotSMisc.MLosingPointTypes[point].DespawnObjectTypeAlliance, BattlegroundConst.RESPAWN_ONE_DAY);
            SpawnBGObject(EotSMisc.MLosingPointTypes[point].DespawnObjectTypeAlliance + 1, BattlegroundConst.RESPAWN_ONE_DAY);
            SpawnBGObject(EotSMisc.MLosingPointTypes[point].DespawnObjectTypeAlliance + 2, BattlegroundConst.RESPAWN_ONE_DAY);
        }
        else
        {
            _mTeamPointsCount[TeamIds.Horde]--;
            SpawnBGObject(EotSMisc.MLosingPointTypes[point].DespawnObjectTypeHorde, BattlegroundConst.RESPAWN_ONE_DAY);
            SpawnBGObject(EotSMisc.MLosingPointTypes[point].DespawnObjectTypeHorde + 1, BattlegroundConst.RESPAWN_ONE_DAY);
            SpawnBGObject(EotSMisc.MLosingPointTypes[point].DespawnObjectTypeHorde + 2, BattlegroundConst.RESPAWN_ONE_DAY);
        }

        SpawnBGObject(EotSMisc.MLosingPointTypes[point].SpawnNeutralObjectType, BattlegroundConst.RESPAWN_IMMEDIATELY);
        SpawnBGObject(EotSMisc.MLosingPointTypes[point].SpawnNeutralObjectType + 1, BattlegroundConst.RESPAWN_IMMEDIATELY);
        SpawnBGObject(EotSMisc.MLosingPointTypes[point].SpawnNeutralObjectType + 2, BattlegroundConst.RESPAWN_IMMEDIATELY);

        //buff isn't despawned

        _mPointOwnedByTeam[point] = TeamFaction.Other;
        _mPointState[point] = EotSPointState.NoOwner;

        if (team == TeamFaction.Alliance)
            SendBroadcastText(EotSMisc.MLosingPointTypes[point].MessageIdAlliance, ChatMsg.BgSystemAlliance, player);
        else
            SendBroadcastText(EotSMisc.MLosingPointTypes[point].MessageIdHorde, ChatMsg.BgSystemHorde, player);

        UpdatePointsIcons(team, point);
        UpdatePointsCount(team);

        //remove bonus honor aura trigger creature when node is lost
        if (point < EotSPoints.POINTS_MAX)
            DelCreature(point + 6); //null checks are in DelCreature! 0-5 spirit guides
    }

    private ObjectGuid GetDroppedFlagGUID()
    {
        return _mDroppedFlagGUID;
    }

    private BattlegroundPointCaptureStatus GetPointCaptureStatus(uint point)
    {
        if (_mPointBarStatus[point] >= EotSProgressBarConsts.ProgressBarAliControlled)
            return BattlegroundPointCaptureStatus.AllianceControlled;

        if (_mPointBarStatus[point] <= EotSProgressBarConsts.ProgressBarHordeControlled)
            return BattlegroundPointCaptureStatus.HordeControlled;

        if (_mCurrentPointPlayersCount[2 * point] == _mCurrentPointPlayersCount[2 * point + 1])
            return BattlegroundPointCaptureStatus.Neutral;

        return _mCurrentPointPlayersCount[2 * point] > _mCurrentPointPlayersCount[2 * point + 1]
                   ? BattlegroundPointCaptureStatus.AllianceCapturing
                   : BattlegroundPointCaptureStatus.HordeCapturing;
    }

    private bool IsFlagPickedup()
    {
        return !_mFlagKeeper.IsEmpty;
    }

    private void RespawnFlag(bool sendMessage)
    {
        if (_mFlagCapturedBgObjectType > 0)
            SpawnBGObject((int)_mFlagCapturedBgObjectType, BattlegroundConst.RESPAWN_ONE_DAY);

        _mFlagCapturedBgObjectType = 0;
        _mFlagState = EotSFlagState.OnBase;
        SpawnBGObject(EotSObjectTypes.FLAG_NETHERSTORM, BattlegroundConst.RESPAWN_IMMEDIATELY);

        if (sendMessage)
        {
            SendBroadcastText(EotSBroadcastTexts.FLAG_RESET, ChatMsg.BgSystemNeutral);
            PlaySoundToAll(EotSSoundIds.FLAG_RESET); // flags respawned sound...
        }

        UpdateWorldState(EotSWorldStateIds.NETHERSTORM_FLAG, 1);
    }

    private void RespawnFlagAfterDrop()
    {
        RespawnFlag(true);

        var obj = BgMap.GetGameObject(GetDroppedFlagGUID());

        if (obj != null)
            obj.Delete();
        else
            Log.Logger.Error("BattlegroundEY: Unknown dropped Id ({0}).", GetDroppedFlagGUID().ToString());

        SetDroppedFlagGUID(ObjectGuid.Empty);
    }

    private void SetFlagPicker(ObjectGuid guid)
    {
        _mFlagKeeper = guid;
    }

    private void UpdatePointsCount(TeamFaction team)
    {
        if (team == TeamFaction.Alliance)
            UpdateWorldState(EotSWorldStateIds.ALLIANCE_BASE, (int)_mTeamPointsCount[TeamIds.Alliance]);
        else
            UpdateWorldState(EotSWorldStateIds.HORDE_BASE, (int)_mTeamPointsCount[TeamIds.Horde]);
    }

    private void UpdatePointsIcons(TeamFaction team, int point)
    {
        //we MUST firstly send 0, after that we can send 1!!!
        if (_mPointState[point] == EotSPointState.UnderControl)
        {
            UpdateWorldState(EotSMisc.MPointsIconStruct[point].WorldStateControlIndex, 0);

            if (team == TeamFaction.Alliance)
                UpdateWorldState(EotSMisc.MPointsIconStruct[point].WorldStateAllianceControlledIndex, 1);
            else
                UpdateWorldState(EotSMisc.MPointsIconStruct[point].WorldStateHordeControlledIndex, 1);
        }
        else
        {
            if (team == TeamFaction.Alliance)
                UpdateWorldState(EotSMisc.MPointsIconStruct[point].WorldStateAllianceControlledIndex, 0);
            else
                UpdateWorldState(EotSMisc.MPointsIconStruct[point].WorldStateHordeControlledIndex, 0);

            UpdateWorldState(EotSMisc.MPointsIconStruct[point].WorldStateControlIndex, 1);
        }
    }

    private void UpdatePointStatuses()
    {
        for (byte point = 0; point < EotSPoints.POINTS_MAX; ++point)
        {
            if (!_mPlayersNearPoint[point].Empty())
            {
                //count new point bar status:
                var pointDelta = _mCurrentPointPlayersCount[2 * point] - _mCurrentPointPlayersCount[2 * point + 1];
                MathFunctions.RoundToInterval(ref pointDelta, -(int)EotSProgressBarConsts.PointMaxCapturersCount, EotSProgressBarConsts.PointMaxCapturersCount);
                _mPointBarStatus[point] += pointDelta;

                if (_mPointBarStatus[point] > EotSProgressBarConsts.ProgressBarAliControlled)
                    //point is fully alliance's
                    _mPointBarStatus[point] = EotSProgressBarConsts.ProgressBarAliControlled;

                if (_mPointBarStatus[point] < EotSProgressBarConsts.ProgressBarHordeControlled)
                    //point is fully horde's
                    _mPointBarStatus[point] = EotSProgressBarConsts.ProgressBarHordeControlled;

                var pointOwnerTeamId = _mPointBarStatus[point] switch
                {
                    //find which team should own this point
                    <= EotSProgressBarConsts.ProgressBarNeutralLow => (uint)TeamFaction.Horde,
                    >= EotSProgressBarConsts.ProgressBarNeutralHigh => (uint)TeamFaction.Alliance,
                    _ => (uint)EotSPointState.NoOwner
                };

                for (byte i = 0; i < _mPlayersNearPoint[point].Count; ++i)
                {
                    var player = ObjectAccessor.FindPlayer(_mPlayersNearPoint[point][i]);

                    if (player == null)
                        continue;

                    player.SendUpdateWorldState(EotSWorldStateIds.PROGRESS_BAR_STATUS, (uint)_mPointBarStatus[point]);
                    var team = GetPlayerTeam(player.GUID);

                    //if point owner changed we must evoke event!
                    if (pointOwnerTeamId != (uint)_mPointOwnedByTeam[point])
                    {
                        //point was uncontrolled and player is from team which captured point
                        if (_mPointState[point] == EotSPointState.Uncontrolled && (uint)team == pointOwnerTeamId)
                            EventTeamCapturedPoint(player, point);

                        //point was under control and player isn't from team which controlled it
                        if (_mPointState[point] == EotSPointState.UnderControl && team != _mPointOwnedByTeam[point])
                            EventTeamLostPoint(player, point);
                    }

                    // @workaround The original AreaTrigger is covered by a bigger one and not triggered on client side.
                    if (point != EotSPoints.FEL_REAVER || _mPointOwnedByTeam[point] != team)
                        continue;

                    if (_mFlagState == 0 || GetFlagPickerGUID() != player.GUID)
                        continue;

                    if (player.Location.GetDistance(2044.0f, 1729.729f, 1190.03f) < 3.0f)
                        EventPlayerCapturedFlag(player, EotSObjectTypes.FLAG_FEL_REAVER);
                }
            }

            var captureStatus = GetPointCaptureStatus(point);

            if (_mLastPointCaptureStatus[point] == captureStatus)
                continue;

            UpdateWorldState(EotSMisc.MPointsIconStruct[point].WorldStateAllianceStatusBarIcon, captureStatus == BattlegroundPointCaptureStatus.AllianceControlled ? 2 : captureStatus == BattlegroundPointCaptureStatus.AllianceCapturing ? 1 : 0);
            UpdateWorldState(EotSMisc.MPointsIconStruct[point].WorldStateHordeStatusBarIcon, captureStatus == BattlegroundPointCaptureStatus.HordeControlled       ? 2 : captureStatus == BattlegroundPointCaptureStatus.HordeCapturing    ? 1 : 0);
            _mLastPointCaptureStatus[point] = captureStatus;
        }
    }

    private void UpdateTeamScore(int team)
    {
        var score = GetTeamScore(team);

        if (score >= EotSScoreIds.MAX_TEAM_SCORE)
        {
            score = EotSScoreIds.MAX_TEAM_SCORE;

            EndBattleground(team == TeamIds.Alliance ? TeamFaction.Alliance : TeamFaction.Horde);
        }

        UpdateWorldState(team == TeamIds.Alliance ? EotSWorldStateIds.ALLIANCE_RESOURCES : EotSWorldStateIds.HORDE_RESOURCES, (int)score);
    }
}