// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Common.Networking;

namespace Game.Entities;

public class VisualAnim : BaseUpdateData<AreaTrigger>
{
	public UpdateField<bool> Field_C = new(0, 1);
	public UpdateField<int> AnimationDataID = new(0, 2);
	public UpdateField<uint> AnimKitID = new(0, 3);
	public UpdateField<uint> AnimProgress = new(0, 4);

	public VisualAnim() : base(0, TypeId.AreaTrigger, 5) { }

	public void WriteCreate(WorldPacket data, AreaTrigger owner, Player receiver)
	{
		data.WriteInt32(AnimationDataID);
		data.WriteUInt32(AnimKitID);
		data.WriteUInt32(AnimProgress);
		data.WriteBit(Field_C);
		data.FlushBits();
	}

	public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, AreaTrigger owner, Player receiver)
	{
		var changesMask = ChangesMask;

		if (ignoreChangesMask)
			changesMask.SetAll();

		data.WriteBits(changesMask.GetBlock(0), 5);

		if (changesMask[0])
			if (changesMask[1])
				data.WriteBit(Field_C);

		data.FlushBits();

		if (changesMask[0])
		{
			if (changesMask[2])
				data.WriteInt32(AnimationDataID);

			if (changesMask[3])
				data.WriteUInt32(AnimKitID);

			if (changesMask[4])
				data.WriteUInt32(AnimProgress);
		}

		data.FlushBits();
	}

	public override void ClearChangesMask()
	{
		ClearChangesMask(Field_C);
		ClearChangesMask(AnimationDataID);
		ClearChangesMask(AnimKitID);
		ClearChangesMask(AnimProgress);
		ChangesMask.ResetAll();
	}
}