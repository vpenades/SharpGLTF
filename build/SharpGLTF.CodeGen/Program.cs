using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using NJsonSchema.References;

using JSONSCHEMA = NJsonSchema.JsonSchema;

namespace SharpGLTF
{
    using CodeGen;
    using SchemaReflection;    

    partial class Program
    {
        #region MAIN

        static void Main(string[] args)
        {
            SchemaDownload.Syncronize(Constants.RemoteSchemaRepo, Constants.LocalRepoDirectory);

            var processors = new List<SchemaProcessor>();

            // ---------------------------------------------- Add Main Schema

            processors.Add(new MainSchemaProcessor());

            // ---------------------------------------------- Add extensions

            // XMP
            processors.Add(new XMPExtension());

            // material extensions       
            processors.Add(new UnlitExtension());
            processors.Add(new IorExtension());
            processors.Add(new SheenExtension());
            processors.Add(new SpecularExtension());
            processors.Add(new ClearCoatExtension());
            processors.Add(new TransmissionExtension());
            processors.Add(new SpecularGlossinessExtension());

            // lights
            processors.Add(new LightsPunctualExtension());

            // gpu mesh instancing
            processors.Add(new MeshGpuInstancingExtension());

            // textures
            processors.Add(new TextureTransformExtension());
            processors.Add(new TextureDDSExtension());
            processors.Add(new TextureWebpExtension());
            processors.Add(new TextureKtx2Extension());

            processors.Add(new AgiArticulationsExtension());
            processors.Add(new AgiStkMetadataExtension());

            // ----------------------------------------------  process all files

            var processes = processors.SelectMany(item => item.Process());

            foreach (var (targetFileName, schema) in processes)
            {
                System.Console.WriteLine($"Emitting {targetFileName}...");

                SchemaProcessing.EmitCodeFromSchema(targetFileName, schema, processors);
            }
        }

        #endregion     
    }    
}
