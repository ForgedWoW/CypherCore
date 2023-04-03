// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.OutdoorPVP.Zones;

internal class HellfirePeninsulaCapturePoint : OPvPCapturePoint
{
    private readonly ulong m_flagSpawnId;
    private readonly uint m_TowerType;

    public HellfirePeninsulaCapturePoint(OutdoorPvP pvp, OutdoorPvPHPTowerType type, GameObject go, ulong flagSpawnId) : base(pvp)
    {
        m_TowerType = (uint)type;
        m_flagSpawnId = flagSpawnId;

        CapturePointSpawnId = go.SpawnId;
        CapturePoint = go;
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
        var bounds = map.GameObjectBySpawnIdStore.LookupByKey(CapturePointSpawnId);

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