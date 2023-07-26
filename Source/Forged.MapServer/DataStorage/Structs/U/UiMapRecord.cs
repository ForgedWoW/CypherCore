// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Forged.MapServer.DataStorage.ClientReader;
using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.U
{
    public sealed class UiMapRecord
    {
        public LocalizedString Name;
        public uint Id;
        public int ParentUiMapID;
        public int Flags;
        public byte System;
        public UiMapType Type;
        public int BountySetID;
        public uint BountyDisplayLocation;
        public int VisibilityPlayerConditionID2; // if not met then map is skipped when evaluating UiMapAssignment
        public int VisibilityPlayerConditionID; // if not met then client checks other maps with the same AlternateUiMapGroup, not re-evaluating UiMapAssignment for them
        public sbyte HelpTextPosition;
        public int BkgAtlasID;
        public int AlternateUiMapGroup;
        public int ContentTuningID;

        public UiMapFlag GetFlags() { return (UiMapFlag)Flags; }
}
}
