// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Forged.MapServer.DataStorage.Structs.E
{
    public sealed class EmotesRecord
    {
        public uint Id;
        public long RaceMask;
        public string EmoteSlashCommand;
        public int AnimId;
        public uint EmoteFlags;
        public byte EmoteSpecProc;
        public uint EmoteSpecProcParam;
        public uint EventSoundID;
        public uint SpellVisualKitId;
        public int ClassMask;
    }
}
