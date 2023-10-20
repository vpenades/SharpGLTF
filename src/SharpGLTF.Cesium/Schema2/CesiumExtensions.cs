using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGLTF.Schema2
{
    /// <summary>
    /// Extension methods for Cesium glTF Extensions
    /// </summary>
    public static partial class CesiumExtensions
    {
        private static bool _CesiumRegistered;

        /// <summary>
        /// This method most be called once at application's startup to register the extensions.
        /// </summary>
        public static void RegisterExtensions()
        {
            if (_CesiumRegistered) return;

            _CesiumRegistered = true;

            ExtensionsFactory.RegisterExtension<MeshPrimitive, CesiumPrimitiveOutline>("CESIUM_primitive_outline");
        }
    }
}
