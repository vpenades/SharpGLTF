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
        /// Using this mode to force load malformed glTF models is not supported nor recomended, because although the
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
        /// <remarks>
        /// <para>
        /// This is the default mode. glTF ecosystem is still a [WIP] and evolving, so it's still fairly common
        /// that many exporters around generate malformed glTF models, which is better to catch early on loading
        /// to prevent causing further issues or misleading errors downstream. That's why although validation slows
        /// down loading time, I strongly encourage keeping validation enabled.
        /// </para>
        /// </remarks>
        Strict
    }
}
