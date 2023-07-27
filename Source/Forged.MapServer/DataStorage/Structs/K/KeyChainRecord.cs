// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Forged.MapServer.DataStorage.Structs.K
{
    public sealed record KeyChainRecord
    {
        public uint Id;
        public byte[] Key = new byte[32];
    }
}