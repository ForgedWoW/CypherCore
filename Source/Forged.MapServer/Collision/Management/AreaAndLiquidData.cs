// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Collision.Management;

public class AreaAndLiquidData
{
    public AreaInfoModel? AreaInfo;
    public float FloorZ = MapConst.VMAPInvalidHeightValue;
    public LiquidInfoModel? LiquidInfo;

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

    public struct LiquidInfoModel
    {
        public float Level;
        public uint LiquidType;
        public LiquidInfoModel(uint type, float level)
        {
            LiquidType = type;
            Level = level;
        }
    }
}