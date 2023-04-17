// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Networking;
using Framework.Constants;

namespace Forged.MapServer.Entities.Objects.Update;

public class ObjectFieldData : BaseUpdateData<WorldObject>
{
    public UpdateField<uint> DynamicFlags = new(0, 2);
    public UpdateField<uint> EntryId = new(0, 1);
    public UpdateField<float> Scale = new(0, 3);

    public ObjectFieldData() : base(0, TypeId.Object, 4) { }

    public override void ClearChangesMask()
    {
        ClearChangesMask(EntryId);
        ClearChangesMask(DynamicFlags);
        ClearChangesMask(Scale);
        ChangesMask.ResetAll();
    }

    public void WriteCreate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, WorldObject owner, Player receiver)
    {
        data.WriteUInt32(GetViewerDependentEntryId(this, owner, receiver));
        data.WriteUInt32(GetViewerDependentDynamicFlags(this, owner, receiver));
        data.WriteFloat(Scale);
    }

    public void WriteUpdate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, WorldObject owner, Player receiver)
    {
        WriteUpdate(data, ChangesMask, false, owner, receiver);
    }

    public void WriteUpdate(WorldPacket data, UpdateMask changesMask, bool ignoreNestedChangesMask, WorldObject owner, Player receiver)
    {
        data.WriteBits(changesMask.GetBlock(0), 4);

        data.FlushBits();

        if (changesMask[0])
        {
            if (changesMask[1])
                data.WriteUInt32(GetViewerDependentEntryId(this, owner, receiver));

            if (changesMask[2])
                data.WriteUInt32(GetViewerDependentDynamicFlags(this, owner, receiver));

            if (changesMask[3])
                data.WriteFloat(Scale);
        }
    }

    private uint GetViewerDependentDynamicFlags(ObjectFieldData objectData, WorldObject obj, Player receiver)
    {
        uint unitDynFlags = objectData.DynamicFlags;

        var unit = obj.AsUnit;

        if (unit != null)
        {
            var creature = obj.AsCreature;

            if (creature != null)
            {
                if ((unitDynFlags & (uint)UnitDynFlags.Tapped) != 0 && !creature.IsTappedBy(receiver))
                    unitDynFlags &= ~(uint)UnitDynFlags.Tapped;

                if ((unitDynFlags & (uint)UnitDynFlags.Lootable) != 0 && !receiver.IsAllowedToLoot(creature))
                    unitDynFlags &= ~(uint)UnitDynFlags.Lootable;

                if ((unitDynFlags & (uint)UnitDynFlags.CanSkin) != 0 && creature.IsSkinnedBy(receiver))
                    unitDynFlags &= ~(uint)UnitDynFlags.CanSkin;
            }

            // unit UNIT_DYNFLAG_TRACK_UNIT should only be sent to caster of SPELL_AURA_MOD_STALKED auras
            if (unitDynFlags.HasAnyFlag((uint)UnitDynFlags.TrackUnit))
                if (!unit.HasAuraTypeWithCaster(AuraType.ModStalked, receiver.GUID))
                    unitDynFlags &= ~(uint)UnitDynFlags.TrackUnit;
        }
        else
        {
            var gameObject = obj.AsGameObject;

            if (gameObject != null)
            {
                GameObjectDynamicLowFlags dynFlags = 0;
                ushort pathProgress = 0xFFFF;

                switch (gameObject.GoType)
                {
                    case GameObjectTypes.QuestGiver:
                        if (gameObject.ActivateToQuest(receiver))
                            dynFlags |= GameObjectDynamicLowFlags.Activate;

                        break;
                    case GameObjectTypes.Chest:
                        if (gameObject.ActivateToQuest(receiver))
                            dynFlags |= GameObjectDynamicLowFlags.Activate | GameObjectDynamicLowFlags.Sparkle | GameObjectDynamicLowFlags.Highlight;
                        else if (receiver.IsGameMaster)
                            dynFlags |= GameObjectDynamicLowFlags.Activate;

                        break;
                    case GameObjectTypes.Goober:
                        if (gameObject.ActivateToQuest(receiver))
                        {
                            dynFlags |= GameObjectDynamicLowFlags.Highlight;

                            if (gameObject.GetGoStateFor(receiver.GUID) != GameObjectState.Active)
                                dynFlags |= GameObjectDynamicLowFlags.Activate;
                        }
                        else if (receiver.IsGameMaster)
                            dynFlags |= GameObjectDynamicLowFlags.Activate;

                        break;
                    case GameObjectTypes.Generic:
                        if (gameObject.ActivateToQuest(receiver))
                            dynFlags |= GameObjectDynamicLowFlags.Sparkle | GameObjectDynamicLowFlags.Highlight;

                        break;
                    case GameObjectTypes.Transport:
                    case GameObjectTypes.MapObjTransport:
                    {
                        dynFlags = (GameObjectDynamicLowFlags)((int)unitDynFlags & 0xFFFF);
                        pathProgress = (ushort)((int)unitDynFlags >> 16);

                        break;
                    }
                    case GameObjectTypes.CapturePoint:
                        if (!gameObject.CanInteractWithCapturePoint(receiver))
                            dynFlags |= GameObjectDynamicLowFlags.NoInterract;
                        else
                            dynFlags &= ~GameObjectDynamicLowFlags.NoInterract;

                        break;
                    case GameObjectTypes.GatheringNode:
                        if (gameObject.ActivateToQuest(receiver))
                            dynFlags |= GameObjectDynamicLowFlags.Activate | GameObjectDynamicLowFlags.Sparkle | GameObjectDynamicLowFlags.Highlight;

                        if (gameObject.GetGoStateFor(receiver.GUID) == GameObjectState.Active)
                            dynFlags |= GameObjectDynamicLowFlags.Depleted;

                        break;
                }

                if (!gameObject.MeetsInteractCondition(receiver))
                    dynFlags |= GameObjectDynamicLowFlags.NoInterract;

                unitDynFlags = ((uint)pathProgress << 16) | (uint)dynFlags;
            }
        }

        return unitDynFlags;
    }

    private uint GetViewerDependentEntryId(ObjectFieldData objectData, WorldObject obj, Player receiver)
    {
        uint entryId = objectData.EntryId;
        var unit = obj.AsUnit;

        var summon = unit?.ToTempSummon();

        if (summon != null && summon.SummonerGUID == receiver.GUID && summon.CreatureIdVisibleToSummoner.HasValue)
            entryId = summon.CreatureIdVisibleToSummoner.Value;

        return entryId;
    }
}