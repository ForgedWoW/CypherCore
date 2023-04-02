// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Addon;

public struct AddOnInfo
{
    public bool Disabled;
    public bool Loaded;
    public string Name;
    public string Version;
    public void Read(WorldPacket data)
    {
        data.ResetBitPos();

        var nameLength = data.ReadBits<uint>(10);
        var versionLength = data.ReadBits<uint>(10);
        Loaded = data.HasBit();
        Disabled = data.HasBit();

        if (nameLength > 1)
        {
            Name = data.ReadString(nameLength - 1);
            data.ReadUInt8(); // null terminator
        }

        if (versionLength > 1)
        {
            Version = data.ReadString(versionLength - 1);
            data.ReadUInt8(); // null terminator
        }
    }
}