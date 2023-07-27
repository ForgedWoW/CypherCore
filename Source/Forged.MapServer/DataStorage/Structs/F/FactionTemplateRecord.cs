using Framework.Constants;

namespace Forged.MapServer.DataStorage.Structs.F;

public sealed record FactionTemplateRecord
{
    static int MAX_FACTION_RELATIONS = 8;

    public uint Id;
    public ushort Faction;
    public ushort Flags;
    public byte FactionGroup;
    public byte FriendGroup;
    public byte EnemyGroup;
    public ushort[] Enemies = new ushort[MAX_FACTION_RELATIONS];
    public ushort[] Friend = new ushort[MAX_FACTION_RELATIONS];

    // helpers
    public bool IsFriendlyTo(FactionTemplateRecord entry)
    {
        if (this == entry)
            return true;

        if (entry.Faction != 0)
        {
            for (int i = 0; i < MAX_FACTION_RELATIONS; ++i)
                if (Enemies[i] == entry.Faction)
                    return false;
            for (int i = 0; i < MAX_FACTION_RELATIONS; ++i)
                if (Friend[i] == entry.Faction)
                    return true;
        }
        return (FriendGroup & entry.FactionGroup) != 0 || (FactionGroup & entry.FriendGroup) != 0;
    }
    public bool IsHostileTo(FactionTemplateRecord entry)
    {
        if (this == entry)
            return false;

        if (entry.Faction != 0)
        {
            for (int i = 0; i < MAX_FACTION_RELATIONS; ++i)
                if (Enemies[i] == entry.Faction)
                    return true;
            for (int i = 0; i < MAX_FACTION_RELATIONS; ++i)
                if (Friend[i] == entry.Faction)
                    return false;
        }
        return (EnemyGroup & entry.FactionGroup) != 0;
    }
    public bool IsHostileToPlayers() { return (EnemyGroup & (byte)FactionMasks.Player) != 0; }
    public bool IsNeutralToAll()
    {
        for (int i = 0; i < MAX_FACTION_RELATIONS; ++i)
            if (Enemies[i] != 0)
                return false;
        return EnemyGroup == 0 && FriendGroup == 0;
    }
    public bool IsContestedGuardFaction() { return (Flags & (ushort)FactionTemplateFlags.ContestedGuard) != 0; }
}