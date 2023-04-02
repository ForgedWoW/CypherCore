// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Loot;

internal class StartLootRoll : ServerPacket
{
    public LootItemData Item = new();
    public ObjectGuid LootObj;
    public Array<LootRollIneligibilityReason> LootRollIneligibleReason = new(4);
    public int MapID;
    public LootMethod Method;
    public uint RollTime;
    public RollMask ValidRolls;
    public StartLootRoll() : base(ServerOpcodes.StartLootRoll) { }

    public override void Write()
    {
        _worldPacket.WritePackedGuid(LootObj);
        _worldPacket.WriteInt32(MapID);
        _worldPacket.WriteUInt32(RollTime);
        _worldPacket.WriteUInt8((byte)ValidRolls);

        foreach (var reason in LootRollIneligibleReason)
            _worldPacket.WriteUInt32((uint)reason);

        _worldPacket.WriteUInt8((byte)Method);
        Item.Write(_worldPacket);
    }
}