using System;
using System.Linq;
using System.Reflection;

namespace Sahlaysta.PortableTerrariaCommon
{

    /// <summary>
    /// Validation and descriptive error messages for using reflection.
    /// </summary>
    internal static class ReflectionHelper
    {

        public static Type GetType(Assembly assembly, string name)
        {
            return assembly.GetType(name, true);
        }

        public static MethodInfo GetMethod(Type type, string name, Type[] parameterTypes)
        {
            if (parameterTypes == null) parameterTypes = new Type[] { };
            MethodInfo methodInfo = type.GetMethod(name, parameterTypes);
            if (methodInfo == null)
            {
                throw new Exception("No found method " + name + " with parameter types { "
                    + (parameterTypes.Length == 0 ? " " : (string.Join(", ", (object[])parameterTypes)))
                    + " } in " + type);
            }
            return methodInfo;
        }

        public static PropertyInfo GetProperty(Type type, string name)
        {
            PropertyInfo propertyInfo = type.GetProperty(name);
            if (propertyInfo == null)
            {
                throw new Exception("Property " + name + " not found in " + type);
            }
            return propertyInfo;
        }

        public static ConstructorInfo GetConstructor(Type type, Type[] parameterTypes)
        {
            if (parameterTypes == null) parameterTypes = new Type[] { };
            ConstructorInfo constructorInfo = type.GetConstructor(parameterTypes);
            if (constructorInfo == null)
            {
                throw new Exception("No found constructor with parameter types {"
                    + (parameterTypes.Length == 0 ? " " : (string.Join(", ", (object[])parameterTypes)))
                    + " } in " + type);
            }
            return constructorInfo;
        }

        public static EventInfo GetEvent(Type type, string name)
        {
            EventInfo eventInfo = type.GetEvent(name);
            if (eventInfo == null)
            {
                throw new Exception("No found event " + name + " in " + type);
            }
            return eventInfo;
        }

        public static object GetEnumValue(Type type, string name)
        {
            if (!type.IsEnum)
            {
                throw new Exception("Type is not enum: " + type);
            }
            object enumValue = type.GetEnumValues().Cast<object>().FirstOrDefault(x => x.ToString() == name);
            if (enumValue == null)
            {
                throw new Exception("No found enum value " + name + " in " + type);
            }
            return enumValue;
        }

    }
}