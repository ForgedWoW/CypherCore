// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.Globals;

public class QuestPOIBlobData
{
    public int BlobIndex;
    public int ObjectiveIndex;
    public int QuestObjectiveID;
    public int QuestObjectID;
    public int MapID;
    public int UiMapID;
    public int Priority;
    public int Flags;
    public int WorldEffectID;
    public int PlayerConditionID;
    public int NavigationPlayerConditionID;
    public int SpawnTrackingID;
    public List<QuestPOIBlobPoint> Points;
    public bool AlwaysAllowMergingBlobs;

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
}