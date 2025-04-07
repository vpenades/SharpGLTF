using SharpGLTF.CodeGen;
using SharpGLTF.SchemaReflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SharpGLTF
{
    partial class Program
    {
        #region MAIN

        static void Main(string[] args)
        {
            SchemaDownload.Syncronize(Constants.RemoteSchemaRepo, Constants.LocalRepoDirectory);

            var processors = new List<SchemaProcessor>();

            // ---------------------------------------------- Add Main Schema

            processors.Add(new MainSchemaProcessor());

            // ---------------------------------------------- Add Khronos Extensions

            processors.AddRange(KhronosExtensions.GetExtensionsProcessors());

            // ---------------------------------------------- Add third party extensions

            processors.AddRange(AgiExtensions.GetExtensionsProcessors());
            processors.AddRange(CesiumExtensions.GetExtensionsProcessors());

            // ----------------------------------------------  process all files

            foreach (var processor in processors)
            {
                foreach (var (targetFileName, schema) in processor.ReadSchema())
                {
                    System.Console.WriteLine($"Emitting {targetFileName}...");

                    SchemaProcessing.EmitCodeFromSchema(processor.GetTargetProject(), targetFileName, schema, processors);
                }
            }
        }

        #endregion     
    }
}