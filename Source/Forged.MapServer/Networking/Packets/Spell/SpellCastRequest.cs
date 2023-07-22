// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Networking.Packets.Movement;

namespace Forged.MapServer.Networking.Packets.Spell;

public class SpellCastRequest
{
    public ObjectGuid CastID;
    public ObjectGuid CraftingNPC;
    public ulong? CraftingOrderID;
    public uint[] Misc = new uint[2];
    public MissileTrajectoryRequest MissileTrajectory;
    public MovementInfo MoveUpdate;
    public Array<SpellExtraCurrencyCost> OptionalCurrencies = new(5 /*MAX_ITEM_EXT_COST_CURRENCIES*/);
    public Array<SpellCraftingReagent> OptionalReagents = new(6);
    public Array<SpellCraftingReagent> RemovedModifications = new(6);
    public uint SendCastFlags;
    public uint SpellID;
    public SpellTargetData Target = new();
    public SpellCastVisual Visual;
    public List<SpellWeight> Weight = new();

    public void Read(WorldPacket data)
    {
        CastID = data.ReadPackedGuid();
        Misc[0] = data.ReadUInt32();
        Misc[1] = data.ReadUInt32();
        SpellID = data.ReadUInt32();

        Visual.Read(data);

        MissileTrajectory.Read(data);
        CraftingNPC = data.ReadPackedGuid();

        var optionalCurrencies = data.ReadUInt32();
        var optionalReagents = data.ReadUInt32();
        var removedModificationsCount = data.ReadUInt32();

        for (var i = 0; i < optionalCurrencies; ++i)
            OptionalCurrencies[i].Read(data);

        SendCastFlags = data.ReadBits<uint>(5);
        var hasMoveUpdate = data.HasBit();
        var weightCount = data.ReadBits<uint>(2);
        var hasCraftingOrderID = data.HasBit();

        Target.Read(data);

        if (hasCraftingOrderID)
            CraftingOrderID = data.ReadUInt64();

        for (var i = 0; i < optionalReagents; ++i)
            OptionalReagents[i].Read(data);

        for (var i = 0; i < removedModificationsCount; ++i)
            RemovedModifications[i].Read(data);

        if (hasMoveUpdate)
            MoveUpdate = MovementExtensions.ReadMovementInfo(data);

        for (var i = 0; i < weightCount; ++i)
        {
            data.ResetBitPos();
            SpellWeight weight;
            weight.Type = data.ReadBits<uint>(2);
            weight.ID = data.ReadInt32();
            weight.Quantity = data.ReadUInt32();
            Weight.Add(weight);
        }
    }
}