// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Globals;

public class SceneTemplate
{
    public uint SceneId;
    public SceneFlags PlaybackFlags;
    public uint ScenePackageId;
    public bool Encrypted;
    public uint ScriptId;
}