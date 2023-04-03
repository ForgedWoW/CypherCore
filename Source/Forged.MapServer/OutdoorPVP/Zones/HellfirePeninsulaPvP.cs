// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Maps;
using Forged.MapServer.Networking.Packets.WorldState;
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

    public uint GetAllianceTowersControlled()
    {
        return m_AllianceTowersControlled;
    }

    public uint GetHordeTowersControlled()
    {
        return m_HordeTowersControlled;
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

    public void SetAllianceTowersControlled(uint count)
    {
        m_AllianceTowersControlled = count;
    }

    public void SetHordeTowersControlled(uint count)
    {
        m_HordeTowersControlled = count;
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
}