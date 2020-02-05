using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using static System.FormattableString;

namespace SharpGLTF
{
    /// <summary>
    /// Utility class to dump the public API of an assembly
    /// </summary>
    public static class DumpAssemblyAPI
    {
        // https://www.hanselman.com/blog/ManagingChangeWithNETAssemblyDiffTools.aspx

        public static IEnumerable<String> GetAssemblySignature(Assembly assembly)
        {
            return assembly.ExportedTypes
                .SelectMany(item => GetTypeSignature(item.GetTypeInfo()).Select(l => "NS " + item.Namespace + " { " + l + " }" ) );
        }

        public static IEnumerable<String> GetTypeSignature(TypeInfo tinfo)
        {
            if (tinfo.IsNestedPrivate) yield break;
            if (tinfo.IsNestedAssembly) yield break;

            string baseName = string.Empty;

            if (tinfo.IsInterface) baseName = "INTERFACE";
            else if (tinfo.IsEnum) baseName = "ENUM";
            else if (tinfo.IsValueType) baseName = "STRUCT";
            else if (tinfo.IsClass)
            {
                baseName = "CLASS";

                if (tinfo.IsSealed && tinfo.IsAbstract) baseName += ":STATIC";
                else
                {
                    if (tinfo.IsSealed) baseName += ":SEALED";
                    if (tinfo.IsAbstract) baseName += ":ABSTRACT";
                }
            }

            if (tinfo.IsNestedFamily) baseName += ":PROTECTED";

            baseName += " " + tinfo.GetQualifiedName();

            if (tinfo.IsEnum)
            {
                foreach(var val in Enum.GetValues(tinfo.AsType()))
                {
                    yield return baseName + " { " + val + $"={(int)val}" + " }";
                }

                yield break;
            }

            if (tinfo.IsClass)
            {
                // base classes
                var baseType = tinfo.BaseType;
                while (baseType != null && baseType != typeof(object))
                {
                    yield return baseName + " { USING " + baseType.GetQualifiedName() + " }";
                    baseType = baseType.BaseType;
                }
            }
            
            foreach (var ifaceType in tinfo.GetInterfaces())
            {
                if (ifaceType.IsNotPublic) continue;

                yield return baseName + " { USING " + ifaceType.GetQualifiedName() + " }";
            }

            foreach (var m in tinfo.DeclaredMembers)
            {
                var signatures = GetMemberSignature(m);
                if (signatures == null) continue;
                foreach (var s in signatures)
                {
                    yield return baseName + " { " +  s + " }";
                }
            }            
        }

        public static IEnumerable<String> GetMemberSignature(MemberInfo minfo)
        {
            if (minfo is FieldInfo finfo) return GetFieldSignature(finfo);

            if (minfo is TypeInfo tinfo) return GetTypeSignature(tinfo);

            if (minfo is PropertyInfo pinfo) return GetPropertySignature(pinfo);

            if (minfo is MethodInfo xinfo) return GetMethodSignature(xinfo);

            if (minfo is ConstructorInfo cinfo) return GetMethodSignature(cinfo);

            if (minfo is EventInfo einfo) return GetEventSignature(einfo);

            return null;
        }

        public static IEnumerable<String> GetFieldSignature(FieldInfo finfo)
        {
            if (!IsVisible(finfo)) yield break;

            var name = "FIELD";

            if (finfo.IsLiteral) name += ":CONST";
            else if (finfo.IsStatic) name += ":STATIC";

            if (finfo.IsInitOnly) name += ":READONLY";

            name += $" {finfo.Name} {finfo.FieldType.GetQualifiedName()}";

            yield return name;
        }

        public static IEnumerable<String> GetEventSignature(EventInfo einfo)
        {
            // if (!IsVisible(einfo)) yield break;

            var name = "EVENT";

            name += $" {einfo.Name} {einfo.EventHandlerType.GetQualifiedName()}";

            yield return name;
        }

        public static IEnumerable<String> GetPropertySignature(PropertyInfo pinfo)
        {
            var pname = $"{pinfo.Name} {pinfo.PropertyType.GetQualifiedName()}";

            var getter = pinfo.GetGetMethod();
            if (IsVisible(getter,true)) yield return "METHOD:GET"+ GetMethodModifiers(getter) + " " + pname;

            var setter = pinfo.GetSetMethod();
            if (IsVisible(setter, true)) yield return "METHOD:SET"+ GetMethodModifiers(setter) +" " + pname;
        }

        public static IEnumerable<String> GetMethodSignature(MethodBase minfo)
        {
            string mname = "METHOD";

            if (minfo is MethodInfo mminfo)
            {
                var isExtension = mminfo.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), true);

                if (isExtension) mname += ":EXTENSION";
                else mname += GetMethodModifiers(minfo);

                mname += " ";

                if (!IsVisible(minfo)) yield break;
                mname += mminfo.Name + $" {mminfo.ReturnType.GetQualifiedName()}";
            }

            if (minfo is ConstructorInfo cinfo)
            {
                if (!IsVisible(minfo,true)) yield break;
                mname += ":CONSTRUCTOR";
            }

            mname += " ";

            var mparams = minfo.GetParameters()
                .Select(item => GetParameterSignature(item))
                .ToList();            

            yield return mname + "(" + string.Join(", ", mparams) + ")";
        }

        public static String GetParameterSignature(ParameterInfo pinfo)
        {
            var prefix = string.Empty;

            if (pinfo.GetCustomAttribute(typeof(ParamArrayAttribute)) != null) prefix += "params ";

            if (pinfo.ParameterType.IsByRef)
            {
                if (pinfo.IsIn) prefix += "in ";
                else if (pinfo.IsOut) prefix += "out ";
                else prefix += "ref ";
            }

            var postfix = string.Empty;

            if (pinfo.HasDefaultValue)
            {
                if (pinfo.DefaultValue == null) postfix = "=null";
                else postfix = "=" + pinfo.DefaultValue.ToString();
            }

            return prefix + pinfo.ParameterType.GetQualifiedName() + postfix;
        }        

        public static Boolean IsVisible(MethodBase minfo, Boolean withSpecials = false)
        {
            if (minfo == null) return false;

            if (minfo.IsSpecialName && !withSpecials) return false;

            if (minfo.IsPrivate) return false;            
            if (minfo.IsAssembly) return false;
            if (minfo.IsFamilyOrAssembly) return false;

            return true;
        }

        public static Boolean IsVisible(FieldInfo finfo)
        {
            if (finfo == null) return false;

            if (finfo.IsPrivate) return false;
            if (finfo.IsAssembly) return false;
            if (finfo.IsFamilyOrAssembly) return false;

            return true;
        }

        public static String GetMethodModifiers(MethodBase minfo)
        {
            if (minfo.IsPrivate) return string.Empty;

            var mod = string.Empty;

            if (minfo.IsFamily) mod += ":PROTECTED";
            if (minfo.IsStatic) mod += ":STATIC";            

            if (minfo.IsAbstract) mod += ":ABSTRACT";
            else if (minfo.IsVirtual) mod += ":VIRTUAL";

            return mod;
        }

        public static String GetQualifiedName(this Type tinfo)
        {
            return tinfo.GetTypeInfo().GetQualifiedName();
        }

        public static String GetQualifiedName(this TypeInfo tinfo)
        {
            var postfix = string.Empty;

            // unwrap jagged array
            while (tinfo.IsArray)
            {
                postfix += "[" + string.Join("", Enumerable.Repeat(",", tinfo.GetArrayRank() - 1)) + "]";
                tinfo = tinfo.GetElementType().GetTypeInfo();                
            }

            var name = tinfo.Name;

            // remove pointer semantics
            name = name.Replace("&", ""); 

            // add jagged array postfix
            name += postfix;

            // handle generic types
            if (tinfo.IsGenericType || tinfo.IsGenericTypeDefinition)
            {
                name = name.Replace("`1", "");
                name = name.Replace("`2", "");
                name = name.Replace("`3", "");
                name = name.Replace("`4", "");
                name = name.Replace("`5", "");
                name = name.Replace("`6", "");
                name = name.Replace("`7", "");
                name = name.Replace("`8", "");

                var gpm = tinfo
                    .GenericTypeArguments
                    .Concat(tinfo.GenericTypeParameters)
                    .Select(item => GetQualifiedName(item))
                    .ToList();

                if (gpm.Count > 0) name += "<" + string.Join(",", gpm) + ">";                
            }

            return name;
        }
    }
}
