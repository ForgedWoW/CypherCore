// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Common.Networking;
using Game.Common.Networking.Packets.Garrison;

namespace Forged.RealmServer;

public partial class WorldSession
{
	[WorldPacketHandler(ClientOpcodes.GarrisonPurchaseBuilding)]
	void HandleGarrisonPurchaseBuilding(GarrisonPurchaseBuilding garrisonPurchaseBuilding)
	{
		if (!_player.GetNPCIfCanInteractWith(garrisonPurchaseBuilding.NpcGUID, NPCFlags.None, NPCFlags2.GarrisonArchitect))
			return;

		var garrison = _player.Garrison;

		if (garrison != null)
			garrison.PlaceBuilding(garrisonPurchaseBuilding.PlotInstanceID, garrisonPurchaseBuilding.BuildingID);
	}

	[WorldPacketHandler(ClientOpcodes.GarrisonCancelConstruction)]
	void HandleGarrisonCancelConstruction(GarrisonCancelConstruction garrisonCancelConstruction)
	{
		if (!_player.GetNPCIfCanInteractWith(garrisonCancelConstruction.NpcGUID, NPCFlags.None, NPCFlags2.GarrisonArchitect))
			return;

		var garrison = _player.Garrison;

		if (garrison != null)
			garrison.CancelBuildingConstruction(garrisonCancelConstruction.PlotInstanceID);
	}

	[WorldPacketHandler(ClientOpcodes.GarrisonRequestBlueprintAndSpecializationData)]
	void HandleGarrisonRequestBlueprintAndSpecializationData(GarrisonRequestBlueprintAndSpecializationData garrisonRequestBlueprintAndSpecializationData)
	{
		var garrison = _player.Garrison;

		if (garrison != null)
			garrison.SendBlueprintAndSpecializationData();
	}

	[WorldPacketHandler(ClientOpcodes.GarrisonGetMapData)]
	void HandleGarrisonGetMapData(GarrisonGetMapData garrisonGetMapData)
	{
		var garrison = _player.Garrison;

		if (garrison != null)
			garrison.SendMapData(_player);
	}
}