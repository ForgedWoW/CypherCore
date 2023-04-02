// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Spell;

public class AuraUpdate : ServerPacket
{
    public List<AuraInfo> Auras = new();
    public ObjectGuid UnitGUID;
    public bool UpdateAll;
    public AuraUpdate() : base(ServerOpcodes.AuraUpdate, ConnectionType.Instance) { }

    public override void Write()
    {
        _worldPacket.WriteBit(UpdateAll);
        _worldPacket.WriteBits(Auras.Count, 9);

        foreach (var aura in Auras)
            aura.Write(_worldPacket);

        _worldPacket.WritePackedGuid(UnitGUID);
    }
}