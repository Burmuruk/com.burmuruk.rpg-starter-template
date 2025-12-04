using System;
using System.Reflection;

namespace Burmuruk.RPGStarterTemplate.Utilities
{
    [AttributeUsage(AttributeTargets.Field)]
    public class DisallowNegativeAttribute : Attribute
    {
        public static bool ValidateUsage(FieldInfo field)
        {
            if (field.FieldType != typeof(int) &&
                field.FieldType != typeof(float) &&
                field.FieldType != typeof(double))
            {
                return false;
            }

            return true;
        }
    }
}
