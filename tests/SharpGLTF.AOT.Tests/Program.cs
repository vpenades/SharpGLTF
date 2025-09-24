using System.Diagnostics.CodeAnalysis;

using NUnitLite;

// https://medium.com/@skyake/guidance-for-net-nativeaot-4b9853c80f8a

namespace SharpGLTF
{
    internal class Program
    {
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(MainTests))] // this is confirmed as being required
        static int Main(string[] args)
        {
            return new AutoRun(typeof(Program).Assembly).Execute(args);
        }
    }
}
