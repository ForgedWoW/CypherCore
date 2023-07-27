using Forged.MapServer.DataStorage.ClientReader;
using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.C;

public sealed record ChatChannelsRecord
{
    public uint Id;
    public LocalizedString Name;
    public string Shortcut;
    public ChannelDBCFlags Flags;
    public sbyte FactionGroup;
    public int Ruleset;
}