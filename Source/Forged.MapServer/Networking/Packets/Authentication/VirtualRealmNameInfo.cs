// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Forged.MapServer.Networking.Packets.Authentication;

internal struct VirtualRealmNameInfo
{
    public VirtualRealmNameInfo(bool isHomeRealm, bool isInternalRealm, string realmNameActual, string realmNameNormalized)
    {
        IsLocal = isHomeRealm;
        IsInternalRealm = isInternalRealm;
        RealmNameActual = realmNameActual;
        RealmNameNormalized = realmNameNormalized;
    }

    public void Write(WorldPacket data)
    {
        data.WriteBit(IsLocal);
        data.WriteBit(IsInternalRealm);
        data.WriteBits(RealmNameActual.GetByteCount(), 8);
        data.WriteBits(RealmNameNormalized.GetByteCount(), 8);
        data.FlushBits();

        data.WriteString(RealmNameActual);
        data.WriteString(RealmNameNormalized);
    }

    public bool IsLocal;               // true if the realm is the same as the account's home realm
    public bool IsInternalRealm;       // @todo research
    public string RealmNameActual;     // the name of the realm
    public string RealmNameNormalized; // the name of the realm without spaces
}