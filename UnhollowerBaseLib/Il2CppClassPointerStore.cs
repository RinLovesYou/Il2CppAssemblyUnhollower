using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnhollowerBaseLib.Attributes;

namespace UnhollowerBaseLib
{
    public static class Il2CppClassPointerStore<T>
    {
        public static IntPtr NativeClassPtr;
        public static Type CreatedTypeRedirect;

        static Il2CppClassPointerStore()
        {
            var targetType = typeof(T);
            if (!targetType.IsEnum)
            {
                RuntimeHelpers.RunClassConstructor(targetType.TypeHandle);
            }
            else
            {
                // Enums can't have cctors under .net runtime
                // Need to grab class pointer manually
                if (targetType.IsNested)
                {
                    var ptrStore = typeof(Il2CppClassPointerStore<>).MakeGenericType(targetType.DeclaringType);
                    var field = ptrStore.GetField(nameof(NativeClassPtr));
                    var declaringTypePtr = (IntPtr) field.GetValue(null);
                    NativeClassPtr = IL2CPP.GetIl2CppNestedType(declaringTypePtr, targetType.Name);
                }
                else
                    NativeClassPtr = IL2CPP.GetIl2CppClass(targetType.Module.Name, targetType.Namespace ?? "", targetType.Name);
            }

            if (targetType.IsPrimitive || targetType == typeof(string))
            {
                RuntimeHelpers.RunClassConstructor(AppDomain.CurrentDomain.GetAssemblies()
                    .Single(it => it.GetName().Name == "Il2Cppmscorlib").GetType("Il2Cpp" + targetType.FullName)
                    .TypeHandle);
            }

            foreach (var customAttribute in targetType.CustomAttributes)
            {
                if (customAttribute.AttributeType != typeof(AlsoInitializeAttribute)) continue;

                var linkedType = (Type) customAttribute.ConstructorArguments[0].Value;
                RuntimeHelpers.RunClassConstructor(linkedType.TypeHandle);
            }
        }
    }
}