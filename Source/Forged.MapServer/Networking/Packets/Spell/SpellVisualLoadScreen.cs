// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Spell;

internal class SpellVisualLoadScreen : ServerPacket
{
    public int Delay;
    public int SpellVisualKitID;

    public SpellVisualLoadScreen(int spellVisualKitId, int delay) : base(ServerOpcodes.SpellVisualLoadScreen, ConnectionType.Instance)
    {
        SpellVisualKitID = spellVisualKitId;
        Delay = delay;
    }

    public override void Write()
    {
        WorldPacket.WriteInt32(SpellVisualKitID);
        WorldPacket.WriteInt32(Delay);
    }
}