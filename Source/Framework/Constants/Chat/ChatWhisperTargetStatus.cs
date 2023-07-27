namespace Framework.Constants;

public enum ChatWhisperTargetStatus : byte
{
    CanWhisper      = 0,
    CanWhisperGuild = 1,
    Offline         = 2,
    WrongFaction    = 3
}
