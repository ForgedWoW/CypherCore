// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.DataStorage.Structs.S;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Globals;
using Forged.MapServer.Maps;
using Forged.MapServer.Maps.GridNotifiers;
using Framework.Constants;
using Serilog;

namespace Forged.MapServer.Entities;

public class TempSummon : Creature
{
    public SummonPropertiesRecord SummonPropertiesRecord;
    private uint _lifetime;
    private ObjectGuid _summonerGuid;

    public TempSummon(SummonPropertiesRecord propertiesRecord, WorldObject owner, bool isWorldObject) : base(isWorldObject)
    {
        SummonPropertiesRecord = propertiesRecord;
        SummonType = TempSummonType.ManualDespawn;

        _summonerGuid = owner?.GUID ?? ObjectGuid.Empty;
        UnitTypeMask |= UnitTypeMask.Summon;
        CanFollowOwner = true;
    }

    public bool CanFollowOwner { get; set; }
    public uint? CreatureIdVisibleToSummoner { get; private set; }
    public uint? DisplayIdVisibleToSummoner { get; private set; }
    public ObjectGuid SummonerGUID => _summonerGuid;

    public uint Timer { get; private set; }
    private TempSummonType SummonType { get; set; }
    public override float GetDamageMultiplierForTarget(WorldObject target)
    {
        return 1.0f;
    }

    public override string GetDebugInfo()
    {
        return $"{base.GetDebugInfo()}\nTempSummonType : {SummonType} Summoner: {SummonerGUID} Timer: {Timer}";
    }

    public WorldObject GetSummoner()
    {
        return !_summonerGuid.IsEmpty ? Global.ObjAccessor.GetWorldObject(this, _summonerGuid) : null;
    }

    public Creature GetSummonerCreatureBase()
    {
        return !_summonerGuid.IsEmpty ? ObjectAccessor.GetCreature(this, _summonerGuid) : null;
    }

    public GameObject GetSummonerGameObject()
    {
        var summoner = GetSummoner();

        return summoner?.AsGameObject;
    }

    public Unit GetSummonerUnit()
    {
        var summoner = GetSummoner();

        return summoner?.AsUnit;
    }

    public virtual void InitStats(uint duration)
    {
        Timer = duration;
        _lifetime = duration;

        if (SummonType == TempSummonType.ManualDespawn)
            SummonType = (duration == 0) ? TempSummonType.DeadDespawn : TempSummonType.TimedDespawn;

        var owner = GetSummonerUnit();

        if (owner != null && IsTrigger && Spells[0] != 0)
            if (owner.IsTypeId(TypeId.Player))
                ControlledByPlayer = true;

        if (owner is { IsPlayer: true })
        {
            var summonedData = Global.ObjectMgr.GetCreatureSummonedData(Entry);

            if (summonedData != null)
            {
                CreatureIdVisibleToSummoner = summonedData.CreatureIdVisibleToSummoner;

                if (summonedData.CreatureIdVisibleToSummoner.HasValue)
                {
                    var creatureTemplateVisibleToSummoner = Global.ObjectMgr.GetCreatureTemplate(summonedData.CreatureIdVisibleToSummoner.Value);
                    DisplayIdVisibleToSummoner = GameObjectManager.ChooseDisplayId(creatureTemplateVisibleToSummoner).CreatureDisplayId;
                }
            }
        }

        if (SummonPropertiesRecord == null)
            return;

        if (owner != null)
        {
            var slot = SummonPropertiesRecord.Slot;

            if (slot > 0)
            {
                if (!owner.SummonSlot[slot].IsEmpty && owner.SummonSlot[slot] != GUID)
                {
                    var oldSummon = Location.Map.GetCreature(owner.SummonSlot[slot]);

                    if (oldSummon is { IsSummon: true })
                        oldSummon.ToTempSummon().UnSummon();
                }

                owner.SummonSlot[slot] = GUID;
            }

            if (!SummonPropertiesRecord.GetFlags().HasFlag(SummonPropertiesFlags.UseCreatureLevel))
                SetLevel(owner.Level);
        }

        var faction = SummonPropertiesRecord.Faction;

        if (owner && SummonPropertiesRecord.GetFlags().HasFlag(SummonPropertiesFlags.UseSummonerFaction)) // TODO: Determine priority between faction and flag
            faction = owner.Faction;

        if (faction != 0)
            Faction = faction;

        if (SummonPropertiesRecord.GetFlags().HasFlag(SummonPropertiesFlags.SummonFromBattlePetJournal))
            RemoveNpcFlag(NPCFlags.WildBattlePet);
    }

    public virtual void InitSummon()
    {
        var owner = GetSummoner();

        if (owner != null)
        {
            if (owner.IsCreature)
                owner.AsCreature.AI?.JustSummoned(this);
            else if (owner.IsGameObject)
                owner.AsGameObject.AI?.JustSummoned(this);

            if (IsAIEnabled)
                AI.IsSummonedBy(owner);
        }
    }

    public override void RemoveFromWorld()
    {
        if (!Location.IsInWorld)
            return;

        if (SummonPropertiesRecord != null)
        {
            var slot = SummonPropertiesRecord.Slot;

            if (slot > 0)
            {
                var owner = GetSummonerUnit();

                if (owner != null)
                    if (owner.SummonSlot[slot] == GUID)
                        owner.SummonSlot[slot].Clear();
            }
        }

        if (!OwnerGUID.IsEmpty)
            Log.Logger.Error("Unit {0} has owner guid when removed from world", Entry);

        base.RemoveFromWorld();
    }

    public override void SaveToDB(uint mapid, List<Difficulty> spawnDifficulties) { }

    public void SetSummonerGUID(ObjectGuid summonerGUID)
    {
        _summonerGuid = summonerGUID;
    }
    public void SetTempSummonType(TempSummonType type)
    {
        SummonType = type;
    }

    public virtual void UnSummon()
    {
        UnSummon(TimeSpan.Zero);
    }

    public virtual void UnSummon(TimeSpan msTime)
    {
        if (msTime != TimeSpan.Zero)
        {
            ForcedUnsummonDelayEvent pEvent = new(this);

            Events.AddEvent(pEvent, Events.CalculateTime(msTime));

            return;
        }

        if (IsPet)
        {
            AsPet.Remove(PetSaveMode.NotInSlot);

            return;
        }

        var owner = GetSummoner();

        if (owner != null)
        {
            if (owner.IsCreature)
                owner.AsCreature.AI?.SummonedCreatureDespawn(this);
            else if (owner.IsGameObject)
                owner.AsGameObject.AI?.SummonedCreatureDespawn(this);
        }

        Location.AddObjectToRemoveList();
    }

    public override void Update(uint diff)
    {
        base.Update(diff);

        if (DeathState == DeathState.Dead)
        {
            UnSummon();

            return;
        }

        switch (SummonType)
        {
            case TempSummonType.ManualDespawn:
            case TempSummonType.DeadDespawn:
                break;
            case TempSummonType.TimedDespawn:
            {
                if (Timer <= diff)
                {
                    UnSummon();

                    return;
                }

                Timer -= diff;

                break;
            }
            case TempSummonType.TimedDespawnOutOfCombat:
            {
                if (!IsInCombat)
                {
                    if (Timer <= diff)
                    {
                        UnSummon();

                        return;
                    }

                    Timer -= diff;
                }
                else if (Timer != _lifetime)
                {
                    Timer = _lifetime;
                }

                break;
            }

            case TempSummonType.CorpseTimedDespawn:
            {
                if (DeathState == DeathState.Corpse)
                {
                    if (Timer <= diff)
                    {
                        UnSummon();

                        return;
                    }

                    Timer -= diff;
                }

                break;
            }
            case TempSummonType.CorpseDespawn:
            {
                // if m_deathState is DEAD, CORPSE was skipped
                if (DeathState == DeathState.Corpse)
                {
                    UnSummon();

                    return;
                }

                break;
            }
            case TempSummonType.TimedOrCorpseDespawn:
            {
                if (DeathState == DeathState.Corpse)
                {
                    UnSummon();

                    return;
                }

                if (!IsInCombat)
                {
                    if (Timer <= diff)
                    {
                        UnSummon();

                        return;
                    }
                    else
                    {
                        Timer -= diff;
                    }
                }
                else if (Timer != _lifetime)
                {
                    Timer = _lifetime;
                }

                break;
            }
            case TempSummonType.TimedOrDeadDespawn:
            {
                if (!IsInCombat && IsAlive)
                {
                    if (Timer <= diff)
                    {
                        UnSummon();

                        return;
                    }
                    else
                    {
                        Timer -= diff;
                    }
                }
                else if (Timer != _lifetime)
                {
                    Timer = _lifetime;
                }

                break;
            }
            default:
                UnSummon();
                Log.Logger.Error("Temporary summoned creature (entry: {0}) have unknown type {1} of ", Entry, SummonType);

                break;
        }
    }
    public override void UpdateObjectVisibilityOnCreate()
    {
        List<WorldObject> objectsToUpdate = new();
        objectsToUpdate.Add(this);

        var smoothPhasing = Visibility.GetSmoothPhasing();

        var infoForSeer = smoothPhasing?.GetInfoForSeer(DemonCreatorGUID);

        if (infoForSeer is { ReplaceObject: { } } && smoothPhasing.IsReplacing(infoForSeer.ReplaceObject.Value))
        {
            var original = Global.ObjAccessor.GetWorldObject(this, infoForSeer.ReplaceObject.Value);

            if (original != null)
                objectsToUpdate.Add(original);
        }

        VisibleChangesNotifier notifier = new(objectsToUpdate, GridType.World);
        Cell.VisitGrid(this, notifier, Visibility.VisibilityRange);
    }

    public override void UpdateObjectVisibilityOnDestroy()
    {
        List<WorldObject> objectsToUpdate = new();
        objectsToUpdate.Add(this);

        WorldObject original = null;
        var smoothPhasing = Visibility.GetSmoothPhasing();

        if (smoothPhasing != null)
        {
            var infoForSeer = smoothPhasing.GetInfoForSeer(DemonCreatorGUID);

            if (infoForSeer is { ReplaceObject: { } } && smoothPhasing.IsReplacing(infoForSeer.ReplaceObject.Value))
                original = Global.ObjAccessor.GetWorldObject(this, infoForSeer.ReplaceObject.Value);

            if (original != null)
            {
                objectsToUpdate.Add(original);

                // disable replacement without removing - it is still needed for next step (visibility update)
                var originalSmoothPhasing = original.Visibility.GetSmoothPhasing();

                originalSmoothPhasing?.DisableReplacementForSeer(DemonCreatorGUID);
            }
        }

        VisibleChangesNotifier notifier = new(objectsToUpdate, GridType.World);
        Cell.VisitGrid(this, notifier, Visibility.VisibilityRange);

        if (original != null) // original is only != null when it was replaced
        {
            var originalSmoothPhasing = original.Visibility.GetSmoothPhasing();

            originalSmoothPhasing?.ClearViewerDependentInfo(DemonCreatorGUID);
        }
    }
}