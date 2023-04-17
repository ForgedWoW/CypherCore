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
    private readonly ulong[] _mTowerFlagSpawnIds = new ulong[(int)OutdoorPvPhpTowerType.Num];

    // how many towers are controlled
    private uint _mAllianceTowersControlled;
    private uint _mHordeTowersControlled;

    public HellfirePeninsulaPvP(Map map) : base(map)
    {
        TypeId = OutdoorPvPTypes.HellfirePeninsula;
        _mAllianceTowersControlled = 0;
        _mHordeTowersControlled = 0;
    }

    public uint GetAllianceTowersControlled()
    {
        return _mAllianceTowersControlled;
    }

    public uint GetHordeTowersControlled()
    {
        return _mHordeTowersControlled;
    }

    public override void HandleKillImpl(Player killer, Unit killed)
    {
        if (!killed.IsTypeId(Framework.Constants.TypeId.Player))
            return;

        if (killer.Team == TeamFaction.Alliance && killed.AsPlayer.Team != TeamFaction.Alliance)
            killer.SpellFactory.CastSpell(killer, OutdoorPvPhpSpells.ALLIANCE_PLAYER_KILL_REWARD, true);
        else if (killer.Team == TeamFaction.Horde && killed.AsPlayer.Team != TeamFaction.Horde)
            killer.SpellFactory.CastSpell(killer, OutdoorPvPhpSpells.HORDE_PLAYER_KILL_REWARD, true);
    }

    public override void HandlePlayerEnterZone(Player player, uint zone)
    {
        // add buffs
        if (player.Team == TeamFaction.Alliance)
        {
            if (_mAllianceTowersControlled >= 3)
                player.SpellFactory.CastSpell(player, OutdoorPvPhpSpells.ALLIANCE_BUFF, true);
        }
        else
        {
            if (_mHordeTowersControlled >= 3)
                player.SpellFactory.CastSpell(player, OutdoorPvPhpSpells.HORDE_BUFF, true);
        }

        base.HandlePlayerEnterZone(player, zone);
    }

    public override void HandlePlayerLeaveZone(Player player, uint zone)
    {
        // remove buffs
        player.RemoveAura(player.Team == TeamFaction.Alliance ? OutdoorPvPhpSpells.ALLIANCE_BUFF : OutdoorPvPhpSpells.HORDE_BUFF);

        base.HandlePlayerLeaveZone(player, zone);
    }

    public override void OnGameObjectCreate(GameObject go)
    {
        switch (go.Entry)
        {
            case 182175:
                AddCapturePoint(new HellfirePeninsulaCapturePoint(this, OutdoorPvPhpTowerType.BrokenHill, go, _mTowerFlagSpawnIds[(int)OutdoorPvPhpTowerType.BrokenHill]));

                break;
            case 182174:
                AddCapturePoint(new HellfirePeninsulaCapturePoint(this, OutdoorPvPhpTowerType.Overlook, go, _mTowerFlagSpawnIds[(int)OutdoorPvPhpTowerType.Overlook]));

                break;
            case 182173:
                AddCapturePoint(new HellfirePeninsulaCapturePoint(this, OutdoorPvPhpTowerType.Stadium, go, _mTowerFlagSpawnIds[(int)OutdoorPvPhpTowerType.Stadium]));

                break;
            case 183514:
                _mTowerFlagSpawnIds[(int)OutdoorPvPhpTowerType.BrokenHill] = go.SpawnId;

                break;
            case 182525:
                _mTowerFlagSpawnIds[(int)OutdoorPvPhpTowerType.Overlook] = go.SpawnId;

                break;
            case 183515:
                _mTowerFlagSpawnIds[(int)OutdoorPvPhpTowerType.Stadium] = go.SpawnId;

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

        initWorldStates.AddState(OutdoorPvPhpWorldStates.DISPLAY_A, 0);
        initWorldStates.AddState(OutdoorPvPhpWorldStates.DISPLAY_H, 0);
        initWorldStates.AddState(OutdoorPvPhpWorldStates.COUNT_H, 0);
        initWorldStates.AddState(OutdoorPvPhpWorldStates.COUNT_A, 0);

        for (var i = 0; i < (int)OutdoorPvPhpTowerType.Num; ++i)
        {
            initWorldStates.AddState(HpConst.MapN[i], 0);
            initWorldStates.AddState(HpConst.MapA[i], 0);
            initWorldStates.AddState(HpConst.MapH[i], 0);
        }

        player.SendPacket(initWorldStates);
    }

    public void SetAllianceTowersControlled(uint count)
    {
        _mAllianceTowersControlled = count;
    }

    public void SetHordeTowersControlled(uint count)
    {
        _mHordeTowersControlled = count;
    }

    public override bool SetupOutdoorPvP()
    {
        _mAllianceTowersControlled = 0;
        _mHordeTowersControlled = 0;

        // add the zones affected by the pvp buff
        foreach (var zone in HpConst.BuffZones)
            RegisterZone(zone);

        return true;
    }

    public override bool Update(uint diff)
    {
        var changed = base.Update(diff);

        if (!changed)
            return false;

        if (_mAllianceTowersControlled == 3)
            TeamApplyBuff(TeamIds.Alliance, OutdoorPvPhpSpells.ALLIANCE_BUFF, OutdoorPvPhpSpells.HORDE_BUFF);
        else if (_mHordeTowersControlled == 3)
            TeamApplyBuff(TeamIds.Horde, OutdoorPvPhpSpells.HORDE_BUFF, OutdoorPvPhpSpells.ALLIANCE_BUFF);
        else
        {
            TeamCastSpell(TeamIds.Alliance, -(int)OutdoorPvPhpSpells.ALLIANCE_BUFF);
            TeamCastSpell(TeamIds.Horde, -(int)OutdoorPvPhpSpells.HORDE_BUFF);
        }

        SetWorldState(OutdoorPvPhpWorldStates.COUNT_A, (int)_mAllianceTowersControlled);
        SetWorldState(OutdoorPvPhpWorldStates.COUNT_H, (int)_mHordeTowersControlled);

        return true;
    }
}