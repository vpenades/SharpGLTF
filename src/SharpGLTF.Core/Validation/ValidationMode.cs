using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGLTF.Validation
{
    /// <summary>
    /// Defines validation modes for reading files.
    /// </summary>
    public enum ValidationMode
    {
        /// <summary>
        /// Skip validation completely.
        /// </summary>
        Skip,

        /// <summary>
        /// In some specific cases, the file can be fixed, at which point the errors successfully fixed will be reported as warnings.
        /// </summary>
        TryFix,

        /// <summary>
        /// Full validation, any error throws an exception.
        /// </summary>
        Strict
    }
}
