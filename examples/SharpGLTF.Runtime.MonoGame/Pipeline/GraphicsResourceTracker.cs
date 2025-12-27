using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework.Graphics;

namespace SharpGLTF.Runtime.Pipeline
{
    /// <summary>
    /// tracks all the disposable objects of a model during loading.    
    /// </summary>
    /// <remarks>
    /// During the process of loading a model, resources like textures, effects and device buffers<br/>
    /// are gathered as a collection of disposables, so when the whole model is disposed, we can also
    /// dispose of the device resources.
    /// </remarks>
    public class GraphicsResourceTracker
    {
        #region data

        private readonly List<GraphicsResource> _Disposables = new List<GraphicsResource>();        

        #endregion

        #region properties

        public IReadOnlyList<GraphicsResource> Disposables => _Disposables;

        #endregion

        #region API        
        public void AddDisposable(GraphicsResource resource)
        {
            if (resource == null) throw new ArgumentNullException();
            if (_Disposables.Contains(resource)) throw new ArgumentException();
            _Disposables.Add(resource);
        }

        #endregion
    }
}
