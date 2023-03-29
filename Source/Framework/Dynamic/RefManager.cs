// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Collections;

namespace Framework.Dynamic;

public class RefManager<TO, FROM> : LinkedListHead where TO : class where FROM : class
{
    public Reference<TO, FROM> GetFirst()
    {
        return (Reference<TO, FROM>)GetFirstElement();
    }

    public Reference<TO, FROM> GetLast()
    {
        return (Reference<TO, FROM>)GetLastElement();
    }

    public void ClearReferences()
    {
        Reference<TO, FROM> refe;

        while ((refe = GetFirst()) != null)
            refe.Invalidate();
    }

    ~RefManager()
    {
        ClearReferences();
    }
}