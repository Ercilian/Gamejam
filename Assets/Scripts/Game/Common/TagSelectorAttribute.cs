using System;

namespace Game.Common
{
    // Marca un string para que el Inspector muestre un dropdown de Tags
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class TagSelectorAttribute : Attribute
    {
    }
}
