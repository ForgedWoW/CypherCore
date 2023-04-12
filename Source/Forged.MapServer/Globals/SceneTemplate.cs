// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Forged.MapServer.Globals;

public class SceneTemplate
{
    public bool Encrypted { get; set; }
    public SceneFlags PlaybackFlags { get; set; }
    public uint SceneId { get; set; }
    public uint ScenePackageId { get; set; }
    public uint ScriptId { get; set; }
}