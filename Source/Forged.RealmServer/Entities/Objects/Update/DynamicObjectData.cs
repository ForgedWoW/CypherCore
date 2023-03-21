// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Forged.RealmServer.Networking;

namespace Forged.RealmServer.Entities;

public class DynamicObjectData : BaseUpdateData<DynamicObject>
{
	public UpdateField<ObjectGuid> Caster = new(0, 1);
	public UpdateField<byte> Type = new(0, 2);
	public UpdateField<SpellCastVisualField> SpellVisual = new(0, 3);
	public UpdateField<uint> SpellID = new(0, 4);
	public UpdateField<float> Radius = new(0, 5);
	public UpdateField<uint> CastTime = new(0, 6);

	public DynamicObjectData() : base(0, TypeId.DynamicObject, 7) { }

	public void WriteCreate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, DynamicObject owner, Player receiver)
	{
		data.WritePackedGuid(Caster);
		data.WriteUInt8(Type);
		((SpellCastVisualField)SpellVisual).WriteCreate(data, owner, receiver);
		data.WriteUInt32(SpellID);
		data.WriteFloat(Radius);
		data.WriteUInt32(CastTime);
	}

	public void WriteUpdate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, DynamicObject owner, Player receiver)
	{
		WriteUpdate(data, ChangesMask, false, owner, receiver);
	}

	public void WriteUpdate(WorldPacket data, UpdateMask changesMask, bool ignoreNestedChangesMask, DynamicObject owner, Player receiver)
	{
		data.WriteBits(ChangesMask.GetBlock(0), 7);

		data.FlushBits();

		if (ChangesMask[0])
		{
			if (ChangesMask[1])
				data.WritePackedGuid(Caster);

			if (ChangesMask[2])
				data.WriteUInt8(Type);

			if (ChangesMask[3])
				((SpellCastVisualField)SpellVisual).WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);

			if (ChangesMask[4])
				data.WriteUInt32(SpellID);

			if (ChangesMask[5])
				data.WriteFloat(Radius);

			if (ChangesMask[6])
				data.WriteUInt32(CastTime);
		}
	}

	public override void ClearChangesMask()
	{
		ClearChangesMask(Caster);
		ClearChangesMask(Type);
		ClearChangesMask(SpellVisual);
		ClearChangesMask(SpellID);
		ClearChangesMask(Radius);
		ClearChangesMask(CastTime);
		ChangesMask.ResetAll();
	}
}