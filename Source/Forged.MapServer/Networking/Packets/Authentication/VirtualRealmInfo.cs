// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.Networking.Packets.Authentication;

internal struct VirtualRealmInfo
{
    public uint RealmAddress;

    // the virtual address of this realm, constructed as RealmHandle::Region << 24 | RealmHandle::Battlegroup << 16 | RealmHandle::Index
    public VirtualRealmNameInfo RealmNameInfo;

    public VirtualRealmInfo(uint realmAddress, bool isHomeRealm, bool isInternalRealm, string realmNameActual, string realmNameNormalized)
    {
        RealmAddress = realmAddress;
        RealmNameInfo = new VirtualRealmNameInfo(isHomeRealm, isInternalRealm, realmNameActual, realmNameNormalized);
    }

    public void Write(WorldPacket data)
    {
        data.WriteUInt32(RealmAddress);
        RealmNameInfo.Write(data);
    }
}