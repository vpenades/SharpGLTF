using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using static System.FormattableString;

namespace SharpGLTF
{
    /// <summary>
    /// Utility class to hopefully compare breaking changes between APIs
    /// </summary>
    static class DumpAssemblyAPI
    {
        // https://www.hanselman.com/blog/ManagingChangeWithNETAssemblyDiffTools.aspx

        // proposed reporting lines:

        // Namespace="" Class="" Method=""
        // Namespace="" Enum="" Constant=""
        // Namespace="" Class="" Property=""
        // Namespace="" Class="" Event=""

        // NS foo.bar { STRUCT name        { FIELD name type       } }
        // NS foo.bar { ABSTRACTCLASS name { PROPERTYGET name type } }
        // NS foo.bar { ABSTRACTCLASS name { PROTECTEDPROPERTYSET name type } }

        // NamesPace { Class|Struct { Class|Struct|Method|Field } }


        public static IEnumerable<String> GetAssemblySignature(Assembly assembly)
        {
            return assembly.ExportedTypes
                .SelectMany(item => GetTypeSignature(item.GetTypeInfo()).Select(l => "NS " + item.Namespace + " { " + l + " } " ) );
        }

        public static IEnumerable<String> GetTypeSignature(TypeInfo tinfo)
        {
            if (tinfo.IsNestedPrivate) yield break;
            if (tinfo.IsNestedAssembly) yield break;

            string baseName = string.Empty;

            if (tinfo.IsNestedFamily) baseName += "PROTECTED";                        

            if (tinfo.IsInterface) baseName += "INTERFACE";
            else if (tinfo.IsEnum) baseName += "ENUM";
            else if (tinfo.IsValueType) baseName += "STRUCT";
            else if (tinfo.IsClass)
            {
                if (tinfo.IsSealed && tinfo.IsAbstract) baseName += "STATIC";
                else
                {
                    if (tinfo.IsSealed) baseName += "SEALED";
                    if (tinfo.IsAbstract) baseName += "ABSTRACT";
                }

                baseName += "CLASS";
            }

            baseName += " " + tinfo.GetFriendlyName();

            Object instance = null;

            try { instance = Activator.CreateInstance(tinfo); }
            catch { }

            foreach (var m in tinfo.DeclaredMembers)
            {
                var signatures = GetMemberSignature(instance, m);
                if (signatures == null) continue;
                foreach (var s in signatures)
                {
                    yield return baseName + " { " +  s + " } ";
                }
            }            
        }

        public static IEnumerable<string> GetMemberSignature(Object instance, MemberInfo minfo)
        {
            if (minfo is FieldInfo finfo) return GetFieldSignature(instance, finfo);

            if (minfo is TypeInfo tinfo) return GetTypeSignature(tinfo);

            if (minfo is PropertyInfo pinfo) return GetPropertySignature(instance, pinfo);

            if (minfo is MethodInfo xinfo) return GetMethodSignature(instance, xinfo);

            if (minfo is ConstructorInfo cinfo) return GetMethodSignature(instance, cinfo);

            return null;
        }

        public static IEnumerable<string> GetFieldSignature(Object instance, FieldInfo finfo)
        {
            if (!IsVisible(finfo)) yield break;

            var name = string.Empty;

            if (finfo.IsLiteral) name += "CONST";
            else name += "FIELD";

            name += $" {finfo.Name} {finfo.FieldType.GetFriendlyName()}";

            if (finfo.IsStatic)
            {
                name = "STATIC" + name;

                var v = finfo.GetValue(null);
                if (v != null) name += Invariant($"= {v}");
            }
            else if (instance != null)
            {
                var v = finfo.GetValue(instance);
                if (v != null) name += Invariant($"= {v}");
            }

            yield return name;
        }

        public static IEnumerable<string> GetPropertySignature(Object instance, PropertyInfo pinfo)
        {
            var pname = $"{pinfo.Name} {pinfo.PropertyType.GetFriendlyName()}";

            var getter = pinfo.GetGetMethod();
            if (IsVisible(getter,true)) yield return GetMethodModifiers(getter)+ "PROPERTYGET " + pname;

            var setter = pinfo.GetSetMethod();
            if (IsVisible(setter, true)) yield return GetMethodModifiers(getter) + "PROPERTYSET " + pname;
        }

        public static IEnumerable<string> GetMethodSignature(Object instance, MethodBase minfo)
        {
            // TODO: if parameters have default values, dump the same method multiple times with one parameter less each time.

            

            var mname = GetMethodModifiers(minfo);

            if (minfo is MethodInfo mminfo)
            {
                if (!IsVisible(minfo)) yield break;
                mname += "METHOD " + mminfo.Name + $" {mminfo.ReturnType.GetFriendlyName()} ";
            }

            if (minfo is ConstructorInfo cinfo)
            {
                if (!IsVisible(minfo,true)) yield break;
                mname += "CONSTRUCTOR ";
            }             

            var mparams = minfo.GetParameters()
                .Select(item => item.ParameterType.GetFriendlyName())
                .ToList();

            yield return mname + "(" + string.Join(", ", mparams) + ")";
        }

        public static bool IsVisible(FieldInfo finfo)
        {
            if (finfo == null) return false;

            if (finfo.IsPrivate) return false;
            if (finfo.IsAssembly) return false;            
            if (finfo.IsFamilyOrAssembly) return false;

            return true;
        }

        public static bool IsVisible(MethodBase minfo, bool withSpecials = false)
        {
            if (minfo == null) return false;

            if (minfo.IsSpecialName && !withSpecials) return false;

            if (minfo.IsPrivate) return false;            
            if (minfo.IsAssembly) return false;
            if (minfo.IsFamilyOrAssembly) return false;

            return true;
        }

        public static string GetMethodModifiers(MethodBase minfo)
        {
            if (minfo.IsPrivate) return string.Empty;

            var mod = string.Empty;

            if (minfo.IsFamily) mod += "PROTECTED";
            if (minfo.IsStatic) mod += "STATIC";            

            if (minfo.IsAbstract) mod += "ABSTRACT";
            else if (minfo.IsVirtual) mod += "VIRTUAL";

            return mod;
        }

        public static string GetFriendlyName(this Type tinfo)
        {
            return tinfo.GetTypeInfo().GetFriendlyName();
        }

        public static string GetFriendlyName(this TypeInfo tinfo)
        {
            if (!tinfo.IsGenericType) return tinfo.Name;

            var name = tinfo.Name;

            /*
            if (tinfo.Name == "System.Nullable`1")
            {
                return tinfo.GenericTypeParameters.First().Name + "?";
            }*/           

            name = name.Replace("`1","");
            name = name.Replace("`2", "");
            name = name.Replace("`3", "");
            name = name.Replace("`4", "");
            name = name.Replace("`5", "");
            name = name.Replace("`6", "");
            name = name.Replace("`7", "");
            name = name.Replace("`8", "");

            var gpm = tinfo
                .GenericTypeArguments
                .Select(item => GetFriendlyName(item.GetTypeInfo()))
                .ToList();

            if (gpm.Count > 0) name += "<" + string.Join(", ", gpm) + ">";

            return name;
            
        }
    }
}
