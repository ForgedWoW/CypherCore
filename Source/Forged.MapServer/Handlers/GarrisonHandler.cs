// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Garrison;
using Framework.Constants;
using Game.Common.Handlers;

namespace Forged.MapServer.Handlers;

public class GarrisonHandler : IWorldSessionHandler
{
	[WorldPacketHandler(ClientOpcodes.GetGarrisonInfo)]
    private void HandleGetGarrisonInfo(GetGarrisonInfo getGarrisonInfo)
	{
		var garrison = _player.Garrison;

		if (garrison != null)
			garrison.SendInfo();
	}

	[WorldPacketHandler(ClientOpcodes.GarrisonPurchaseBuilding)]
    private void HandleGarrisonPurchaseBuilding(GarrisonPurchaseBuilding garrisonPurchaseBuilding)
	{
		if (!_player.GetNPCIfCanInteractWith(garrisonPurchaseBuilding.NpcGUID, NPCFlags.None, NPCFlags2.GarrisonArchitect))
			return;

		var garrison = _player.Garrison;

		if (garrison != null)
			garrison.PlaceBuilding(garrisonPurchaseBuilding.PlotInstanceID, garrisonPurchaseBuilding.BuildingID);
	}

	[WorldPacketHandler(ClientOpcodes.GarrisonCancelConstruction)]
    private void HandleGarrisonCancelConstruction(GarrisonCancelConstruction garrisonCancelConstruction)
	{
		if (!_player.GetNPCIfCanInteractWith(garrisonCancelConstruction.NpcGUID, NPCFlags.None, NPCFlags2.GarrisonArchitect))
			return;

		var garrison = _player.Garrison;

		if (garrison != null)
			garrison.CancelBuildingConstruction(garrisonCancelConstruction.PlotInstanceID);
	}

	[WorldPacketHandler(ClientOpcodes.GarrisonRequestBlueprintAndSpecializationData)]
    private void HandleGarrisonRequestBlueprintAndSpecializationData(GarrisonRequestBlueprintAndSpecializationData garrisonRequestBlueprintAndSpecializationData)
	{
		var garrison = _player.Garrison;

		if (garrison != null)
			garrison.SendBlueprintAndSpecializationData();
	}

	[WorldPacketHandler(ClientOpcodes.GarrisonGetMapData)]
    private void HandleGarrisonGetMapData(GarrisonGetMapData garrisonGetMapData)
	{
		var garrison = _player.Garrison;

		if (garrison != null)
			garrison.SendMapData(_player);
	}
}