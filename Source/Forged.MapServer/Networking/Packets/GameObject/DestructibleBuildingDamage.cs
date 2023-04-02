// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.GameObject;

internal class DestructibleBuildingDamage : ServerPacket
{
    public ObjectGuid Caster;
    public int Damage;
    public ObjectGuid Owner;
    public uint SpellID;
    public ObjectGuid Target;
    public DestructibleBuildingDamage() : base(ServerOpcodes.DestructibleBuildingDamage, ConnectionType.Instance) { }

    public override void Write()
    {
        _worldPacket.WritePackedGuid(Target);
        _worldPacket.WritePackedGuid(Owner);
        _worldPacket.WritePackedGuid(Caster);
        _worldPacket.WriteInt32(Damage);
        _worldPacket.WriteUInt32(SpellID);
    }
}