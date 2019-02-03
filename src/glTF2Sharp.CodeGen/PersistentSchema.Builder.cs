using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Epsylon.glTF2Toolkit.CodeGen
{
    static class PersistentSchema
    {
        public static SchemaType.Context Generate(NJsonSchema.CodeGeneration.CSharp.CSharpTypeResolver types)
        {
            var context = new SchemaType.Context();

            foreach(var t in types.Types.Keys)
            {
                context.UseType(t);
            }

            return context;
        }

        public static SchemaType UseType(this SchemaType.Context ctx, NJsonSchema.JsonSchema4 schema, bool isRequired = true)
        {
            if (ctx == null) throw new ArgumentNullException(nameof(ctx));
            if (schema == null) throw new ArgumentNullException(nameof(schema));

            

            if (schema is NJsonSchema.JsonProperty prop)
            {
                // System.Diagnostics.Debug.Assert(prop.Name != "scene");

                isRequired &= prop.IsRequired;                
            }


            if (_IsStringType(schema))
            {
                return ctx.UseString();
            }

            if (_IsBlittableType(schema))
            {
                bool isNullable = !isRequired;

                if (schema.Type == NJsonSchema.JsonObjectType.Integer) return ctx.UseBlittable(typeof(Int32).GetTypeInfo(), isNullable);
                if (schema.Type == NJsonSchema.JsonObjectType.Number) return ctx.UseBlittable(typeof(Double).GetTypeInfo(), isNullable);
                if (schema.Type == NJsonSchema.JsonObjectType.Boolean) return ctx.UseBlittable(typeof(Boolean).GetTypeInfo(), isNullable);                
                throw new NotImplementedException();
            }            

            if (schema.HasReference) return ctx.UseType(schema.ActualTypeSchema, isRequired); // we could have our own ref

            if (schema.IsArray)
            {
                return ctx.UseArray(ctx.UseType(schema.Item.ActualSchema));
            }

            if (_IsEnumeration(schema))
            {
                

                if (schema is NJsonSchema.JsonProperty property)
                {
                    bool isNullable = !isRequired;

                    var dict = new Dictionary<string, Int64>();

                    foreach (var v in property.AnyOf)
                    {
                        var val = v.Enumeration.FirstOrDefault();
                        var key = v.Description;

                        if (val is String) { key = (string)val; val = (Int64)0; }                        

                        if (string.IsNullOrWhiteSpace(key)) continue;

                        dict[key] = (Int64)val;
                    }

                    // JSon Schema AnyOf enumerations are basically anonymous, so we create
                    // a "shared name" with a concatenation of all the values:

                    var name = string.Join("-", dict.Keys.OrderBy(item => item));                    

                    var etype = ctx.UseEnum(name, isNullable);

                    foreach (var kvp in dict) etype.SetValue(kvp.Key, (int)kvp.Value);

                    if (dict.Values.Distinct().Count() > 1) etype.UseIntegers = true;                    

                    return etype;
                }

                throw new NotImplementedException();                
            }

            if (_IsDictionary(schema))
            {
                var key = ctx.UseString();
                var val = ctx.UseType(_GetDictionaryValue(schema));

                return ctx.UseDictionary(key, val);
            }

            if (_IsClass(schema))
            {
                var classDecl = ctx.UseClass(schema.Title);

                // process base class                

                if (schema.InheritedSchema != null) classDecl.BaseClass = ctx.UseType(schema.InheritedSchema) as ClassType;

                // filter declared properties

                var keys = _GetProperyNames(schema);
                if (schema.InheritedSchema != null) // filter our parent properties
                {
                    var baseKeys = _GetInheritedPropertyNames(schema).ToArray();
                    keys = keys.Except(baseKeys).ToArray();
                }
                
                // process declared properties

                var props = keys.Select(key => schema.Properties.Values.FirstOrDefault(item => item.Name == key));

                var required = schema.RequiredProperties;

                // System.Diagnostics.Debug.Assert(schema.Title != "Buffer View");

                foreach(var p in props)
                {
                    var field = classDecl.UseField(p.Name);
                    field.FieldType = ctx.UseType(p, required.Contains(p.Name));

                    field.MinimumValue = p.Minimum;
                    field.MaximumValue = p.Maximum;
                    field.DefaultValue = p.Default;

                    field.MinItems = p.MinItems;
                    field.MaxItems = p.MaxItems;
                }


                return classDecl;
            }

            if (schema.Type == NJsonSchema.JsonObjectType.Object) return ctx.UseAnyType();

            if (schema.Type == NJsonSchema.JsonObjectType.None) return ctx.UseAnyType();

            throw new NotImplementedException();
        }

        private static bool _IsBlittableType(NJsonSchema.JsonSchema4 schema)
        {
            if (schema == null) return false;
            if (schema.Type == NJsonSchema.JsonObjectType.Boolean) return true;
            if (schema.Type == NJsonSchema.JsonObjectType.Number) return true;
            if (schema.Type == NJsonSchema.JsonObjectType.Integer) return true;            

            return false;
        }        

        private static bool _IsStringType(NJsonSchema.JsonSchema4 schema)
        {
            return schema.Type == NJsonSchema.JsonObjectType.String;
        }

        private static bool _IsEnumeration(NJsonSchema.JsonSchema4 schema)
        {
            if (schema.Type != NJsonSchema.JsonObjectType.None) return false;

            if (schema.IsArray || schema.IsDictionary) return false;

            if (schema.AnyOf.Count == 0) return false;

            // if (!schema.IsEnumeration) return false; // useless

            return true;
        }

        private static bool _IsDictionary(NJsonSchema.JsonSchema4 schema)
        {
            // if (schema.Type != NJsonSchema.JsonObjectType.Object) return false;

            if (schema.AdditionalPropertiesSchema != null) return true;
            if (schema.AllowAdditionalProperties == false && schema.PatternProperties.Any()) return true;            

            return false;
        }        

        private static NJsonSchema.JsonSchema4 _GetDictionaryValue(NJsonSchema.JsonSchema4 schema)
        {
            if (schema.AdditionalPropertiesSchema != null)
            {
                return schema.AdditionalPropertiesSchema;
            }

            if (schema.AllowAdditionalProperties == false && schema.PatternProperties.Any())
            {
                var valueTypes = schema.PatternProperties.Values.ToArray();

                if (valueTypes.Length == 1) return valueTypes.First();
            }

            throw new NotImplementedException();
        }

        private static bool _IsClass(NJsonSchema.JsonSchema4 schema)
        {
            if (schema.Type != NJsonSchema.JsonObjectType.Object) return false;
            
            return !string.IsNullOrWhiteSpace(schema.Title);            
        }

        private static string[] _GetProperyNames(NJsonSchema.JsonSchema4 schema)
        {
            return schema
                    .Properties
                    .Values
                    .Select(item => item.Name)
                    .ToArray();
        }

        private static string[] _GetInheritedPropertyNames(NJsonSchema.JsonSchema4 schema)
        {
            if (schema?.InheritedSchema == null) return Enumerable.Empty<string>().ToArray();

            return _GetInheritedPropertyNames(schema.InheritedSchema)
                .Concat(_GetProperyNames(schema.InheritedSchema))
                .ToArray();
        }
    }
}
