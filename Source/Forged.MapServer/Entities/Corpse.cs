// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Text;
using Forged.MapServer.Cache;
using Forged.MapServer.Chrono;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Objects.Update;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.LootManagement;
using Forged.MapServer.Maps;
using Forged.MapServer.Maps.Grids;
using Forged.MapServer.Networking;
using Framework.Collections;
using Framework.Constants;
using Framework.Database;
using Game.Common;
using Serilog;

namespace Forged.MapServer.Entities;

public class Corpse : WorldObject
{
    private readonly CharacterCache _characterCache;
    private readonly CharacterDatabase _characterDatabase;
    // gride for corpse position for fast search

    public Corpse(CorpseType type, ClassFactory classFactory, CharacterDatabase characterDatabase, CharacterCache characterCache) : base(type != CorpseType.Bones, classFactory)
    {
        CorpseType = type;
        _characterDatabase = characterDatabase;
        _characterCache = characterCache;
        ObjectTypeId = TypeId.Corpse;
        ObjectTypeMask |= TypeMask.Corpse;

        UpdateFlag.Stationary = true;

        CorpseData = new CorpseData();

        GhostTime = GameTime.CurrentTime;
    }

    public CellCoord CellCoord { get; private set; }
    public CorpseData CorpseData { get; set; }

    public CorpseType CorpseType { get; }

    public override uint Faction
    {
        get => (uint)(int)CorpseData.FactionTemplate;
        set => SetFactionTemplate((int)value);
    }

    public long GhostTime { get; private set; }
    public Loot Loot { get; set; }
    public Player LootRecipient { get; set; }

    public override ObjectGuid OwnerGUID => CorpseData.Owner;

    public static void DeleteFromDB(ObjectGuid ownerGuid, SQLTransaction trans, CharacterDatabase characterDatabase)
    {
        var stmt = characterDatabase.GetPreparedStatement(CharStatements.DEL_CORPSE);
        stmt.AddValue(0, ownerGuid.Counter);
        characterDatabase.ExecuteOrAppend(trans, stmt);

        stmt = characterDatabase.GetPreparedStatement(CharStatements.DEL_CORPSE_PHASES);
        stmt.AddValue(0, ownerGuid.Counter);
        characterDatabase.ExecuteOrAppend(trans, stmt);

        stmt = characterDatabase.GetPreparedStatement(CharStatements.DEL_CORPSE_CUSTOMIZATIONS);
        stmt.AddValue(0, ownerGuid.Counter);
        characterDatabase.ExecuteOrAppend(trans, stmt);
    }

    public override void AddToWorld()
    {
        // Register the corpse for guid lookup
        if (!Location.IsInWorld)
            Location.Map.ObjectsStore.TryAdd(GUID, this);

        base.AddToWorld();
    }

    public override void BuildValuesCreate(WorldPacket data, Player target)
    {
        var flags = GetUpdateFieldFlagsFor(target);
        WorldPacket buffer = new();

        ObjectData.WriteCreate(buffer, flags, this, target);
        CorpseData.WriteCreate(buffer, flags, this, target);

        data.WriteUInt32(buffer.GetSize() + 1);
        data.WriteUInt8((byte)flags);
        data.WriteBytes(buffer);
    }

    public override void BuildValuesUpdate(WorldPacket data, Player target)
    {
        var flags = GetUpdateFieldFlagsFor(target);
        WorldPacket buffer = new();

        buffer.WriteUInt32(Values.GetChangedObjectTypeMask());

        if (Values.HasChanged(TypeId.Object))
            ObjectData.WriteUpdate(buffer, flags, this, target);

        if (Values.HasChanged(TypeId.Corpse))
            CorpseData.WriteUpdate(buffer, flags, this, target);

        data.WriteUInt32(buffer.GetSize());
        data.WriteBytes(buffer);
    }

    public override void ClearUpdateMask(bool remove)
    {
        Values.ClearChangesMask(CorpseData);
        base.ClearUpdateMask(remove);
    }

    public bool Create(ulong guidlow, Map map)
    {
        Create(ObjectGuid.Create(HighGuid.Corpse, map.Id, 0, guidlow));

        return true;
    }

    public bool Create(ulong guidlow, Player owner)
    {
        Location.Relocate(owner.Location.X, owner.Location.Y, owner.Location.Z, owner.Location.Orientation);

        if (!GridDefines.IsValidMapCoord(Location))
        {
            Log.Logger.Error("Corpse (guidlow {0}, owner {1}) not created. Suggested coordinates isn't valid (X: {2} Y: {3})",
                             guidlow,
                             owner.GetName(),
                             owner.Location.X,
                             owner.Location.Y);

            return false;
        }

        Create(ObjectGuid.Create(HighGuid.Corpse, owner.Location.MapId, 0, guidlow));

        ObjectScale = 1;
        SetOwnerGUID(owner.GUID);

        CellCoord = GridDefines.ComputeCellCoord(Location.X, Location.Y);

        PhasingHandler.InheritPhaseShift(this, owner);

        return true;
    }

    public void DeleteFromDB(SQLTransaction trans)
    {
        DeleteFromDB(OwnerGUID, trans, _characterDatabase);
    }

    public CorpseDynFlags GetCorpseDynamicFlags()
    {
        return (CorpseDynFlags)(uint)CorpseData.DynamicFlags;
    }

    public override Loot GetLootForPlayer(Player player)
    {
        return Loot;
    }

    public bool HasCorpseDynamicFlag(CorpseDynFlags flag)
    {
        return (CorpseData.DynamicFlags & (uint)flag) != 0;
    }

    public bool IsExpired(long t)
    {
        // Deleted character
        if (!_characterCache.HasCharacterCacheEntry(OwnerGUID))
            return true;

        if (CorpseType == CorpseType.Bones)
            return GhostTime < t - 60 * Time.MINUTE;

        return GhostTime < t - 3 * Time.DAY;
    }

    public bool LoadCorpseFromDB(ulong guid, SQLFields field)
    {
        //        0     1     2     3            4      5          6          7     8      9       10     11        12    13          14          15
        // SELECT posX, posY, posZ, orientation, mapId, displayId, itemCache, race, class, gender, flags, dynFlags, time, corpseType, instanceId, guid FROM corpse WHERE mapId = ? AND instanceId = ?

        var posX = field.Read<float>(0);
        var posY = field.Read<float>(1);
        var posZ = field.Read<float>(2);
        var o = field.Read<float>(3);
        var mapId = field.Read<ushort>(4);

        Create(ObjectGuid.Create(HighGuid.Corpse, mapId, 0, guid));

        ObjectScale = 1.0f;
        SetDisplayId(field.Read<uint>(5));
        StringArray items = new(field.Read<string>(6), ' ');

        if (items.Length == CorpseData.Items.GetSize())
            for (uint index = 0; index < CorpseData.Items.GetSize(); ++index)
                SetItem(index, uint.Parse(items[(int)index]));

        SetRace(field.Read<byte>(7));
        SetClass(field.Read<byte>(8));
        SetSex(field.Read<byte>(9));
        ReplaceAllFlags((CorpseFlags)field.Read<byte>(10));
        ReplaceAllCorpseDynamicFlags((CorpseDynFlags)field.Read<byte>(11));
        SetOwnerGUID(ObjectGuid.Create(HighGuid.Player, field.Read<ulong>(15)));
        SetFactionTemplate(CliDB.ChrRacesStorage.LookupByKey(CorpseData.RaceID).FactionID);

        GhostTime = field.Read<uint>(12);

        var instanceId = field.Read<uint>(14);

        // place
        Location = new WorldLocation(mapId, posX, posY, posZ, o);
        Location.SetLocationInstanceId(instanceId);

        if (!GridDefines.IsValidMapCoord(Location))
        {
            Log.Logger.Error("Corpse ({0}, owner: {1}) is not created, given coordinates are not valid (X: {2}, Y: {3}, Z: {4})",
                             GUID.ToString(),
                             OwnerGUID.ToString(),
                             posX,
                             posY,
                             posZ);

            return false;
        }

        CellCoord = GridDefines.ComputeCellCoord(Location.X, Location.Y);

        return true;
    }

    public void RemoveCorpseDynamicFlag(CorpseDynFlags dynamicFlags)
    {
        RemoveUpdateFieldFlagValue(Values.ModifyValue(CorpseData).ModifyValue(CorpseData.DynamicFlags), (uint)dynamicFlags);
    }

    public override void RemoveFromWorld()
    {
        // Remove the corpse from the accessor
        if (Location.IsInWorld)
            Location.Map.ObjectsStore.TryRemove(GUID, out _);

        base.RemoveFromWorld();
    }

    public void ReplaceAllCorpseDynamicFlags(CorpseDynFlags dynamicFlags)
    {
        SetUpdateFieldValue(Values.ModifyValue(CorpseData).ModifyValue(CorpseData.DynamicFlags), (uint)dynamicFlags);
    }

    public void ReplaceAllFlags(CorpseFlags flags)
    {
        SetUpdateFieldValue(Values.ModifyValue(CorpseData).ModifyValue(CorpseData.Flags), (uint)flags);
    }

    public void ResetGhostTime()
    {
        GhostTime = GameTime.CurrentTime;
    }

    public void SaveToDB()
    {
        // prevent DB data inconsistence problems and duplicates
        SQLTransaction trans = new();
        DeleteFromDB(trans);

        StringBuilder items = new();

        for (var i = 0; i < CorpseData.Items.GetSize(); ++i)
            items.Append($"{CorpseData.Items[i]} ");

        byte index = 0;
        var stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_CORPSE);
        stmt.AddValue(index++, OwnerGUID.Counter);             // guid
        stmt.AddValue(index++, Location.X);                    // posX
        stmt.AddValue(index++, Location.Y);                    // posY
        stmt.AddValue(index++, Location.Z);                    // posZ
        stmt.AddValue(index++, Location.Orientation);          // orientation
        stmt.AddValue(index++, Location.MapId);                // mapId
        stmt.AddValue(index++, (uint)CorpseData.DisplayID);    // displayId
        stmt.AddValue(index++, items.ToString());              // itemCache
        stmt.AddValue(index++, (byte)CorpseData.RaceID);       // race
        stmt.AddValue(index++, (byte)CorpseData.Class);        // class
        stmt.AddValue(index++, (byte)CorpseData.Sex);          // gender
        stmt.AddValue(index++, (uint)CorpseData.Flags);        // flags
        stmt.AddValue(index++, (uint)CorpseData.DynamicFlags); // dynFlags
        stmt.AddValue(index++, (uint)GhostTime);               // time
        stmt.AddValue(index++, (uint)CorpseType);              // corpseType
        stmt.AddValue(index++, InstanceId);                    // instanceId
        trans.Append(stmt);

        foreach (var phaseId in Location.PhaseShift.Phases.Keys)
        {
            index = 0;
            stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_CORPSE_PHASES);
            stmt.AddValue(index++, OwnerGUID.Counter); // OwnerGuid
            stmt.AddValue(index++, phaseId);           // PhaseId
            trans.Append(stmt);
        }

        foreach (var customization in CorpseData.Customizations)
        {
            index = 0;
            stmt = _characterDatabase.GetPreparedStatement(CharStatements.INS_CORPSE_CUSTOMIZATIONS);
            stmt.AddValue(index++, OwnerGUID.Counter); // OwnerGuid
            stmt.AddValue(index++, customization.ChrCustomizationOptionID);
            stmt.AddValue(index++, customization.ChrCustomizationChoiceID);
            trans.Append(stmt);
        }

        _characterDatabase.CommitTransaction(trans);
    }

    public void SetCellCoord(CellCoord cellCoord)
    {
        CellCoord = cellCoord;
    }

    public void SetClass(byte classId)
    {
        SetUpdateFieldValue(Values.ModifyValue(CorpseData).ModifyValue(CorpseData.Class), classId);
    }

    public void SetCorpseDynamicFlag(CorpseDynFlags dynamicFlags)
    {
        SetUpdateFieldFlagValue(Values.ModifyValue(CorpseData).ModifyValue(CorpseData.DynamicFlags), (uint)dynamicFlags);
    }

    public void SetCustomizations(List<ChrCustomizationChoice> customizations)
    {
        ClearDynamicUpdateFieldValues(Values.ModifyValue(CorpseData).ModifyValue(CorpseData.Customizations));

        foreach (var customization in customizations)
        {
            var newChoice = new ChrCustomizationChoice
            {
                ChrCustomizationOptionID = customization.ChrCustomizationOptionID,
                ChrCustomizationChoiceID = customization.ChrCustomizationChoiceID
            };

            AddDynamicUpdateFieldValue(Values.ModifyValue(CorpseData).ModifyValue(CorpseData.Customizations), newChoice);
        }
    }

    public void SetDisplayId(uint displayId)
    {
        SetUpdateFieldValue(Values.ModifyValue(CorpseData).ModifyValue(CorpseData.DisplayID), displayId);
    }

    public void SetFactionTemplate(int factionTemplate)
    {
        SetUpdateFieldValue(Values.ModifyValue(CorpseData).ModifyValue(CorpseData.FactionTemplate), factionTemplate);
    }

    public void SetGuildGUID(ObjectGuid guildGuid)
    {
        SetUpdateFieldValue(Values.ModifyValue(CorpseData).ModifyValue(CorpseData.GuildGUID), guildGuid);
    }

    public void SetItem(uint slot, uint item)
    {
        SetUpdateFieldValue(ref Values.ModifyValue(CorpseData).ModifyValue(CorpseData.Items, (int)slot), item);
    }

    public void SetOwnerGUID(ObjectGuid owner)
    {
        SetUpdateFieldValue(Values.ModifyValue(CorpseData).ModifyValue(CorpseData.Owner), owner);
    }

    public void SetPartyGUID(ObjectGuid partyGuid)
    {
        SetUpdateFieldValue(Values.ModifyValue(CorpseData).ModifyValue(CorpseData.PartyGUID), partyGuid);
    }

    public void SetRace(byte race)
    {
        SetUpdateFieldValue(Values.ModifyValue(CorpseData).ModifyValue(CorpseData.RaceID), race);
    }

    public void SetSex(byte sex)
    {
        SetUpdateFieldValue(Values.ModifyValue(CorpseData).ModifyValue(CorpseData.Sex), sex);
    }

    public override void Update(uint diff)
    {
        base.Update(diff);

        Loot?.Update();
    }
}