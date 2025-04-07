using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using JSONSCHEMA = NJsonSchema.JsonSchema;

namespace SharpGLTF.SchemaReflection
{
    /// <summary>
    /// Utility class that reads a json schema and creates a <see cref="SchemaType.Context"/>
    /// Which is a collection of Schema Types resembling <see cref="System.Type"/>
    /// </summary>
    static class SchemaTypesReader
    {
        // issues related to gltf2 schema parsing:
        // https://github.com/RSuter/NJsonSchema
        // https://github.com/RSuter/NJsonSchema/issues/378
        // https://github.com/RSuter/NJsonSchema/issues/377

        #region API

        public static SchemaType.Context Generate(NJsonSchema.CodeGeneration.CSharp.CSharpTypeResolver types)
        {
            var context = new SchemaType.Context();

            foreach(var t in types.Types.Keys)
            {
                context._UseType(t);
            }

            return context;
        }

        #endregion

        #region core

        private static SchemaType _UseType(this SchemaType.Context ctx, JSONSCHEMA schema, bool isRequired = true)
        {
            ArgumentNullException.ThrowIfNull(ctx);
            ArgumentNullException.ThrowIfNull(schema);

            if (schema is NJsonSchema.JsonSchemaProperty prop)
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

            if (schema.HasReference) return ctx._UseType(schema.ActualTypeSchema, isRequired); // we could have our own ref

            if (schema.IsArray)
            {
                return ctx.UseArray(ctx._UseType(schema.Item.ActualSchema));
            }

            if (_IsEnumeration(schema))
            {
                if (schema is NJsonSchema.JsonSchemaProperty property)
                {
                    bool isNullable = !isRequired;

                    var dict = new Dictionary<string, Int64>();

                    foreach (var v in property.AnyOf)
                    {
                        var key = v.Description;
                        var val = v.Enumeration?.FirstOrDefault();
                        var ext = v.ExtensionData?.FirstOrDefault() ?? default;                        

                        if (val is String txt)
                        {
                            System.Diagnostics.Debug.Assert(v.Type == NJsonSchema.JsonObjectType.None);

                            key = txt; val = (Int64)0;
                        }

                        if (v.Type == NJsonSchema.JsonObjectType.None && ext.Key == "const")
                        {
                            key = (string)ext.Value; val = (Int64)0;
                        }

                        if (v.Type == NJsonSchema.JsonObjectType.Integer && ext.Key == "const")
                        {
                            val = (Int64)ext.Value;
                        }

                        System.Diagnostics.Debug.Assert(key != null || dict.Count > 0);

                        if (string.IsNullOrWhiteSpace(key)) continue;

                        dict[key] = (Int64)val;
                    }

                    // JSon Schema AnyOf enumerations are basically anonymous, so we create
                    // a "shared name" with a concatenation of all the values:

                    var name = string.Join("-", dict.Keys.OrderBy(item => item));                    

                    var etype = ctx.UseEnum(name, isNullable);

                    etype.Identifier = _GetSchemaIdentifier(schema);
                    etype.Description = schema.Description;

                    foreach (var kvp in dict) etype.SetValue(kvp.Key, (int)kvp.Value);

                    if (dict.Values.Distinct().Count() > 1) etype.UseIntegers = true;                    

                    return etype;
                }

                throw new NotImplementedException();                
            }

            if (_IsDictionary(schema))
            {
                var key = ctx.UseString();
                var val = ctx._UseType(_GetDictionaryValue(schema));

                return ctx.UseDictionary(key, val);
            }

            if (_IsClass(schema))
            {
                var classDecl = ctx.UseClass(schema.Title);

                classDecl.Identifier = _GetSchemaIdentifier(schema);
                classDecl.Description = schema.Description;

                // process base class                

                if (schema.InheritedSchema != null) classDecl.BaseClass = ctx._UseType(schema.InheritedSchema) as ClassType;

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

                    field.Description = p.Description;

                    field.FieldType = ctx._UseType(p, required.Contains(p.Name));

                    field.ExclusiveMinimumValue = p.ExclusiveMinimum ?? (p.IsExclusiveMinimum ? p.Minimum : null);
                    field.InclusiveMinimumValue = p.IsExclusiveMinimum ? null : p.Minimum;
                    field.DefaultValue = p.Default;
                    field.InclusiveMaximumValue = p.IsExclusiveMaximum ? null : p.Maximum;
                    field.ExclusiveMaximumValue = p.ExclusiveMaximum ?? (p.IsExclusiveMaximum ? p.Maximum : null);

                    field.MinItems = p.MinItems;
                    field.MaxItems = p.MaxItems;
                }


                return classDecl;
            }

            if (schema.Type == NJsonSchema.JsonObjectType.Object) return ctx.UseAnyType();

            if (schema.Type == NJsonSchema.JsonObjectType.None) return ctx.UseAnyType();

            throw new NotImplementedException();
        }

        private static string _GetSchemaIdentifier(JSONSCHEMA schema)
        {
            if (schema.ExtensionData != null)
            {
                if (schema.ExtensionData.TryGetValue("$id", out var value) && value is string id)
                {
                    return id;
                }
            }

            if (!string.IsNullOrWhiteSpace(schema.DocumentPath))
            {
                return System.IO.Path.GetFileName(schema.DocumentPath);
            }

            return null;

            
        }

        private static bool _IsBlittableType(JSONSCHEMA schema)
        {
            if (schema == null) return false;
            if (schema.Type == NJsonSchema.JsonObjectType.Boolean) return true;
            if (schema.Type == NJsonSchema.JsonObjectType.Number) return true;
            if (schema.Type == NJsonSchema.JsonObjectType.Integer) return true;            

            return false;
        }        

        private static bool _IsStringType(JSONSCHEMA schema)
        {
            return schema.Type == NJsonSchema.JsonObjectType.String;
        }

        private static bool _IsEnumeration(JSONSCHEMA schema)
        {
            if (schema.Type != NJsonSchema.JsonObjectType.None) return false;

            if (schema.IsArray || schema.IsDictionary) return false;

            if (schema.AnyOf.Count == 0) return false;

            // if (!schema.IsEnumeration) return false; // useless

            return true;
        }

        private static bool _IsDictionary(JSONSCHEMA schema)
        {
            // if (schema.Type != NJsonSchema.JsonObjectType.Object) return false;

            if (schema.AdditionalPropertiesSchema != null) return true;
            if (schema.AllowAdditionalProperties == false && schema.PatternProperties.Any()) return true;            

            return false;
        }        

        private static JSONSCHEMA _GetDictionaryValue(JSONSCHEMA schema)
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

        private static bool _IsClass(JSONSCHEMA schema)
        {
            if (schema.Type != NJsonSchema.JsonObjectType.Object) return false;
            
            return !string.IsNullOrWhiteSpace(schema.Title);            
        }

        private static string[] _GetProperyNames(JSONSCHEMA schema)
        {
            return schema
                    .Properties
                    .Values
                    .Select(item => item.Name)
                    .ToArray();
        }

        private static string[] _GetInheritedPropertyNames(JSONSCHEMA schema)
        {
            if (schema?.InheritedSchema == null) return Enumerable.Empty<string>().ToArray();

            return _GetInheritedPropertyNames(schema.InheritedSchema)
                .Concat(_GetProperyNames(schema.InheritedSchema))
                .ToArray();
        }

        #endregion
    }
}
