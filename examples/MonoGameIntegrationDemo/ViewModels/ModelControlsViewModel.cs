using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CommunityToolkit.Mvvm.ComponentModel;

using SharpGLTF.Runtime;
using SharpGLTF.Schema2;

namespace MonoGameIntegrationDemo.ViewModels
{
    public partial class ModelControlsViewModel : ViewModelBase
    {
        #region lifecycle
        public ModelControlsViewModel(ModelRoot loadModel, MonoGameModelInstance instance)
        {
            _LoadModel = loadModel;
            _Instance = instance;

            if (instance.Controller.Armature.AnimationTracks.Count > 0)
            {
                AnimationControls = new ModelAnimationControlsViewModel(_Instance);
            }
        }
        #endregion

        #region data

        private readonly SharpGLTF.Schema2.ModelRoot _LoadModel;
        private readonly MonoGameModelInstance _Instance;

        #endregion

        #region Properties

        [ObservableProperty]
        private ModelAnimationControlsViewModel? _AnimationControls;

        #endregion
    }

    public partial class ModelAnimationControlsViewModel : ViewModelBase
    {
        #region lifecycle
        public ModelAnimationControlsViewModel(MonoGameModelInstance instance)
        {
            _Instance = instance;
            _SelectedTrack = AnimationTracks.FirstOrDefault();
        }

        #endregion

        #region data

        private readonly MonoGameModelInstance _Instance;

        #endregion

        #region Properties

        public IReadOnlyList<AnimationTrackInfo> AnimationTracks => _Instance.Controller.Armature.AnimationTracks;

        [ObservableProperty]
        private AnimationTrackInfo? _SelectedTrack;

        private float _AnimationTime;        

        public float AnimationTime
        {
            get => _AnimationTime;
            set
            {
                _AnimationTime = value;

                var trackIdx = _SelectedTrack == null
                    ? -1
                    : AnimationTracks.ToList().IndexOf(_SelectedTrack);

                if (trackIdx < 0) return;


                _Instance.Controller.Armature.SetAnimationFrame(trackIdx, _AnimationTime);
            }
        }

        #endregion
    }
}
