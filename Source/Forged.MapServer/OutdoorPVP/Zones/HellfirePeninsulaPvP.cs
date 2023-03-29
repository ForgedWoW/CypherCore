// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Maps;
using Forged.MapServer.Networking.Packets.WorldState;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IOutdoorPvP;
using Framework.Constants;

namespace Forged.MapServer.OutdoorPVP.Zones;

internal class HellfirePeninsulaPvP : OutdoorPvP
{
    private readonly ulong[] m_towerFlagSpawnIds = new ulong[(int)OutdoorPvPHPTowerType.Num];

    // how many towers are controlled
    private uint m_AllianceTowersControlled;
    private uint m_HordeTowersControlled;

    public HellfirePeninsulaPvP(Map map) : base(map)
    {
        m_TypeId = OutdoorPvPTypes.HellfirePeninsula;
        m_AllianceTowersControlled = 0;
        m_HordeTowersControlled = 0;
    }

    public override bool SetupOutdoorPvP()
    {
        m_AllianceTowersControlled = 0;
        m_HordeTowersControlled = 0;

        // add the zones affected by the pvp buff
        for (var i = 0; i < HPConst.BuffZones.Length; ++i)
            RegisterZone(HPConst.BuffZones[i]);

        return true;
    }

    public override void OnGameObjectCreate(GameObject go)
    {
        switch (go.Entry)
        {
            case 182175:
                AddCapturePoint(new HellfirePeninsulaCapturePoint(this, OutdoorPvPHPTowerType.BrokenHill, go, m_towerFlagSpawnIds[(int)OutdoorPvPHPTowerType.BrokenHill]));

                break;
            case 182174:
                AddCapturePoint(new HellfirePeninsulaCapturePoint(this, OutdoorPvPHPTowerType.Overlook, go, m_towerFlagSpawnIds[(int)OutdoorPvPHPTowerType.Overlook]));

                break;
            case 182173:
                AddCapturePoint(new HellfirePeninsulaCapturePoint(this, OutdoorPvPHPTowerType.Stadium, go, m_towerFlagSpawnIds[(int)OutdoorPvPHPTowerType.Stadium]));

                break;
            case 183514:
                m_towerFlagSpawnIds[(int)OutdoorPvPHPTowerType.BrokenHill] = go.SpawnId;

                break;
            case 182525:
                m_towerFlagSpawnIds[(int)OutdoorPvPHPTowerType.Overlook] = go.SpawnId;

                break;
            case 183515:
                m_towerFlagSpawnIds[(int)OutdoorPvPHPTowerType.Stadium] = go.SpawnId;

                break;
            default:
                break;
        }

        base.OnGameObjectCreate(go);
    }

    public override void HandlePlayerEnterZone(Player player, uint zone)
    {
        // add buffs
        if (player.Team == TeamFaction.Alliance)
        {
            if (m_AllianceTowersControlled >= 3)
                player.CastSpell(player, OutdoorPvPHPSpells.AllianceBuff, true);
        }
        else
        {
            if (m_HordeTowersControlled >= 3)
                player.CastSpell(player, OutdoorPvPHPSpells.HordeBuff, true);
        }

        base.HandlePlayerEnterZone(player, zone);
    }

    public override void HandlePlayerLeaveZone(Player player, uint zone)
    {
        // remove buffs
        if (player.Team == TeamFaction.Alliance)
            player.RemoveAura(OutdoorPvPHPSpells.AllianceBuff);
        else
            player.RemoveAura(OutdoorPvPHPSpells.HordeBuff);

        base.HandlePlayerLeaveZone(player, zone);
    }

    public override bool Update(uint diff)
    {
        var changed = base.Update(diff);

        if (changed)
        {
            if (m_AllianceTowersControlled == 3)
            {
                TeamApplyBuff(TeamIds.Alliance, OutdoorPvPHPSpells.AllianceBuff, OutdoorPvPHPSpells.HordeBuff);
            }
            else if (m_HordeTowersControlled == 3)
            {
                TeamApplyBuff(TeamIds.Horde, OutdoorPvPHPSpells.HordeBuff, OutdoorPvPHPSpells.AllianceBuff);
            }
            else
            {
                TeamCastSpell(TeamIds.Alliance, -(int)OutdoorPvPHPSpells.AllianceBuff);
                TeamCastSpell(TeamIds.Horde, -(int)OutdoorPvPHPSpells.HordeBuff);
            }

            SetWorldState(OutdoorPvPHPWorldStates.Count_A, (int)m_AllianceTowersControlled);
            SetWorldState(OutdoorPvPHPWorldStates.Count_H, (int)m_HordeTowersControlled);
        }

        return changed;
    }

    public override void SendRemoveWorldStates(Player player)
    {
        InitWorldStates initWorldStates = new()
        {
            MapID = player.Location.MapId,
            AreaID = player.Location.Zone,
            SubareaID = player.Location.Area
        };

        initWorldStates.AddState(OutdoorPvPHPWorldStates.Display_A, 0);
        initWorldStates.AddState(OutdoorPvPHPWorldStates.Display_H, 0);
        initWorldStates.AddState(OutdoorPvPHPWorldStates.Count_H, 0);
        initWorldStates.AddState(OutdoorPvPHPWorldStates.Count_A, 0);

        for (var i = 0; i < (int)OutdoorPvPHPTowerType.Num; ++i)
        {
            initWorldStates.AddState(HPConst.Map_N[i], 0);
            initWorldStates.AddState(HPConst.Map_A[i], 0);
            initWorldStates.AddState(HPConst.Map_H[i], 0);
        }

        player.SendPacket(initWorldStates);
    }

    public override void HandleKillImpl(Player killer, Unit killed)
    {
        if (!killed.IsTypeId(TypeId.Player))
            return;

        if (killer.Team == TeamFaction.Alliance && killed.AsPlayer.Team != TeamFaction.Alliance)
            killer.CastSpell(killer, OutdoorPvPHPSpells.AlliancePlayerKillReward, true);
        else if (killer.Team == TeamFaction.Horde && killed.AsPlayer.Team != TeamFaction.Horde)
            killer.CastSpell(killer, OutdoorPvPHPSpells.HordePlayerKillReward, true);
    }

    public uint GetAllianceTowersControlled()
    {
        return m_AllianceTowersControlled;
    }

    public void SetAllianceTowersControlled(uint count)
    {
        m_AllianceTowersControlled = count;
    }

    public uint GetHordeTowersControlled()
    {
        return m_HordeTowersControlled;
    }

    public void SetHordeTowersControlled(uint count)
    {
        m_HordeTowersControlled = count;
    }
}

internal class HellfirePeninsulaCapturePoint : OPvPCapturePoint
{
    private readonly uint m_TowerType;
    private readonly ulong m_flagSpawnId;

    public HellfirePeninsulaCapturePoint(OutdoorPvP pvp, OutdoorPvPHPTowerType type, GameObject go, ulong flagSpawnId) : base(pvp)
    {
        m_TowerType = (uint)type;
        m_flagSpawnId = flagSpawnId;

        m_capturePointSpawnId = go.SpawnId;
        m_capturePoint = go;
        SetCapturePointData(go.Entry);
    }

    public override void ChangeState()
    {
        uint field = 0;

        switch (OldState)
        {
            case ObjectiveStates.Neutral:
                field = HPConst.Map_N[m_TowerType];

                break;
            case ObjectiveStates.Alliance:
                field = HPConst.Map_A[m_TowerType];
                var alliance_towers = ((HellfirePeninsulaPvP)PvP).GetAllianceTowersControlled();

                if (alliance_towers != 0)
                    ((HellfirePeninsulaPvP)PvP).SetAllianceTowersControlled(--alliance_towers);

                break;
            case ObjectiveStates.Horde:
                field = HPConst.Map_H[m_TowerType];
                var horde_towers = ((HellfirePeninsulaPvP)PvP).GetHordeTowersControlled();

                if (horde_towers != 0)
                    ((HellfirePeninsulaPvP)PvP).SetHordeTowersControlled(--horde_towers);

                break;
            case ObjectiveStates.NeutralAllianceChallenge:
                field = HPConst.Map_N[m_TowerType];

                break;
            case ObjectiveStates.NeutralHordeChallenge:
                field = HPConst.Map_N[m_TowerType];

                break;
            case ObjectiveStates.AllianceHordeChallenge:
                field = HPConst.Map_A[m_TowerType];

                break;
            case ObjectiveStates.HordeAllianceChallenge:
                field = HPConst.Map_H[m_TowerType];

                break;
        }

        // send world state update
        if (field != 0)
        {
            PvP.SetWorldState((int)field, 0);
            field = 0;
        }

        uint artkit = 21;
        var artkit2 = HPConst.TowerArtKit_N[m_TowerType];

        switch (State)
        {
            case ObjectiveStates.Neutral:
                field = HPConst.Map_N[m_TowerType];

                break;
            case ObjectiveStates.Alliance:
            {
                field = HPConst.Map_A[m_TowerType];
                artkit = 2;
                artkit2 = HPConst.TowerArtKit_A[m_TowerType];
                var alliance_towers = ((HellfirePeninsulaPvP)PvP).GetAllianceTowersControlled();

                if (alliance_towers < 3)
                    ((HellfirePeninsulaPvP)PvP).SetAllianceTowersControlled(++alliance_towers);

                PvP.SendDefenseMessage(HPConst.BuffZones[0], HPConst.LangCapture_A[m_TowerType]);

                break;
            }
            case ObjectiveStates.Horde:
            {
                field = HPConst.Map_H[m_TowerType];
                artkit = 1;
                artkit2 = HPConst.TowerArtKit_H[m_TowerType];
                var horde_towers = ((HellfirePeninsulaPvP)PvP).GetHordeTowersControlled();

                if (horde_towers < 3)
                    ((HellfirePeninsulaPvP)PvP).SetHordeTowersControlled(++horde_towers);

                PvP.SendDefenseMessage(HPConst.BuffZones[0], HPConst.LangCapture_H[m_TowerType]);

                break;
            }
            case ObjectiveStates.NeutralAllianceChallenge:
                field = HPConst.Map_N[m_TowerType];

                break;
            case ObjectiveStates.NeutralHordeChallenge:
                field = HPConst.Map_N[m_TowerType];

                break;
            case ObjectiveStates.AllianceHordeChallenge:
                field = HPConst.Map_A[m_TowerType];
                artkit = 2;
                artkit2 = HPConst.TowerArtKit_A[m_TowerType];

                break;
            case ObjectiveStates.HordeAllianceChallenge:
                field = HPConst.Map_H[m_TowerType];
                artkit = 1;
                artkit2 = HPConst.TowerArtKit_H[m_TowerType];

                break;
        }

        var map = Global.MapMgr.FindMap(530, 0);
        var bounds = map.GameObjectBySpawnIdStore.LookupByKey(m_capturePointSpawnId);

        foreach (var go in bounds)
            go.GoArtKit = artkit;

        bounds = map.GameObjectBySpawnIdStore.LookupByKey(m_flagSpawnId);

        foreach (var go in bounds)
            go.GoArtKit = artkit2;

        // send world state update
        if (field != 0)
            PvP.SetWorldState((int)field, 1);

        // complete quest objective
        if (State == ObjectiveStates.Alliance || State == ObjectiveStates.Horde)
            SendObjectiveComplete(HPConst.CreditMarker[m_TowerType], ObjectGuid.Empty);
    }
}

[Script]
internal class OutdoorPvP_hellfire_peninsula : ScriptObjectAutoAddDBBound, IOutdoorPvPGetOutdoorPvP
{
    public OutdoorPvP_hellfire_peninsula() : base("outdoorpvp_hp") { }

    public OutdoorPvP GetOutdoorPvP(Map map)
    {
        return new HellfirePeninsulaPvP(map);
    }
}

internal struct HPConst
{
    public static uint[] LangCapture_A =
    {
        DefenseMessages.BrokenHillTakenAlliance, DefenseMessages.OverlookTakenAlliance, DefenseMessages.StadiumTakenAlliance
    };

    public static uint[] LangCapture_H =
    {
        DefenseMessages.BrokenHillTakenHorde, DefenseMessages.OverlookTakenHorde, DefenseMessages.StadiumTakenHorde
    };

    public static uint[] Map_N =
    {
        2485, 2482, 0x9a8
    };

    public static uint[] Map_A =
    {
        2483, 2480, 2471
    };

    public static uint[] Map_H =
    {
        2484, 2481, 2470
    };

    public static uint[] TowerArtKit_A =
    {
        65, 62, 67
    };

    public static uint[] TowerArtKit_H =
    {
        64, 61, 68
    };

    public static uint[] TowerArtKit_N =
    {
        66, 63, 69
    };

    //  HP, citadel, ramparts, blood furnace, shattered halls, mag's lair
    public static uint[] BuffZones =
    {
        3483, 3563, 3562, 3713, 3714, 3836
    };

    public static uint[] CreditMarker =
    {
        19032, 19028, 19029
    };

    public static uint[] CapturePointEventEnter =
    {
        11404, 11396, 11388
    };

    public static uint[] CapturePointEventLeave =
    {
        11403, 11395, 11387
    };
}

internal struct DefenseMessages
{
    public const uint OverlookTakenAlliance = 14841;   // '|cffffff00The Overlook has been taken by the Alliance!|r'
    public const uint OverlookTakenHorde = 14842;      // '|cffffff00The Overlook has been taken by the Horde!|r'
    public const uint StadiumTakenAlliance = 14843;    // '|cffffff00The Stadium has been taken by the Alliance!|r'
    public const uint StadiumTakenHorde = 14844;       // '|cffffff00The Stadium has been taken by the Horde!|r'
    public const uint BrokenHillTakenAlliance = 14845; // '|cffffff00Broken Hill has been taken by the Alliance!|r'
    public const uint BrokenHillTakenHorde = 14846;    // '|cffffff00Broken Hill has been taken by the Horde!|r'
}

internal struct OutdoorPvPHPSpells
{
    public const uint AlliancePlayerKillReward = 32155;
    public const uint HordePlayerKillReward = 32158;
    public const uint AllianceBuff = 32071;
    public const uint HordeBuff = 32049;
}

internal enum OutdoorPvPHPTowerType
{
    BrokenHill = 0,
    Overlook = 1,
    Stadium = 2,
    Num = 3
}

internal struct OutdoorPvPHPWorldStates
{
    public const int Display_A = 0x9ba;
    public const int Display_H = 0x9b9;

    public const int Count_H = 0x9ae;
    public const int Count_A = 0x9ac;
}