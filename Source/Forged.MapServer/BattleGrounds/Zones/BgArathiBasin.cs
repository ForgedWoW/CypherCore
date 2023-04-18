// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Framework.Constants;
using Serilog;

namespace Forged.MapServer.BattleGrounds.Zones;

internal class BgArathiBasin : Battleground
{
    public const uint ABBG_WEEKEND_HONOR_TICKS = 160;

    public const uint ABBG_WEEKEND_REPUTATION_TICKS = 120;

    public const int EVENT_START_BATTLE = 9158;

    public const uint EXPLOIT_TELEPORT_LOCATION_ALLIANCE = 3705;

    public const uint EXPLOIT_TELEPORT_LOCATION_HORDE = 3706;

    public const int FLAG_CAPTURING_TIME = 60000;

    public const int MAX_TEAM_SCORE = 1500;

    //Const
    public const uint NOT_ABBG_WEEKEND_HONOR_TICKS = 260;

    public const uint NOT_ABBG_WEEKEND_REPUTATION_TICKS = 160;
    // Achievement: Let's Get This Done

    public const uint SOUND_ASSAULTED_ALLIANCE = 8212;
    public const uint SOUND_ASSAULTED_HORDE = 8174;
    public const int SOUND_CAPTURED_ALLIANCE = 8173;
    public const int SOUND_CAPTURED_HORDE = 8213;
    public const int SOUND_CLAIMED = 8192;
    public const int SOUND_NEAR_VICTORY_ALLIANCE = 8456;
    public const int SOUND_NEAR_VICTORY_HORDE = 8457;

    public const int WARNING_NEAR_VICTORY_SCORE = 1400;

    // x, y, z, o
    public static float[][] BuffPositions =
    {
        new[]
        {
            1185.566f, 1184.629f, -56.36329f, 2.303831f
        }, // stables
        new[]
        {
            990.1131f, 1008.73f, -42.60328f, 0.8203033f
        }, // blacksmith
        new[]
        {
            818.0089f, 842.3543f, -56.54062f, 3.176533f
        }, // farm
        new[]
        {
            808.8463f, 1185.417f, 11.92161f, 5.619962f
        }, // lumber mill
        new[]
        {
            1147.091f, 816.8362f, -98.39896f, 6.056293f
        } // gold mine
    };

    // x, y, z, o, rot0, rot1, rot2, rot3
    public static float[][] DoorPositions =
    {
        new[]
        {
            1284.597f, 1281.167f, -15.97792f, 0.7068594f, 0.012957f, -0.060288f, 0.344959f, 0.93659f
        },
        new[]
        {
            708.0903f, 708.4479f, -17.8342f, -2.391099f, 0.050291f, 0.015127f, 0.929217f, -0.365784f
        }
    };

    // WorldSafeLocs ids for 5 nodes, and for ally, and horde starting location
    public static uint[] GraveyardIds =
    {
        895, 894, 893, 897, 896, 898, 899
    };

    public static int[] NodeIcons =
    {
        1842, 1846, 1845, 1844, 1843
    };

    public static Position[] NodePositions =
    {
        new(1166.785f, 1200.132f, -56.70859f, 0.9075713f), // stables
        new(977.0156f, 1046.616f, -44.80923f, -2.600541f), // blacksmith
        new(806.1821f, 874.2723f, -55.99371f, -2.303835f), // farm
        new(856.1419f, 1148.902f, 11.18469f, -2.303835f),  // lumber mill
        new(1146.923f, 848.1782f, -110.917f, -0.7330382f)  // gold mine
    };

    public static int[] NodeStates =
    {
        1767, 1782, 1772, 1792, 1787
    };

    public static Position[] SpiritGuidePos =
    {
        new(1200.03f, 1171.09f, -56.47f, 5.15f), // stables
        new(1017.43f, 960.61f, -42.95f, 4.88f),  // blacksmith
        new(833.00f, 793.00f, -57.25f, 5.27f),   // farm
        new(775.17f, 1206.40f, 15.79f, 1.90f),   // lumber mill
        new(1207.48f, 787.00f, -83.36f, 5.51f),  // gold mine
        new(1354.05f, 1275.48f, -11.30f, 4.77f), // alliance starting base
        new(714.61f, 646.15f, -10.87f, 4.34f)    // horde starting base
    };

    // Tick intervals and given points: case 0, 1, 2, 3, 4, 5 captured nodes
    public static uint[] TickIntervals =
    {
        0, 12000, 9000, 6000, 3000, 1000
    };

    public static uint[] TickPoints =
    {
        0, 10, 10, 10, 10, 30
    };

    private readonly BannerTimer[] _mBannerTimers = new BannerTimer[ABBattlegroundNodes.DYNAMIC_NODES_COUNT];

    private readonly uint[] _mHonorScoreTics = new uint[SharedConst.PvpTeamsCount];

    private readonly uint[] _mLastTick = new uint[SharedConst.PvpTeamsCount];

    /// <summary>
    ///     Nodes info:
    ///     0: neutral
    ///     1: ally contested
    ///     2: horde contested
    ///     3: ally occupied
    ///     4: horde occupied
    /// </summary>
    private readonly ABNodeStatus[] _mNodes = new ABNodeStatus[ABBattlegroundNodes.DYNAMIC_NODES_COUNT];

    private readonly uint[] _mNodeTimers = new uint[ABBattlegroundNodes.DYNAMIC_NODES_COUNT];
    private readonly ABNodeStatus[] _mPrevNodes = new ABNodeStatus[ABBattlegroundNodes.DYNAMIC_NODES_COUNT];
    private readonly uint[] _mReputationScoreTics = new uint[SharedConst.PvpTeamsCount];
    private uint _mHonorTics;
    private bool _mIsInformedNearVictory;
    private uint _mReputationTics;

    public BgArathiBasin(BattlegroundTemplate battlegroundTemplate) : base(battlegroundTemplate)
    {
        _mIsInformedNearVictory = false;
        MBuffChange = true;
        BgObjects = new ObjectGuid[ABObjectTypes.MAX];
        BgCreatures = new ObjectGuid[ABBattlegroundNodes.ALL_COUNT + 5]; //+5 for aura triggers

        for (byte i = 0; i < ABBattlegroundNodes.DYNAMIC_NODES_COUNT; ++i)
        {
            _mNodes[i] = 0;
            _mPrevNodes[i] = 0;
            _mNodeTimers[i] = 0;
            _mBannerTimers[i].Timer = 0;
            _mBannerTimers[i].Type = 0;
            _mBannerTimers[i].TeamIndex = 0;
        }

        for (byte i = 0; i < SharedConst.PvpTeamsCount; ++i)
        {
            _mLastTick[i] = 0;
            _mHonorScoreTics[i] = 0;
            _mReputationScoreTics[i] = 0;
        }

        _mHonorTics = 0;
        _mReputationTics = 0;
    }

    public override void AddPlayer(Player player)
    {
        var isInBattleground = IsPlayerInBattleground(player.GUID);
        base.AddPlayer(player);

        if (!isInBattleground)
            PlayerScores[player.GUID] = new BattlegroundABScore(player.GUID, player.GetBgTeam());
    }

    public override void EndBattleground(TeamFaction winner)
    {
        // Win reward
        if (winner == TeamFaction.Alliance)
            RewardHonorToTeam(GetBonusHonorFromKill(1), TeamFaction.Alliance);

        if (winner == TeamFaction.Horde)
            RewardHonorToTeam(GetBonusHonorFromKill(1), TeamFaction.Horde);

        // Complete map_end rewards (even if no team wins)
        RewardHonorToTeam(GetBonusHonorFromKill(1), TeamFaction.Horde);
        RewardHonorToTeam(GetBonusHonorFromKill(1), TeamFaction.Alliance);

        base.EndBattleground(winner);
    }

    //Invoked if a player used a banner as a gameobject
    public override void EventPlayerClickedOnFlag(Player source, GameObject targetObj)
    {
        if (Status != BattlegroundStatus.InProgress)
            return;

        byte node = ABBattlegroundNodes.NODE_STABLES;
        var obj = BgMap.GetGameObject(BgObjects[node * 8 + 7]);

        while (node < ABBattlegroundNodes.DYNAMIC_NODES_COUNT && (!obj || !source.Location.IsWithinDistInMap(obj, 10)))
        {
            ++node;
            obj = BgMap.GetGameObject(BgObjects[node * 8 + ABObjectTypes.AURA_CONTESTED]);
        }

        if (node == ABBattlegroundNodes.DYNAMIC_NODES_COUNT)
            // this means our player isn't close to any of banners - maybe cheater ??
            return;

        var teamIndex = GetTeamIndexByTeamId(GetPlayerTeam(source.GUID));

        // Check if player really could use this banner, not cheated
        if (!(_mNodes[node] == 0 || teamIndex == (int)_mNodes[node] % 2))
            return;

        source.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.PvPActive);
        uint sound;

        // If node is neutral, change to contested
        if (_mNodes[node] == ABNodeStatus.Neutral)
        {
            UpdatePlayerScore(source, ScoreType.BasesAssaulted, 1);
            _mPrevNodes[node] = _mNodes[node];
            _mNodes[node] = (ABNodeStatus)(teamIndex + 1);
            // burn current neutral banner
            _DelBanner(node, ABNodeStatus.Neutral, 0);
            // create new contested banner
            _CreateBanner(node, ABNodeStatus.Contested, (byte)teamIndex, true);
            _SendNodeUpdate(node);
            _mNodeTimers[node] = FLAG_CAPTURING_TIME;

            // FIXME: team and node names not localized
            if (teamIndex == TeamIds.Alliance)
                SendBroadcastText(ABBattlegroundBroadcastTexts.ABNodes[node].TextAllianceClaims, ChatMsg.BgSystemAlliance, source);
            else
                SendBroadcastText(ABBattlegroundBroadcastTexts.ABNodes[node].TextHordeClaims, ChatMsg.BgSystemHorde, source);

            sound = SOUND_CLAIMED;
        }
        // If node is contested
        else if (_mNodes[node] == ABNodeStatus.AllyContested || _mNodes[node] == ABNodeStatus.HordeContested)
        {
            // If last state is NOT occupied, change node to enemy-contested
            if (_mPrevNodes[node] < ABNodeStatus.Occupied)
            {
                UpdatePlayerScore(source, ScoreType.BasesAssaulted, 1);
                _mPrevNodes[node] = _mNodes[node];
                _mNodes[node] = ABNodeStatus.Contested + teamIndex;
                // burn current contested banner
                _DelBanner(node, ABNodeStatus.Contested, (byte)teamIndex);
                // create new contested banner
                _CreateBanner(node, ABNodeStatus.Contested, (byte)teamIndex, true);
                _SendNodeUpdate(node);
                _mNodeTimers[node] = FLAG_CAPTURING_TIME;

                if (teamIndex == TeamIds.Alliance)
                    SendBroadcastText(ABBattlegroundBroadcastTexts.ABNodes[node].TextAllianceAssaulted, ChatMsg.BgSystemAlliance, source);
                else
                    SendBroadcastText(ABBattlegroundBroadcastTexts.ABNodes[node].TextHordeAssaulted, ChatMsg.BgSystemHorde, source);
            }
            // If contested, change back to occupied
            else
            {
                UpdatePlayerScore(source, ScoreType.BasesDefended, 1);
                _mPrevNodes[node] = _mNodes[node];
                _mNodes[node] = ABNodeStatus.Occupied + teamIndex;
                // burn current contested banner
                _DelBanner(node, ABNodeStatus.Contested, (byte)teamIndex);
                // create new occupied banner
                _CreateBanner(node, ABNodeStatus.Occupied, (byte)teamIndex, true);
                _SendNodeUpdate(node);
                _mNodeTimers[node] = 0;
                _NodeOccupied(node, teamIndex == TeamIds.Alliance ? TeamFaction.Alliance : TeamFaction.Horde);

                if (teamIndex == TeamIds.Alliance)
                    SendBroadcastText(ABBattlegroundBroadcastTexts.ABNodes[node].TextAllianceDefended, ChatMsg.BgSystemAlliance, source);
                else
                    SendBroadcastText(ABBattlegroundBroadcastTexts.ABNodes[node].TextHordeDefended, ChatMsg.BgSystemHorde, source);
            }

            sound = teamIndex == TeamIds.Alliance ? SOUND_ASSAULTED_ALLIANCE : SOUND_ASSAULTED_HORDE;
        }
        // If node is occupied, change to enemy-contested
        else
        {
            UpdatePlayerScore(source, ScoreType.BasesAssaulted, 1);
            _mPrevNodes[node] = _mNodes[node];
            _mNodes[node] = ABNodeStatus.Contested + teamIndex;
            // burn current occupied banner
            _DelBanner(node, ABNodeStatus.Occupied, (byte)teamIndex);
            // create new contested banner
            _CreateBanner(node, ABNodeStatus.Contested, (byte)teamIndex, true);
            _SendNodeUpdate(node);
            _NodeDeOccupied(node);
            _mNodeTimers[node] = FLAG_CAPTURING_TIME;

            if (teamIndex == TeamIds.Alliance)
                SendBroadcastText(ABBattlegroundBroadcastTexts.ABNodes[node].TextAllianceAssaulted, ChatMsg.BgSystemAlliance, source);
            else
                SendBroadcastText(ABBattlegroundBroadcastTexts.ABNodes[node].TextHordeAssaulted, ChatMsg.BgSystemHorde, source);

            sound = teamIndex == TeamIds.Alliance ? SOUND_ASSAULTED_ALLIANCE : SOUND_ASSAULTED_HORDE;
        }

        // If node is occupied again, send "X has taken the Y" msg.
        if (_mNodes[node] >= ABNodeStatus.Occupied)
        {
            // FIXME: team and node names not localized
            if (teamIndex == TeamIds.Alliance)
                SendBroadcastText(ABBattlegroundBroadcastTexts.ABNodes[node].TextAllianceTaken, ChatMsg.BgSystemAlliance);
            else
                SendBroadcastText(ABBattlegroundBroadcastTexts.ABNodes[node].TextHordeTaken, ChatMsg.BgSystemHorde);
        }

        PlaySoundToAll(sound);
    }

    public override WorldSafeLocsEntry GetClosestGraveYard(Player player)
    {
        var teamIndex = GetTeamIndexByTeamId(GetPlayerTeam(player.GUID));

        // Is there any occupied node for this team?
        List<byte> nodes = new();

        for (byte i = 0; i < ABBattlegroundNodes.DYNAMIC_NODES_COUNT; ++i)
            if (_mNodes[i] == ABNodeStatus.Occupied + teamIndex)
                nodes.Add(i);

        WorldSafeLocsEntry goodEntry = null;

        // If so, select the closest node to place ghost on
        if (!nodes.Empty())
        {
            var plrX = player.Location.X;
            var plrY = player.Location.Y;

            var mindist = 999999.0f;

            for (byte i = 0; i < nodes.Count; ++i)
            {
                var entry = Global.ObjectMgr.GetWorldSafeLoc(GraveyardIds[nodes[i]]);

                if (entry == null)
                    continue;

                var dist = (entry.Loc.X - plrX) * (entry.Loc.X - plrX) + (entry.Loc.Y - plrY) * (entry.Loc.Y - plrY);

                if (mindist > dist)
                {
                    mindist = dist;
                    goodEntry = entry;
                }
            }

            nodes.Clear();
        }

        // If not, place ghost on starting location
        if (goodEntry == null)
            goodEntry = Global.ObjectMgr.GetWorldSafeLoc(GraveyardIds[teamIndex + 5]);

        return goodEntry;
    }

    public override WorldSafeLocsEntry GetExploitTeleportLocation(TeamFaction team)
    {
        return Global.ObjectMgr.GetWorldSafeLoc(team == TeamFaction.Alliance ? EXPLOIT_TELEPORT_LOCATION_ALLIANCE : EXPLOIT_TELEPORT_LOCATION_HORDE);
    }

    public override TeamFaction GetPrematureWinner()
    {
        // How many bases each team owns
        byte ally = 0, horde = 0;

        for (byte i = 0; i < ABBattlegroundNodes.DYNAMIC_NODES_COUNT; ++i)
            if (_mNodes[i] == ABNodeStatus.AllyOccupied)
                ++ally;
            else if (_mNodes[i] == ABNodeStatus.HordeOccupied)
                ++horde;

        if (ally > horde)
            return TeamFaction.Alliance;

        if (horde > ally)
            return TeamFaction.Horde;

        // If the values are equal, fall back to the original result (based on number of players on each team)
        return base.GetPrematureWinner();
    }

    public override void HandleAreaTrigger(Player player, uint trigger, bool entered)
    {
        switch (trigger)
        {
            case 6635: // Horde Start
            case 6634: // Alliance Start
                if (Status == BattlegroundStatus.WaitJoin && !entered)
                    TeleportPlayerToExploitLocation(player);

                break;
            case 3948: // Arathi Basin Alliance Exit.
            case 3949: // Arathi Basin Horde Exit.
            case 3866: // Stables
            case 3869: // Gold Mine
            case 3867: // Farm
            case 3868: // Lumber Mill
            case 3870: // Black Smith
            case 4020: // Unk1
            case 4021: // Unk2
            case 4674: // Unk3
            default:
                base.HandleAreaTrigger(player, trigger, entered);

                break;
        }
    }

    public override void PostUpdateImpl(uint diff)
    {
        if (Status == BattlegroundStatus.InProgress)
        {
            int[] teamPoints =
            {
                0, 0
            };

            for (byte node = 0; node < ABBattlegroundNodes.DYNAMIC_NODES_COUNT; ++node)
            {
                // 3 sec delay to spawn new banner instead previous despawned one
                if (_mBannerTimers[node].Timer != 0)
                {
                    if (_mBannerTimers[node].Timer > diff)
                        _mBannerTimers[node].Timer -= diff;
                    else
                    {
                        _mBannerTimers[node].Timer = 0;
                        _CreateBanner(node, (ABNodeStatus)_mBannerTimers[node].Type, _mBannerTimers[node].TeamIndex, false);
                    }
                }

                // 1-minute to occupy a node from contested state
                if (_mNodeTimers[node] != 0)
                {
                    if (_mNodeTimers[node] > diff)
                        _mNodeTimers[node] -= diff;
                    else
                    {
                        _mNodeTimers[node] = 0;
                        // Change from contested to occupied !
                        var teamIndex = (int)_mNodes[node] - 1;
                        _mPrevNodes[node] = _mNodes[node];
                        _mNodes[node] += 2;
                        // burn current contested banner
                        _DelBanner(node, ABNodeStatus.Contested, (byte)teamIndex);
                        // create new occupied banner
                        _CreateBanner(node, ABNodeStatus.Occupied, teamIndex, true);
                        _SendNodeUpdate(node);
                        _NodeOccupied(node, teamIndex == TeamIds.Alliance ? TeamFaction.Alliance : TeamFaction.Horde);
                        // Message to chatlog

                        if (teamIndex == 0)
                        {
                            SendBroadcastText(ABBattlegroundBroadcastTexts.ABNodes[node].TextAllianceTaken, ChatMsg.BgSystemAlliance);
                            PlaySoundToAll(SOUND_CAPTURED_ALLIANCE);
                        }
                        else
                        {
                            SendBroadcastText(ABBattlegroundBroadcastTexts.ABNodes[node].TextHordeTaken, ChatMsg.BgSystemHorde);
                            PlaySoundToAll(SOUND_CAPTURED_HORDE);
                        }
                    }
                }

                for (var team = 0; team < SharedConst.PvpTeamsCount; ++team)
                    if (_mNodes[node] == team + ABNodeStatus.Occupied)
                        ++teamPoints[team];
            }

            // Accumulate points
            for (var team = 0; team < SharedConst.PvpTeamsCount; ++team)
            {
                var points = teamPoints[team];

                if (points == 0)
                    continue;

                _mLastTick[team] += diff;

                if (_mLastTick[team] > TickIntervals[points])
                {
                    _mLastTick[team] -= TickIntervals[points];
                    MTeamScores[team] += TickPoints[points];
                    _mHonorScoreTics[team] += TickPoints[points];
                    _mReputationScoreTics[team] += TickPoints[points];

                    if (_mReputationScoreTics[team] >= _mReputationTics)
                    {
                        if (team == TeamIds.Alliance)
                            RewardReputationToTeam(509, 10, TeamFaction.Alliance);
                        else
                            RewardReputationToTeam(510, 10, TeamFaction.Horde);

                        _mReputationScoreTics[team] -= _mReputationTics;
                    }

                    if (_mHonorScoreTics[team] >= _mHonorTics)
                    {
                        RewardHonorToTeam(GetBonusHonorFromKill(1), team == TeamIds.Alliance ? TeamFaction.Alliance : TeamFaction.Horde);
                        _mHonorScoreTics[team] -= _mHonorTics;
                    }

                    if (!_mIsInformedNearVictory && MTeamScores[team] > WARNING_NEAR_VICTORY_SCORE)
                    {
                        if (team == TeamIds.Alliance)
                        {
                            SendBroadcastText(ABBattlegroundBroadcastTexts.ALLIANCE_NEAR_VICTORY, ChatMsg.BgSystemNeutral);
                            PlaySoundToAll(SOUND_NEAR_VICTORY_ALLIANCE);
                        }
                        else
                        {
                            SendBroadcastText(ABBattlegroundBroadcastTexts.HORDE_NEAR_VICTORY, ChatMsg.BgSystemNeutral);
                            PlaySoundToAll(SOUND_NEAR_VICTORY_HORDE);
                        }

                        _mIsInformedNearVictory = true;
                    }

                    if (MTeamScores[team] > MAX_TEAM_SCORE)
                        MTeamScores[team] = MAX_TEAM_SCORE;

                    if (team == TeamIds.Alliance)
                        UpdateWorldState(ABWorldStates.RESOURCES_ALLY, (int)MTeamScores[team]);
                    else
                        UpdateWorldState(ABWorldStates.RESOURCES_HORDE, (int)MTeamScores[team]);

                    // update achievement flags
                    // we increased m_TeamScores[team] so we just need to check if it is 500 more than other teams resources
                    var otherTeam = (team + 1) % SharedConst.PvpTeamsCount;

                    if (MTeamScores[team] > MTeamScores[otherTeam] + 500)
                    {
                        if (team == TeamIds.Alliance)
                            UpdateWorldState(ABWorldStates.HAD_500DISADVANTAGE_HORDE, 1);
                        else
                            UpdateWorldState(ABWorldStates.HAD_500DISADVANTAGE_ALLIANCE, 1);
                    }
                }
            }

            // Test win condition
            if (MTeamScores[TeamIds.Alliance] >= MAX_TEAM_SCORE)
                EndBattleground(TeamFaction.Alliance);
            else if (MTeamScores[TeamIds.Horde] >= MAX_TEAM_SCORE)
                EndBattleground(TeamFaction.Horde);
        }
    }

    public override void RemovePlayer(Player player, ObjectGuid guid, TeamFaction team) { }

    public override void Reset()
    {
        //call parent's class reset
        base.Reset();

        for (var i = 0; i < SharedConst.PvpTeamsCount; ++i)
        {
            MTeamScores[i] = 0;
            _mLastTick[i] = 0;
            _mHonorScoreTics[i] = 0;
            _mReputationScoreTics[i] = 0;
        }

        _mIsInformedNearVictory = false;
        var isBGWeekend = Global.BattlegroundMgr.IsBGWeekend(GetTypeID());
        _mHonorTics = isBGWeekend ? ABBG_WEEKEND_HONOR_TICKS : NOT_ABBG_WEEKEND_HONOR_TICKS;
        _mReputationTics = isBGWeekend ? ABBG_WEEKEND_REPUTATION_TICKS : NOT_ABBG_WEEKEND_REPUTATION_TICKS;

        for (byte i = 0; i < ABBattlegroundNodes.DYNAMIC_NODES_COUNT; ++i)
        {
            _mNodes[i] = 0;
            _mPrevNodes[i] = 0;
            _mNodeTimers[i] = 0;
            _mBannerTimers[i].Timer = 0;
        }
    }

    public override bool SetupBattleground()
    {
        var result = true;

        for (var i = 0; i < ABBattlegroundNodes.DYNAMIC_NODES_COUNT; ++i)
        {
            result &= AddObject(ABObjectTypes.BANNER_NEUTRAL + 8 * i, (uint)(NodeObjectId.BANNER0 + i), NodePositions[i], 0, 0, (float)Math.Sin(NodePositions[i].Orientation / 2), (float)Math.Cos(NodePositions[i].Orientation / 2), BattlegroundConst.RespawnOneDay);
            result &= AddObject(ABObjectTypes.BANNER_CONT_A + 8 * i, ABObjectIds.BANNER_CONT_A, NodePositions[i], 0, 0, (float)Math.Sin(NodePositions[i].Orientation / 2), (float)Math.Cos(NodePositions[i].Orientation / 2), BattlegroundConst.RespawnOneDay);
            result &= AddObject(ABObjectTypes.BANNER_CONT_H + 8 * i, ABObjectIds.BANNER_CONT_H, NodePositions[i], 0, 0, (float)Math.Sin(NodePositions[i].Orientation / 2), (float)Math.Cos(NodePositions[i].Orientation / 2), BattlegroundConst.RespawnOneDay);
            result &= AddObject(ABObjectTypes.BANNER_ALLY + 8 * i, ABObjectIds.BANNER_A, NodePositions[i], 0, 0, (float)Math.Sin(NodePositions[i].Orientation / 2), (float)Math.Cos(NodePositions[i].Orientation / 2), BattlegroundConst.RespawnOneDay);
            result &= AddObject(ABObjectTypes.BANNER_HORDE + 8 * i, ABObjectIds.BANNER_H, NodePositions[i], 0, 0, (float)Math.Sin(NodePositions[i].Orientation / 2), (float)Math.Cos(NodePositions[i].Orientation / 2), BattlegroundConst.RespawnOneDay);
            result &= AddObject(ABObjectTypes.AURA_ALLY + 8 * i, ABObjectIds.AURA_A, NodePositions[i], 0, 0, (float)Math.Sin(NodePositions[i].Orientation / 2), (float)Math.Cos(NodePositions[i].Orientation / 2), BattlegroundConst.RespawnOneDay);
            result &= AddObject(ABObjectTypes.AURA_HORDE + 8 * i, ABObjectIds.AURA_H, NodePositions[i], 0, 0, (float)Math.Sin(NodePositions[i].Orientation / 2), (float)Math.Cos(NodePositions[i].Orientation / 2), BattlegroundConst.RespawnOneDay);
            result &= AddObject(ABObjectTypes.AURA_CONTESTED + 8 * i, ABObjectIds.AURA_C, NodePositions[i], 0, 0, (float)Math.Sin(NodePositions[i].Orientation / 2), (float)Math.Cos(NodePositions[i].Orientation / 2), BattlegroundConst.RespawnOneDay);

            if (!result)
            {
                Log.Logger.Error("BatteGroundAB: Failed to spawn some object Battleground not created!");

                return false;
            }
        }

        result &= AddObject(ABObjectTypes.GATE_A, ABObjectIds.GATE_A, DoorPositions[0][0], DoorPositions[0][1], DoorPositions[0][2], DoorPositions[0][3], DoorPositions[0][4], DoorPositions[0][5], DoorPositions[0][6], DoorPositions[0][7]);
        result &= AddObject(ABObjectTypes.GATE_H, ABObjectIds.GATE_H, DoorPositions[1][0], DoorPositions[1][1], DoorPositions[1][2], DoorPositions[1][3], DoorPositions[1][4], DoorPositions[1][5], DoorPositions[1][6], DoorPositions[1][7]);

        if (!result)
        {
            Log.Logger.Error("BatteGroundAB: Failed to spawn door object Battleground not created!");

            return false;
        }

        //buffs
        for (var i = 0; i < ABBattlegroundNodes.DYNAMIC_NODES_COUNT; ++i)
        {
            result &= AddObject(ABObjectTypes.SPEEDBUFF_STABLES + 3 * i, BuffEntries[0], BuffPositions[i][0], BuffPositions[i][1], BuffPositions[i][2], BuffPositions[i][3], 0, 0, (float)Math.Sin(BuffPositions[i][3] / 2), (float)Math.Cos(BuffPositions[i][3] / 2), BattlegroundConst.RespawnOneDay);
            result &= AddObject(ABObjectTypes.SPEEDBUFF_STABLES + 3 * i + 1, BuffEntries[1], BuffPositions[i][0], BuffPositions[i][1], BuffPositions[i][2], BuffPositions[i][3], 0, 0, (float)Math.Sin(BuffPositions[i][3] / 2), (float)Math.Cos(BuffPositions[i][3] / 2), BattlegroundConst.RespawnOneDay);
            result &= AddObject(ABObjectTypes.SPEEDBUFF_STABLES + 3 * i + 2, BuffEntries[2], BuffPositions[i][0], BuffPositions[i][1], BuffPositions[i][2], BuffPositions[i][3], 0, 0, (float)Math.Sin(BuffPositions[i][3] / 2), (float)Math.Cos(BuffPositions[i][3] / 2), BattlegroundConst.RespawnOneDay);

            if (!result)
            {
                Log.Logger.Error("BatteGroundAB: Failed to spawn buff object!");

                return false;
            }
        }

        UpdateWorldState(ABWorldStates.RESOURCES_MAX, MAX_TEAM_SCORE);
        UpdateWorldState(ABWorldStates.RESOURCES_WARNING, WARNING_NEAR_VICTORY_SCORE);

        return true;
    }

    public override void StartingEventCloseDoors()
    {
        // despawn banners, auras and buffs
        for (var obj = ABObjectTypes.BANNER_NEUTRAL; obj < ABBattlegroundNodes.DYNAMIC_NODES_COUNT * 8; ++obj)
            SpawnBGObject(obj, BattlegroundConst.RespawnOneDay);

        for (var i = 0; i < ABBattlegroundNodes.DYNAMIC_NODES_COUNT * 3; ++i)
            SpawnBGObject(ABObjectTypes.SPEEDBUFF_STABLES + i, BattlegroundConst.RespawnOneDay);

        // Starting doors
        DoorClose(ABObjectTypes.GATE_A);
        DoorClose(ABObjectTypes.GATE_H);
        SpawnBGObject(ABObjectTypes.GATE_A, BattlegroundConst.RespawnImmediately);
        SpawnBGObject(ABObjectTypes.GATE_H, BattlegroundConst.RespawnImmediately);

        // Starting base spirit guides
        _NodeOccupied(ABBattlegroundNodes.SPIRIT_ALIANCE, TeamFaction.Alliance);
        _NodeOccupied(ABBattlegroundNodes.SPIRIT_HORDE, TeamFaction.Horde);
    }

    public override void StartingEventOpenDoors()
    {
        // spawn neutral banners
        for (int banner = ABObjectTypes.BANNER_NEUTRAL, i = 0; i < 5; banner += 8, ++i)
            SpawnBGObject(banner, BattlegroundConst.RespawnImmediately);

        for (var i = 0; i < ABBattlegroundNodes.DYNAMIC_NODES_COUNT; ++i)
        {
            //randomly select buff to spawn
            var buff = RandomHelper.IRand(0, 2);
            SpawnBGObject(ABObjectTypes.SPEEDBUFF_STABLES + buff + i * 3, BattlegroundConst.RespawnImmediately);
        }

        DoorOpen(ABObjectTypes.GATE_A);
        DoorOpen(ABObjectTypes.GATE_H);

        // Achievement: Let's Get This Done
        TriggerGameEvent(EVENT_START_BATTLE);
    }

    public override bool UpdatePlayerScore(Player player, ScoreType type, uint value, bool doAddHonor = true)
    {
        if (!base.UpdatePlayerScore(player, type, value, doAddHonor))
            return false;

        switch (type)
        {
            case ScoreType.BasesAssaulted:
                player.UpdateCriteria(CriteriaType.TrackedWorldStateUIModified, (uint)ABObjectives.AssaultBase);

                break;
            case ScoreType.BasesDefended:
                player.UpdateCriteria(CriteriaType.TrackedWorldStateUIModified, (uint)ABObjectives.DefendBase);

                break;
        }

        return true;
    }

    private void _CreateBanner(byte node, ABNodeStatus type, int teamIndex, bool delay)
    {
        // Just put it into the queue
        if (delay)
        {
            _mBannerTimers[node].Timer = 2000;
            _mBannerTimers[node].Type = (byte)type;
            _mBannerTimers[node].TeamIndex = (byte)teamIndex;

            return;
        }

        var obj = node * 8 + (byte)type + teamIndex;

        SpawnBGObject(obj, BattlegroundConst.RespawnImmediately);

        // handle aura with banner
        if (type == 0)
            return;

        obj = node * 8 + (type == ABNodeStatus.Occupied ? 5 + teamIndex : 7);
        SpawnBGObject(obj, BattlegroundConst.RespawnImmediately);
    }

    private void _DelBanner(byte node, ABNodeStatus type, byte teamIndex)
    {
        var obj = node * 8 + (byte)type + teamIndex;
        SpawnBGObject(obj, BattlegroundConst.RespawnOneDay);

        // handle aura with banner
        if (type == 0)
            return;

        obj = node * 8 + (type == ABNodeStatus.Occupied ? 5 + teamIndex : 7);
        SpawnBGObject(obj, BattlegroundConst.RespawnOneDay);
    }

    private void _NodeDeOccupied(byte node)
    {
        //only dynamic nodes, no start points
        if (node >= ABBattlegroundNodes.DYNAMIC_NODES_COUNT)
            return;

        //remove bonus honor aura trigger creature when node is lost
        DelCreature(node + 7); //null checks are in DelCreature! 0-6 spirit guides

        RelocateDeadPlayers(BgCreatures[node]);

        DelCreature(node);

        // buff object isn't despawned
    }

    private void _NodeOccupied(byte node, TeamFaction team)
    {
        if (!AddSpiritGuide(node, SpiritGuidePos[node], GetTeamIndexByTeamId(team)))
            Log.Logger.Error("Failed to spawn spirit guide! point: {0}, team: {1}, ", node, team);

        if (node >= ABBattlegroundNodes.DYNAMIC_NODES_COUNT) //only dynamic nodes, no start points
            return;

        byte capturedNodes = 0;

        for (byte i = 0; i < ABBattlegroundNodes.DYNAMIC_NODES_COUNT; ++i)
            if (_mNodes[i] == ABNodeStatus.Occupied + GetTeamIndexByTeamId(team) && _mNodeTimers[i] == 0)
                ++capturedNodes;

        if (capturedNodes >= 5)
            CastSpellOnTeam(BattlegroundConst.AbQuestReward5Bases, team);

        if (capturedNodes >= 4)
            CastSpellOnTeam(BattlegroundConst.AbQuestReward4Bases, team);

        var trigger = !BgCreatures[node + 7].IsEmpty ? GetBGCreature(node + 7) : null; // 0-6 spirit guides

        if (!trigger)
            trigger = AddCreature(SharedConst.WorldTrigger, node + 7, NodePositions[node], GetTeamIndexByTeamId(team));

        //add bonus honor aura trigger creature when node is accupied
        //cast bonus aura (+50% honor in 25yards)
        //aura should only apply to players who have accupied the node, set correct faction for trigger
        if (trigger)
        {
            trigger.Faction = team == TeamFaction.Alliance ? 84u : 83u;
            trigger.CastSpell(trigger, BattlegroundConst.SpellHonorableDefender25y, false);
        }
    }

    private void _SendNodeUpdate(byte node)
    {
        // Send node owner state update to refresh map icons on client
        int[] idPlusArray =
        {
            0, 2, 3, 0, 1
        };

        int[] statePlusArray =
        {
            0, 2, 0, 2, 0
        };

        if (_mPrevNodes[node] != 0)
            UpdateWorldState(NodeStates[node] + idPlusArray[(int)_mPrevNodes[node]], 0);
        else
            UpdateWorldState(NodeIcons[node], 0);

        UpdateWorldState(NodeStates[node] + idPlusArray[(byte)_mNodes[node]], 1);

        switch (node)
        {
            case ABBattlegroundNodes.NODE_STABLES:
                UpdateWorldState(ABWorldStates.STABLES_ICON_NEW, (int)_mNodes[node] + statePlusArray[(int)_mNodes[node]]);
                UpdateWorldState(ABWorldStates.STABLES_HORDE_CONTROL_STATE, _mNodes[node] == ABNodeStatus.HordeOccupied   ? 2 : _mNodes[node] == ABNodeStatus.HordeContested ? 1 : 0);
                UpdateWorldState(ABWorldStates.STABLES_ALLIANCE_CONTROL_STATE, _mNodes[node] == ABNodeStatus.AllyOccupied ? 2 : _mNodes[node] == ABNodeStatus.AllyContested  ? 1 : 0);

                break;
            case ABBattlegroundNodes.NODE_BLACKSMITH:
                UpdateWorldState(ABWorldStates.BLACKSMITH_ICON_NEW, (int)_mNodes[node] + statePlusArray[(int)_mNodes[node]]);
                UpdateWorldState(ABWorldStates.BLACKSMITH_HORDE_CONTROL_STATE, _mNodes[node] == ABNodeStatus.HordeOccupied   ? 2 : _mNodes[node] == ABNodeStatus.HordeContested ? 1 : 0);
                UpdateWorldState(ABWorldStates.BLACKSMITH_ALLIANCE_CONTROL_STATE, _mNodes[node] == ABNodeStatus.AllyOccupied ? 2 : _mNodes[node] == ABNodeStatus.AllyContested  ? 1 : 0);

                break;
            case ABBattlegroundNodes.NODE_FARM:
                UpdateWorldState(ABWorldStates.FARM_ICON_NEW, (int)_mNodes[node] + statePlusArray[(int)_mNodes[node]]);
                UpdateWorldState(ABWorldStates.FARM_HORDE_CONTROL_STATE, _mNodes[node] == ABNodeStatus.HordeOccupied   ? 2 : _mNodes[node] == ABNodeStatus.HordeContested ? 1 : 0);
                UpdateWorldState(ABWorldStates.FARM_ALLIANCE_CONTROL_STATE, _mNodes[node] == ABNodeStatus.AllyOccupied ? 2 : _mNodes[node] == ABNodeStatus.AllyContested  ? 1 : 0);

                break;
            case ABBattlegroundNodes.NODE_LUMBER_MILL:
                UpdateWorldState(ABWorldStates.LUMBER_MILL_ICON_NEW, (int)_mNodes[node] + statePlusArray[(int)_mNodes[node]]);
                UpdateWorldState(ABWorldStates.LUMBER_MILL_HORDE_CONTROL_STATE, _mNodes[node] == ABNodeStatus.HordeOccupied   ? 2 : _mNodes[node] == ABNodeStatus.HordeContested ? 1 : 0);
                UpdateWorldState(ABWorldStates.LUMBER_MILL_ALLIANCE_CONTROL_STATE, _mNodes[node] == ABNodeStatus.AllyOccupied ? 2 : _mNodes[node] == ABNodeStatus.AllyContested  ? 1 : 0);

                break;
            case ABBattlegroundNodes.NODE_GOLD_MINE:
                UpdateWorldState(ABWorldStates.GOLD_MINE_ICON_NEW, (int)_mNodes[node] + statePlusArray[(int)_mNodes[node]]);
                UpdateWorldState(ABWorldStates.GOLD_MINE_HORDE_CONTROL_STATE, _mNodes[node] == ABNodeStatus.HordeOccupied   ? 2 : _mNodes[node] == ABNodeStatus.HordeContested ? 1 : 0);
                UpdateWorldState(ABWorldStates.GOLD_MINE_ALLIANCE_CONTROL_STATE, _mNodes[node] == ABNodeStatus.AllyOccupied ? 2 : _mNodes[node] == ABNodeStatus.AllyContested  ? 1 : 0);

                break;
        }

        // How many bases each team owns
        byte ally = 0, horde = 0;

        for (byte i = 0; i < ABBattlegroundNodes.DYNAMIC_NODES_COUNT; ++i)
            if (_mNodes[i] == ABNodeStatus.AllyOccupied)
                ++ally;
            else if (_mNodes[i] == ABNodeStatus.HordeOccupied)
                ++horde;

        UpdateWorldState(ABWorldStates.OCCUPIED_BASES_ALLY, ally);
        UpdateWorldState(ABWorldStates.OCCUPIED_BASES_HORDE, horde);
    }
}