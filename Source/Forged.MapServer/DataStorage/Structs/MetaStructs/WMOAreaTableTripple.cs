// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Forged.MapServer.DataStorage.Structs.MetaStructs
{
    public struct WMOAreaTableTripple
    {
        public WMOAreaTableTripple(int r, int a, int g)
        {
            groupId = g;
            rootId = r;
            adtId = a;
        }

        // ordered by entropy; that way memcmp will have a minimal medium runtime
        int groupId;
        int rootId;
        int adtId;
    }
}
