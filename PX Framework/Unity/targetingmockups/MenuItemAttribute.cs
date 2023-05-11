using System;

namespace UnityEditor
{
    [AttributeUsage(AttributeTargets.Method)]
    public class MenuItemAttribute : Attribute
    {
        public MenuItemAttribute(string itemName, bool isValidateFunction = false, int priority = 0) { }
    }
}