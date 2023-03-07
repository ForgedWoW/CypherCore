// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Maps.Grids;

namespace Game.Maps;

public class PositionFullTerrainStatus
{
	public struct AreaInfoModel
	{
		public int AdtId;
		public int RootId;
		public int GroupId;
		public uint MogpFlags;

		public AreaInfoModel(int adtId, int rootId, int groupId, uint flags)
		{
			AdtId     = adtId;
			RootId    = rootId;
			GroupId   = groupId;
			MogpFlags = flags;
		}
	}

	public uint AreaId { get; set; }
	public float FloorZ { get; set; }
	public bool Outdoors { get; set; } = true;
	public ZLiquidStatus LiquidStatus { get; set; }
	public AreaInfoModel? AreaInfo { get; set; }
	public LiquidData LiquidInfo { get; set; }
}