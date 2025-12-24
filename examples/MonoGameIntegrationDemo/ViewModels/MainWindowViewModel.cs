using System.Collections.Generic;
using System.Threading.Tasks;

using Avalonia.Platform.Storage;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Microsoft.Xna.Framework;

namespace MonoGameIntegrationDemo.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private readonly Models.MonoGameContext _CurrentGame = new Models.MonoGameContext();
        
        public Game CurrentGame => _CurrentGame;

        [ObservableProperty]
        private ModelControlsViewModel? _ModelControls;


        [RelayCommand]
        public async Task LoadModelAsync(IReadOnlyList<Avalonia.Platform.Storage.IStorageFile> files)
        {            
            var modelPath = files[0].TryGetLocalPath();

            var model = await Task.Run(() => SharpGLTF.Schema2.ModelRoot.Load(modelPath));

            var inst = _CurrentGame.SetModel(model);

            ModelControls = new ModelControlsViewModel(model, inst);
        }
    }
}
