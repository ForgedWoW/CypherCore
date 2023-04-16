// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.OutdoorPVP.Zones;

internal class HellfirePeninsulaCapturePoint : OPvPCapturePoint
{
    private readonly ulong _mFlagSpawnId;
    private readonly uint _mTowerType;

    public HellfirePeninsulaCapturePoint(OutdoorPvP pvp, OutdoorPvPhpTowerType type, GameObject go, ulong flagSpawnId) : base(pvp)
    {
        _mTowerType = (uint)type;
        _mFlagSpawnId = flagSpawnId;

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
                field = HpConst.MapN[_mTowerType];

                break;
            case ObjectiveStates.Alliance:
                field = HpConst.MapA[_mTowerType];
                var allianceTowers = ((HellfirePeninsulaPvP)PvP).GetAllianceTowersControlled();

                if (allianceTowers != 0)
                    ((HellfirePeninsulaPvP)PvP).SetAllianceTowersControlled(--allianceTowers);

                break;
            case ObjectiveStates.Horde:
                field = HpConst.MapH[_mTowerType];
                var hordeTowers = ((HellfirePeninsulaPvP)PvP).GetHordeTowersControlled();

                if (hordeTowers != 0)
                    ((HellfirePeninsulaPvP)PvP).SetHordeTowersControlled(--hordeTowers);

                break;
            case ObjectiveStates.NeutralAllianceChallenge:
                field = HpConst.MapN[_mTowerType];

                break;
            case ObjectiveStates.NeutralHordeChallenge:
                field = HpConst.MapN[_mTowerType];

                break;
            case ObjectiveStates.AllianceHordeChallenge:
                field = HpConst.MapA[_mTowerType];

                break;
            case ObjectiveStates.HordeAllianceChallenge:
                field = HpConst.MapH[_mTowerType];

                break;
        }

        // send world state update
        if (field != 0)
        {
            PvP.SetWorldState((int)field, 0);
            field = 0;
        }

        uint artkit = 21;
        var artkit2 = HpConst.TowerArtKitN[_mTowerType];

        switch (State)
        {
            case ObjectiveStates.Neutral:
                field = HpConst.MapN[_mTowerType];

                break;
            case ObjectiveStates.Alliance:
            {
                field = HpConst.MapA[_mTowerType];
                artkit = 2;
                artkit2 = HpConst.TowerArtKitA[_mTowerType];
                var allianceTowers = ((HellfirePeninsulaPvP)PvP).GetAllianceTowersControlled();

                if (allianceTowers < 3)
                    ((HellfirePeninsulaPvP)PvP).SetAllianceTowersControlled(++allianceTowers);

                PvP.SendDefenseMessage(HpConst.BuffZones[0], HpConst.LangCaptureA[_mTowerType]);

                break;
            }
            case ObjectiveStates.Horde:
            {
                field = HpConst.MapH[_mTowerType];
                artkit = 1;
                artkit2 = HpConst.TowerArtKitH[_mTowerType];
                var hordeTowers = ((HellfirePeninsulaPvP)PvP).GetHordeTowersControlled();

                if (hordeTowers < 3)
                    ((HellfirePeninsulaPvP)PvP).SetHordeTowersControlled(++hordeTowers);

                PvP.SendDefenseMessage(HpConst.BuffZones[0], HpConst.LangCaptureH[_mTowerType]);

                break;
            }
            case ObjectiveStates.NeutralAllianceChallenge:
                field = HpConst.MapN[_mTowerType];

                break;
            case ObjectiveStates.NeutralHordeChallenge:
                field = HpConst.MapN[_mTowerType];

                break;
            case ObjectiveStates.AllianceHordeChallenge:
                field = HpConst.MapA[_mTowerType];
                artkit = 2;
                artkit2 = HpConst.TowerArtKitA[_mTowerType];

                break;
            case ObjectiveStates.HordeAllianceChallenge:
                field = HpConst.MapH[_mTowerType];
                artkit = 1;
                artkit2 = HpConst.TowerArtKitH[_mTowerType];

                break;
        }

        var map = PvP.Map.MapManager.FindMap(530, 0);
        var bounds = map.GameObjectBySpawnIdStore.LookupByKey(CapturePointSpawnId);

        foreach (var go in bounds)
            go.GoArtKit = artkit;

        bounds = map.GameObjectBySpawnIdStore.LookupByKey(_mFlagSpawnId);

        foreach (var go in bounds)
            go.GoArtKit = artkit2;

        // send world state update
        if (field != 0)
            PvP.SetWorldState((int)field, 1);

        // complete quest objective
        if (State is ObjectiveStates.Alliance or ObjectiveStates.Horde)
            SendObjectiveComplete(HpConst.CreditMarker[_mTowerType], ObjectGuid.Empty);
    }
}