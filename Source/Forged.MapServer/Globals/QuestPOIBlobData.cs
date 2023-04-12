// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.Globals;

public class QuestPOIBlobData
{
    public QuestPOIBlobData(int blobIndex, int objectiveIndex, int questObjectiveID, int questObjectID, int mapID, int uiMapID, int priority, int flags,
                            int worldEffectID, int playerConditionID, int navigationPlayerConditionID, int spawnTrackingID, List<QuestPOIBlobPoint> points, bool alwaysAllowMergingBlobs)
    {
        BlobIndex = blobIndex;
        ObjectiveIndex = objectiveIndex;
        QuestObjectiveID = questObjectiveID;
        QuestObjectID = questObjectID;
        MapID = mapID;
        UiMapID = uiMapID;
        Priority = priority;
        Flags = flags;
        WorldEffectID = worldEffectID;
        PlayerConditionID = playerConditionID;
        NavigationPlayerConditionID = navigationPlayerConditionID;
        SpawnTrackingID = spawnTrackingID;
        Points = points;
        AlwaysAllowMergingBlobs = alwaysAllowMergingBlobs;
    }

    public bool AlwaysAllowMergingBlobs { get; set; }
    public int BlobIndex { get; set; }
    public int Flags { get; set; }
    public int MapID { get; set; }
    public int NavigationPlayerConditionID { get; set; }
    public int ObjectiveIndex { get; set; }
    public int PlayerConditionID { get; set; }
    public List<QuestPOIBlobPoint> Points { get; set; }
    public int Priority { get; set; }
    public int QuestObjectID { get; set; }
    public int QuestObjectiveID { get; set; }
    public int SpawnTrackingID { get; set; }
    public int UiMapID { get; set; }
    public int WorldEffectID { get; set; }
}