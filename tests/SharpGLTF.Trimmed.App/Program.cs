using System;
using SharpGLTF.Schema2;

namespace SharpGLTF
{
    class Program
    {
        public static void Main(string[] args)
        {
            ModelRoot
                .Load(args[0])
                .Save(args[1]);
        }
    }
}