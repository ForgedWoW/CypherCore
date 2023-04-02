// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.GameObject;

internal class GameObjectPlaySpellVisual : ServerPacket
{
    public ObjectGuid ActivatorGUID;
    public ObjectGuid ObjectGUID;
    public uint SpellVisualID;
    public GameObjectPlaySpellVisual() : base(ServerOpcodes.GameObjectPlaySpellVisual) { }

    public override void Write()
    {
        WorldPacket.WritePackedGuid(ObjectGUID);
        WorldPacket.WritePackedGuid(ActivatorGUID);
        WorldPacket.WriteUInt32(SpellVisualID);
    }
}