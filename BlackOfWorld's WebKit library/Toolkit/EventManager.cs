using System;

namespace BlackOfWorld.Webkit.Toolkit
{
    internal class EventManager
    {
        public dynamic FireEvent(Delegate mevent,Object obj, EventArgs args)
        {
            return mevent?.DynamicInvoke(obj, args);
        }
    }
}
