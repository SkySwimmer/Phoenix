using System;

namespace Phoenix.Client
{
    /// <summary>
    /// Marks this type as a mod component
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ModComponent : Attribute
    {
    }
}