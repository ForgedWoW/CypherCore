// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.Scenarios;

public class ScenarioPOI
{
	public int BlobIndex;
	public int MapID;
	public int UiMapID;
	public int Priority;
	public int Flags;
	public int WorldEffectID;
	public int PlayerConditionID;
	public int NavigationPlayerConditionID;
	public List<ScenarioPOIPoint> Points = new();

	public ScenarioPOI(int blobIndex, int mapID, int uiMapID, int priority, int flags, int worldEffectID, int playerConditionID, int navigationPlayerConditionID, List<ScenarioPOIPoint> points)
	{
		BlobIndex = blobIndex;
		MapID = mapID;
		UiMapID = uiMapID;
		Priority = priority;
		Flags = flags;
		WorldEffectID = worldEffectID;
		PlayerConditionID = playerConditionID;
		NavigationPlayerConditionID = navigationPlayerConditionID;
		Points = points;
	}
}