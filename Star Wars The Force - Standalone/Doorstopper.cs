using System;

namespace TheForce_Standalone
{
    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method)]
    public class ReloadableAttribute : Attribute { }
}

