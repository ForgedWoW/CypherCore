// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.GameObject;

internal class GameObjectPlaySpellVisual : ServerPacket
{
    public ObjectGuid ObjectGUID;
    public ObjectGuid ActivatorGUID;
    public uint SpellVisualID;
    public GameObjectPlaySpellVisual() : base(ServerOpcodes.GameObjectPlaySpellVisual) { }

    public override void Write()
    {
        _worldPacket.WritePackedGuid(ObjectGUID);
        _worldPacket.WritePackedGuid(ActivatorGUID);
        _worldPacket.WriteUInt32(SpellVisualID);
    }
}