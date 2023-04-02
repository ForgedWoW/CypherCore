﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Collections;

namespace Framework.Dynamic
{
    public class Reference<TO, FROM> : LinkedListElement where TO : class where FROM : class
    {
        private FROM _RefFrom;
        private TO _RefTo;

        public Reference()
        {
            _RefTo = null; _RefFrom = null;
        }

        public FROM Source => _RefFrom;

        public TO Target => _RefTo;

        // Link is invalid due to destruction of referenced target object. Call comes from the refTo object
        // Tell our refFrom object, that the link is cut
        public void Invalidate()                                   // the iRefFrom MUST remain!!
        {
            SourceObjectDestroyLink();
            Delink();
            _RefTo = null;
        }

        public bool IsValid()                                // Only check the iRefTo
        {
            return _RefTo != null;
        }

        // Create new link
        public void Link(TO toObj, FROM fromObj)
        {
            if (fromObj == null)
                return; // fromObj MUST not be NULL

            if (IsValid())
                Unlink();
            if (toObj != null)
            {
                _RefTo = toObj;
                _RefFrom = fromObj;
                TargetObjectBuildLink();
            }
        }

        public Reference<TO, FROM> Next()
        { return ((Reference<TO, FROM>)GetNextElement()); }

        public Reference<TO, FROM> Prev()
        { return ((Reference<TO, FROM>)GetPrevElement()); }

        // Tell our refFrom (source) object, that the link is cut (Target destroyed)
        public virtual void SourceObjectDestroyLink()
        { }

        // Tell our refTo (target) object that we have a link
        public virtual void TargetObjectBuildLink()
        { }

        // Tell our refTo (taget) object, that the link is cut
        public virtual void TargetObjectDestroyLink()
        { }

        // We don't need the reference anymore. Call comes from the refFrom object
        // Tell our refTo object, that the link is cut
        public void Unlink()
        {
            TargetObjectDestroyLink();
            Delink();
            _RefTo = null;
            _RefFrom = null;
        }
    }
}