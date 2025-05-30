using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharpGLTF.Reflection
{
    /// <summary>
    /// Represents a reflected glTF property
    /// </summary>
    /// <remarks>
    /// This structure and its API is subject to change in the next versions, so for now avoid using it directly.
    /// </remarks>
    [System.Diagnostics.DebuggerDisplay("({ValueType}) {Name} = {Value}")]
    public readonly struct FieldInfo
    {
        #region diagnostics

        /// <summary>
        /// Verifies a path points to an existing property
        /// </summary>
        /// <param name="reflectionObject">The root object to reflect.</param>
        /// <param name="path">The reflection path.</param>
        /// <exception cref="ArgumentException">If the property pointed by the path does not exist.</exception>
        public static void Verify(IReflectionObject reflectionObject, string path)
        {
            if (path.Contains("/extras/", StringComparison.Ordinal)) return;

            var backingField = From(reflectionObject, path);
            if (backingField.IsEmpty) throw new ArgumentException($"{path} not found in the current model, add objects before animations, or disable verification.", nameof(path));
        }

        #endregion

        #region lifecycle

        /// <summary>
        /// Finds a child object using a pointer path
        /// </summary>
        /// <param name="reflectionObject">the root object</param>
        /// <param name="path">the path to the child object</param>
        /// <returns>a <see cref="FieldInfo"/> or null</returns>
        /// <example>
        /// "/nodes/0/rotation"
        /// </example>        
        public static FieldInfo From(IReflectionObject reflectionObject, string path)
        {
            while (path.Length > 0 && reflectionObject != null)
            {
                if (path[0] != '/') throw new ArgumentException($"invalid path: {path}", nameof(path));
                path = path.Substring(1);

                #if NETSTANDARD2_0
                var len = path.IndexOf('/');
                #else
                var len = path.IndexOf('/', StringComparison.Ordinal);
                #endif                
                if (len < 0) len = path.Length;
                
                var part = path.Substring(0, len);
                if (!reflectionObject.TryGetField(part, out var field)) return default;

                path = path.Substring(len);
                if (path.Length == 0) return field;

                reflectionObject = field.Value as IReflectionObject;
            }

            return default;
        }

        public static FieldInfo From<TInstance, TValue>(string name, TInstance instance, Func<TInstance, TValue> getter)
        {
            return new FieldInfo(name, typeof(TValue), instance, inst => getter.Invoke((TInstance)inst));
        }

        private FieldInfo(string name, Type valueType, Object instance, Func<Object, Object> getter)
        {
            this.Name = name;
            this.ValueType = valueType;
            this.Instance = instance;
            _Getter = getter;            
        }

        #endregion

        #region data

        public string Name { get; }

        private readonly Func<Object, Object> _Getter;

        public Object Instance { get; }

        #endregion

        #region properties

        public bool IsEmpty => _Getter == null;
        public Type ValueType { get; }
        public Object Value => _Getter.Invoke(Instance);

        #endregion
    }
}
