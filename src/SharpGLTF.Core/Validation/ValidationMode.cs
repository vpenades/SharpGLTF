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
        /// Skips validation completely.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This mode is intended to be used in scenarios where you know the models you're loading are perfectly
        /// valid, and you want to skip validation because you want to speed up model loading.
        /// </para>
        /// <para>
        /// Using this mode for loading malformed glTF models is not supported nor recomended, because although the
        /// loading will not give any errors, it's impossible to guarantee the API will work correcly afterwards.
        /// </para>
        /// </remarks>
        Skip,

        /// <summary>
        /// In some specific cases, the file can be fixed, at which point the errors successfully
        /// fixed will be reported as warnings.
        /// </summary>
        TryFix,

        /// <summary>
        /// Full validation, any error throws an exception.
        /// </summary>
        Strict
    }
}
