// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Garrison;
using Forged.MapServer.Server;
using Framework.Constants;
using Game.Common.Handlers;

// ReSharper disable UnusedMember.Local

namespace Forged.MapServer.OpCodeHandlers;

public class GarrisonHandler : IWorldSessionHandler
{
    private readonly WorldSession _session;

    public GarrisonHandler(WorldSession session)
    {
        _session = session;
    }

    [WorldPacketHandler(ClientOpcodes.GarrisonCancelConstruction)]
    private void HandleGarrisonCancelConstruction(GarrisonCancelConstruction garrisonCancelConstruction)
    {
        if (_session.Player.GetNPCIfCanInteractWith(garrisonCancelConstruction.NpcGUID, NPCFlags.None, NPCFlags2.GarrisonArchitect) == null)
            return;

        var garrison = _session.Player.Garrison;

        garrison?.CancelBuildingConstruction(garrisonCancelConstruction.PlotInstanceID);
    }

    [WorldPacketHandler(ClientOpcodes.GarrisonGetMapData)]
    private void HandleGarrisonGetMapData(GarrisonGetMapData garrisonGetMapData)
    {
        if (garrisonGetMapData != null)
            _session.Player.Garrison?.SendMapData(_session.Player);
    }

    [WorldPacketHandler(ClientOpcodes.GarrisonPurchaseBuilding)]
    private void HandleGarrisonPurchaseBuilding(GarrisonPurchaseBuilding garrisonPurchaseBuilding)
    {
        if (_session.Player.GetNPCIfCanInteractWith(garrisonPurchaseBuilding.NpcGUID, NPCFlags.None, NPCFlags2.GarrisonArchitect) == null)
            return;

        _session.Player.Garrison?.PlaceBuilding(garrisonPurchaseBuilding.PlotInstanceID, garrisonPurchaseBuilding.BuildingID);
    }

    [WorldPacketHandler(ClientOpcodes.GarrisonRequestBlueprintAndSpecializationData)]
    private void HandleGarrisonRequestBlueprintAndSpecializationData(GarrisonRequestBlueprintAndSpecializationData garrisonRequestBlueprintAndSpecializationData)
    {
        if (garrisonRequestBlueprintAndSpecializationData != null)
            _session.Player.Garrison?.SendBlueprintAndSpecializationData();
    }

    [WorldPacketHandler(ClientOpcodes.GetGarrisonInfo)]
    private void HandleGetGarrisonInfo(GetGarrisonInfo getGarrisonInfo)
    {
        if (getGarrisonInfo != null)
            _session.Player.Garrison?.SendInfo();
    }
}