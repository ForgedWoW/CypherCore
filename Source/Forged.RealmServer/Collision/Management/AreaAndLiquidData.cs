// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.RealmServer.Collision;

public class AreaAndLiquidData
{
	public float FloorZ = MapConst.VMAPInvalidHeightValue;
	public AreaInfoModel? AreaInfo;
	public LiquidInfoModel? LiquidInfo;

	public struct AreaInfoModel
	{
		public int AdtId;
		public int RootId;
		public int GroupId;
		public uint MogpFlags;

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
		public uint LiquidType;
		public float Level;

		public LiquidInfoModel(uint type, float level)
		{
			LiquidType = type;
			Level = level;
		}
	}
}