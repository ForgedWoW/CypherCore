// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Spell;

internal class CustomLoadScreen : ServerPacket
{
    private readonly uint LoadingScreenID;
    private readonly uint TeleportSpellID;
    public CustomLoadScreen(uint teleportSpellId, uint loadingScreenId) : base(ServerOpcodes.CustomLoadScreen)
    {
        TeleportSpellID = teleportSpellId;
        LoadingScreenID = loadingScreenId;
    }

    public override void Write()
    {
        WorldPacket.WriteUInt32(TeleportSpellID);
        WorldPacket.WriteUInt32(LoadingScreenID);
    }
}