// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Numerics;
using Framework.Constants;
using Game.Common.Networking;

namespace Game.Entities;

public class AreaTriggerFieldData : BaseUpdateData<AreaTrigger>
{
	public UpdateField<ScaleCurve> OverrideScaleCurve = new(0, 1);
	public UpdateField<ScaleCurve> ExtraScaleCurve = new(0, 2);
	public UpdateField<ScaleCurve> Field_C38 = new(0, 3);
	public UpdateField<ScaleCurve> Field_C54 = new(0, 4);
	public UpdateField<ScaleCurve> Field_C70 = new(0, 5);
	public UpdateField<ObjectGuid> Caster = new(0, 6);
	public UpdateField<uint> Duration = new(0, 7);
	public UpdateField<uint> TimeToTarget = new(0, 8);
	public UpdateField<uint> TimeToTargetScale = new(0, 9);
	public UpdateField<uint> TimeToTargetExtraScale = new(0, 10);
	public UpdateField<uint> Field_B0 = new(0, 11);
	public UpdateField<uint> SpellID = new(0, 12);
	public UpdateField<uint> SpellForVisuals = new(0, 13);
	public UpdateField<SpellCastVisualField> SpellVisual = new(0, 14);
	public UpdateField<float> BoundsRadius2D = new(0, 15);
	public UpdateField<uint> DecalPropertiesID = new(0, 16);
	public UpdateField<ObjectGuid> CreatingEffectGUID = new(0, 17);
	public UpdateField<uint> Field_80 = new(0, 18);
	public UpdateField<uint> Field_84 = new(0, 19);
	public UpdateField<ObjectGuid> Field_88 = new(0, 20);
	public UpdateField<Vector3> Field_F8 = new(0, 21);
	public UpdateField<VisualAnim> VisualAnim = new(0, 22);

	public AreaTriggerFieldData() : base(0, TypeId.AreaTrigger, 23) { }

	public void WriteCreate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, AreaTrigger owner, Player receiver)
	{
		OverrideScaleCurve.GetValue().WriteCreate(data, owner, receiver);
		data.WritePackedGuid(Caster);
		data.WriteUInt32(Duration);
		data.WriteUInt32(TimeToTarget);
		data.WriteUInt32(TimeToTargetScale);
		data.WriteUInt32(TimeToTargetExtraScale);
		data.WriteUInt32(Field_B0);
		data.WriteUInt32(SpellID);
		data.WriteUInt32(SpellForVisuals);

		SpellVisual.GetValue().WriteCreate(data, owner, receiver);

		data.WriteFloat(BoundsRadius2D);
		data.WriteUInt32(DecalPropertiesID);
		data.WritePackedGuid(CreatingEffectGUID);
		data.WriteUInt32(Field_80);
		data.WriteUInt32(Field_84);
		data.WritePackedGuid(Field_88);
		data.WriteVector3(Field_F8);
		ExtraScaleCurve.GetValue().WriteCreate(data, owner, receiver);
		Field_C38.GetValue().WriteCreate(data, owner, receiver);
		Field_C54.GetValue().WriteCreate(data, owner, receiver);
		Field_C70.GetValue().WriteCreate(data, owner, receiver);
		VisualAnim.GetValue().WriteCreate(data, owner, receiver);
	}

	public void WriteUpdate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, AreaTrigger owner, Player receiver)
	{
		WriteUpdate(data, ChangesMask, false, owner, receiver);
	}

	public void WriteUpdate(WorldPacket data, UpdateMask changesMask, bool ignoreNestedChangesMask, AreaTrigger owner, Player receiver)
	{
		data.WriteBits(ChangesMask.GetBlock(0), 23);

		data.FlushBits();

		if (ChangesMask[0])
		{
			if (ChangesMask[1])
				OverrideScaleCurve.GetValue().WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);

			if (ChangesMask[6])
				data.WritePackedGuid(Caster);

			if (ChangesMask[7])
				data.WriteUInt32(Duration);

			if (ChangesMask[8])
				data.WriteUInt32(TimeToTarget);

			if (ChangesMask[9])
				data.WriteUInt32(TimeToTargetScale);

			if (ChangesMask[10])
				data.WriteUInt32(TimeToTargetExtraScale);

			if (ChangesMask[11])
				data.WriteUInt32(Field_B0);

			if (changesMask[12])
				data.WriteUInt32(SpellID);

			if (ChangesMask[13])
				data.WriteUInt32(SpellForVisuals);

			if (ChangesMask[14])
				SpellVisual.GetValue().WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);

			if (ChangesMask[15])
				data.WriteFloat(BoundsRadius2D);

			if (ChangesMask[16])
				data.WriteUInt32(DecalPropertiesID);

			if (ChangesMask[17])
				data.WritePackedGuid(CreatingEffectGUID);

			if (changesMask[18])
				data.WriteUInt32(Field_80);

			if (changesMask[19])
				data.WriteUInt32(Field_84);

			if (changesMask[20])
				data.WritePackedGuid(Field_88);

			if (changesMask[21])
				data.WriteVector3(Field_F8);

			if (ChangesMask[2])
				ExtraScaleCurve.GetValue().WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);

			if (changesMask[3])
				Field_C38.GetValue().WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);

			if (changesMask[4])
				Field_C54.GetValue().WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);

			if (changesMask[5])
				Field_C70.GetValue().WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);

			if (changesMask[22])
				VisualAnim.GetValue().WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
		}
	}

	public override void ClearChangesMask()
	{
		ClearChangesMask(OverrideScaleCurve);
		ClearChangesMask(ExtraScaleCurve);
		ClearChangesMask(Field_C38);
		ClearChangesMask(Field_C54);
		ClearChangesMask(Field_C70);
		ClearChangesMask(Caster);
		ClearChangesMask(Duration);
		ClearChangesMask(TimeToTarget);
		ClearChangesMask(TimeToTargetScale);
		ClearChangesMask(TimeToTargetExtraScale);
		ClearChangesMask(Field_B0);
		ClearChangesMask(SpellID);
		ClearChangesMask(SpellForVisuals);
		ClearChangesMask(SpellVisual);
		ClearChangesMask(BoundsRadius2D);
		ClearChangesMask(DecalPropertiesID);
		ClearChangesMask(CreatingEffectGUID);
		ClearChangesMask(Field_80);
		ClearChangesMask(Field_84);
		ClearChangesMask(Field_88);
		ClearChangesMask(Field_F8);
		ClearChangesMask(VisualAnim);
		ChangesMask.ResetAll();
	}
}