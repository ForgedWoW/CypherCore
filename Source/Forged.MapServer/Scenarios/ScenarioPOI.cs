// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.Scenarios;

public class ScenarioPOI
{
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

    public int BlobIndex { get; set; }
    public int Flags { get; set; }
    public int MapID { get; set; }
    public int NavigationPlayerConditionID { get; set; }
    public int PlayerConditionID { get; set; }
    public List<ScenarioPOIPoint> Points { get; set; }
    public int Priority { get; set; }
    public int UiMapID { get; set; }
    public int WorldEffectID { get; set; }
}