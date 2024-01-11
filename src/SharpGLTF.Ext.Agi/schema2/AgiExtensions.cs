using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGLTF.Schema2
{
    using AGI;

    /// <summary>
    /// Extension methods for AGI glTF Extensions
    /// </summary>
    public static partial class AGIExtensions
    {
        private static bool _AgiRegistered;

        /// <summary>
        /// This method most be called once at application's startup to register the extensions.
        /// </summary>
        public static void RegisterExtensions()
        {
            if (_AgiRegistered) return;

            _AgiRegistered = true;

            ExtensionsFactory.RegisterExtension<ModelRoot, AgiRootArticulations>("AGI_articulations", p => new AgiRootArticulations(p));
            ExtensionsFactory.RegisterExtension<ModelRoot, AgiRootStkMetadata>("AGI_stk_metadata", p => new AgiRootStkMetadata(p));
            ExtensionsFactory.RegisterExtension<Node, AgiNodeArticulations>("AGI_articulations", p => new AgiNodeArticulations(p));
            ExtensionsFactory.RegisterExtension<Node, AgiNodeStkMetadata>("AGI_stk_metadata", p => new AgiNodeStkMetadata(p));
        }
    }
}
