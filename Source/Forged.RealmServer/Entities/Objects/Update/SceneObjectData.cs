// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Forged.RealmServer.Networking;

namespace Forged.RealmServer.Entities;

public class SceneObjectData : BaseUpdateData<WorldObject>
{
	public UpdateField<int> ScriptPackageID = new(0, 1);
	public UpdateField<uint> RndSeedVal = new(0, 2);
	public UpdateField<ObjectGuid> CreatedBy = new(0, 3);
	public UpdateField<uint> SceneType = new(0, 4);

	public SceneObjectData() : base(5) { }

	public void WriteCreate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, WorldObject owner, Player receiver)
	{
		data.WriteInt32(ScriptPackageID);
		data.WriteUInt32(RndSeedVal);
		data.WritePackedGuid(CreatedBy);
		data.WriteUInt32(SceneType);
	}

	public void WriteUpdate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, WorldObject owner, Player receiver)
	{
		WriteUpdate(data, ChangesMask, false, owner, receiver);
	}

	public void WriteUpdate(WorldPacket data, UpdateMask changesMask, bool ignoreNestedChangesMask, WorldObject owner, Player receiver)
	{
		data.WriteBits(ChangesMask.GetBlock(0), 5);

		data.FlushBits();

		if (ChangesMask[0])
		{
			if (ChangesMask[1])
				data.WriteInt32(ScriptPackageID);

			if (ChangesMask[2])
				data.WriteUInt32(RndSeedVal);

			if (ChangesMask[3])
				data.WritePackedGuid(CreatedBy);

			if (ChangesMask[4])
				data.WriteUInt32(SceneType);
		}
	}

	public override void ClearChangesMask()
	{
		ClearChangesMask(ScriptPackageID);
		ClearChangesMask(RndSeedVal);
		ClearChangesMask(CreatedBy);
		ClearChangesMask(SceneType);
		ChangesMask.ResetAll();
	}
}