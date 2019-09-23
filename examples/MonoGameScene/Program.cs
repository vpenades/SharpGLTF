using System;
using System.Linq;

namespace MonoGameScene
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            using (var game = new Game1()) game.Run();
        }
    }
}
