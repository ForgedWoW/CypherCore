namespace Forged.MapServer.Globals;

public class LanguageDesc
{
    public uint SpellId;
    public uint SkillId;

    public LanguageDesc() { }

    public LanguageDesc(uint spellId, uint skillId)
    {
        SpellId = spellId;
        SkillId = skillId;
    }

    public override int GetHashCode()
    {
        return SpellId.GetHashCode() ^ SkillId.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        if (obj is LanguageDesc)
            return (LanguageDesc)obj == this;

        return false;
    }

    public static bool operator ==(LanguageDesc left, LanguageDesc right)
    {
        return left.SpellId == right.SpellId && left.SkillId == right.SkillId;
    }

    public static bool operator !=(LanguageDesc left, LanguageDesc right)
    {
        return !(left == right);
    }
}