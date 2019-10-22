using System;

namespace SharpGLTF
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0) return;

            System.IO.Directory.CreateDirectory(args[0]);

            var examples = new ExampleFiles(args[0]);

            examples.DownloadReferenceModels();
        }
    }
}
