// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Maps.Grids;
using Framework.Constants;

namespace Forged.MapServer.Maps;

public class PositionFullTerrainStatus
{
    public uint AreaId { get; set; }
    public AreaInfoModel? AreaInfo { get; set; }
    public float FloorZ { get; set; }
    public LiquidData LiquidInfo { get; set; }
    public ZLiquidStatus LiquidStatus { get; set; }
    public bool Outdoors { get; set; } = true;

    public struct AreaInfoModel
    {
        public int AdtId;
        public int GroupId;
        public uint MogpFlags;
        public int RootId;

        public AreaInfoModel(int adtId, int rootId, int groupId, uint flags)
        {
            AdtId = adtId;
            RootId = rootId;
            GroupId = groupId;
            MogpFlags = flags;
        }
    }
}