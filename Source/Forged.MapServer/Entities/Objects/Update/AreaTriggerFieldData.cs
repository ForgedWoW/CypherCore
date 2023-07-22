// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Numerics;
using Forged.MapServer.Entities.AreaTriggers;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Networking;
using Framework.Constants;

namespace Forged.MapServer.Entities.Objects.Update;

public class AreaTriggerFieldData : BaseUpdateData<AreaTrigger>
{
    public UpdateField<float> BoundsRadius2D = new(0, 17);
    public UpdateField<ObjectGuid> Caster = new(0, 8);
    public UpdateField<ObjectGuid> CreatingEffectGUID = new(0, 19);
    public UpdateField<uint> DecalPropertiesID = new(0, 18);
    public UpdateField<uint> Duration = new(0, 9);
    public UpdateField<ScaleCurve> ExtraScaleCurve = new(0, 4);
    public UpdateField<bool> Field260 = new(0, 1);
    public UpdateField<bool> Field261 = new(0, 2);
    public UpdateField<uint> NumUnitsInside = new(0, 20);
    public UpdateField<uint> NumPlayersInside = new(0, 21);
    public UpdateField<ObjectGuid> OrbitPathTarget = new(0, 22);
    public UpdateField<uint> FieldB0 = new(0, 13);
    public UpdateField<ScaleCurve> FieldC38 = new(0, 5);
    public UpdateField<ScaleCurve> FieldC54 = new(0, 6);
    public UpdateField<ScaleCurve> FieldC70 = new(0, 7);
    public UpdateField<Vector3> FieldF8 = new(0, 23);
    public UpdateField<ScaleCurve> OverrideScaleCurve = new(0, 3);
    public UpdateField<uint> SpellForVisuals = new(0, 15);
    public UpdateField<uint> SpellID = new(0, 14);
    public UpdateField<SpellCastVisualField> SpellVisual = new(0, 16);
    public UpdateField<uint> TimeToTarget = new(0, 10);
    public UpdateField<uint> TimeToTargetExtraScale = new(0, 12);
    public UpdateField<uint> TimeToTargetScale = new(0, 11);
    public UpdateField<VisualAnim> VisualAnim = new(0, 24);

    public AreaTriggerFieldData() : base(0, TypeId.AreaTrigger, 25)
    {
    }

    public override void ClearChangesMask()
    {
        ClearChangesMask(Field260);
        ClearChangesMask(Field261);
        ClearChangesMask(OverrideScaleCurve);
        ClearChangesMask(ExtraScaleCurve);
        ClearChangesMask(FieldC38);
        ClearChangesMask(FieldC54);
        ClearChangesMask(FieldC70);
        ClearChangesMask(Caster);
        ClearChangesMask(Duration);
        ClearChangesMask(TimeToTarget);
        ClearChangesMask(TimeToTargetScale);
        ClearChangesMask(TimeToTargetExtraScale);
        ClearChangesMask(FieldB0);
        ClearChangesMask(SpellID);
        ClearChangesMask(SpellForVisuals);
        ClearChangesMask(SpellVisual);
        ClearChangesMask(BoundsRadius2D);
        ClearChangesMask(DecalPropertiesID);
        ClearChangesMask(CreatingEffectGUID);
        ClearChangesMask(NumUnitsInside);
        ClearChangesMask(NumPlayersInside);
        ClearChangesMask(OrbitPathTarget);
        ClearChangesMask(FieldF8);
        ClearChangesMask(VisualAnim);
        ChangesMask.ResetAll();
    }

    public void WriteCreate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, AreaTrigger owner, Player receiver)
    {
        OverrideScaleCurve.Value.WriteCreate(data, owner, receiver);
        data.WritePackedGuid(Caster);
        data.WriteUInt32(Duration);
        data.WriteUInt32(TimeToTarget);
        data.WriteUInt32(TimeToTargetScale);
        data.WriteUInt32(TimeToTargetExtraScale);
        data.WriteUInt32(FieldB0);
        data.WriteUInt32(SpellID);
        data.WriteUInt32(SpellForVisuals);

        SpellVisual.Value.WriteCreate(data, owner, receiver);

        data.WriteFloat(BoundsRadius2D);
        data.WriteUInt32(DecalPropertiesID);
        data.WritePackedGuid(CreatingEffectGUID);
        data.WriteUInt32(NumUnitsInside);
        data.WriteUInt32(NumPlayersInside);
        data.WritePackedGuid(OrbitPathTarget);
        data.WriteVector3(FieldF8);
        ExtraScaleCurve.Value.WriteCreate(data, owner, receiver);
        data.WriteBit(Field260);
        data.WriteBit(Field261);
        FieldC38.Value.WriteCreate(data, owner, receiver);
        FieldC54.Value.WriteCreate(data, owner, receiver);
        FieldC70.Value.WriteCreate(data, owner, receiver);
        VisualAnim.Value.WriteCreate(data, owner, receiver);
        data.FlushBits();
    }

    public void WriteUpdate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, AreaTrigger owner, Player receiver)
    {
        WriteUpdate(data, ChangesMask, false, owner, receiver);
    }

    public void WriteUpdate(WorldPacket data, UpdateMask changesMask, bool ignoreNestedChangesMask, AreaTrigger owner, Player receiver)
    {
        data.WriteBits(ChangesMask.GetBlock(0), 25);

        if (ChangesMask[0])
        {
            if (ChangesMask[1])
            {
                data.WriteBit(Field260);
            }
            if (changesMask[2])
            {
                data.WriteBit(Field261);
            }
        }
        data.FlushBits();
        if (changesMask[0])
        {
            if (changesMask[3])
            {
                OverrideScaleCurve.Value.WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
            }
            if (ChangesMask[8])
            {
                data.WritePackedGuid(Caster);
            }
            if (ChangesMask[9])
            {
                data.WriteUInt32(Duration);
            }
            if (ChangesMask[10])
            {
                data.WriteUInt32(TimeToTarget);
            }
            if (ChangesMask[11])
            {
                data.WriteUInt32(TimeToTargetScale);
            }
            if (ChangesMask[12])
            {
                data.WriteUInt32(TimeToTargetExtraScale);
            }
            if (ChangesMask[13])
            {
                data.WriteUInt32(FieldB0);
            }
            if (changesMask[14])
            {
                data.WriteUInt32(SpellID);
            }
            if (ChangesMask[15])
            {
                data.WriteUInt32(SpellForVisuals);
            }
            if (ChangesMask[16])
            {
                SpellVisual.Value.WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
            }
            if (ChangesMask[17])
            {
                data.WriteFloat(BoundsRadius2D);
            }
            if (ChangesMask[18])
            {
                data.WriteUInt32(DecalPropertiesID);
            }
            if (ChangesMask[19])
            {
                data.WritePackedGuid(CreatingEffectGUID);
            }
            if (changesMask[20])
            {
                data.WriteUInt32(NumUnitsInside);
            }
            if (changesMask[21])
            {
                data.WriteUInt32(NumPlayersInside);
            }
            if (changesMask[22])
            {
                data.WritePackedGuid(OrbitPathTarget);
            }
            if (changesMask[23])
            {
                data.WriteVector3(FieldF8);
            }
            if (ChangesMask[4])
            {
                ExtraScaleCurve.Value.WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
            }
            if (changesMask[5])
            {
                FieldC38.Value.WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
            }
            if (changesMask[6])
            {
                FieldC54.Value.WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
            }
            if (changesMask[7])
            {
                FieldC70.Value.WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
            }
            if (changesMask[24])
            {
                VisualAnim.Value.WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
            }
        }
        data.FlushBits();
    }
}