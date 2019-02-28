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


        public static IEnumerable<String> DumpAPI(Assembly assembly)
        {
            return assembly.ExportedTypes.SelectMany(item => DumpAPI(item.Namespace, item.GetTypeInfo()));
        }

        public static IEnumerable<String> DumpAPI(string baseName, TypeInfo type)
        {
            if (type.IsNestedPrivate) yield break;

            if (type.IsInterface) baseName += ".INTERFACE";
            if (type.IsEnum) baseName += ".ENUM";
            if (type.IsClass)
            {
                baseName += ".";
                if (type.IsSealed) baseName += "SEALED";
                if (type.IsAbstract) baseName += "ABSTRACT";
                baseName += "CLASS";
            }            

            baseName += "." + type.GetFriendlyName();

            Object instance = null;

            try { instance = System.Activator.CreateInstance(type); }
            catch { }

            foreach (var f in type.DeclaredFields)
            {
                if (f.IsPrivate) continue;

                var name = baseName;

                if (f.IsLiteral) name += ".CONST";
                else name += ".FIELD";

                name += $".{f.FieldType.GetFriendlyName()}.{f.Name}";

                if (f.IsStatic)
                {
                    var v = f.GetValue(null);
                    if (v != null) name += Invariant($"= {v}");
                }
                else if (instance != null)
                {
                    var v = f.GetValue(instance);
                    if (v != null) name += Invariant($"= {v}");
                }

                yield return name;
            }

            /* property getters and setters are already dumped by methods
            foreach (var p in type.DeclaredProperties)
            {
                var pname = $"{baseName}.PROPERTY.{p.PropertyType.GetFriendlyName()}.{p.Name}";

                var getter = p.GetGetMethod();
                if (getter != null && !getter.IsPrivate) yield return pname + ".Get()";

                var setter = p.GetSetMethod();
                if (setter != null && !setter.IsPrivate) yield return pname + ".Set()";                
            }
            */

            foreach(var m in type.DeclaredMethods)
            {
                // TODO: if parameters have default values, dump the same method multiple times with one parameter less each time.

                if (m.IsPrivate) continue;

                var mname = $"{baseName}.METHOD.{m.ReturnType.GetFriendlyName()}.{m.Name}";

                var mparams = m.GetParameters()
                    .Select(item => item.ParameterType.GetFriendlyName())
                    .ToList();

                yield return mname + "(" + string.Join(", ", mparams) + ")";
            }

            foreach(var n in type.DeclaredNestedTypes)
            {
                foreach (var nn in DumpAPI(baseName, n)) yield return nn;
            }
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

            var gpm = tinfo
                .GenericTypeArguments
                .Select(item => GetFriendlyName(item.GetTypeInfo()))
                .ToList();

            if (gpm.Count > 0) name += "<" + string.Join(", ", gpm) + ">";

            return name;
            
        }
    }
}
