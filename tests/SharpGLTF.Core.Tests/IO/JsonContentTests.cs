using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Encodings.Web;

using NUnit.Framework;

namespace SharpGLTF.IO
{
    class _TestStructure
    {
        public string Author { get; set; }
        public int Integer1 { get; set; }
        public bool Bool1 { get; set; }
        public float Single1 { get; set; }
        public float Single2 { get; set; }
        public float Single3 { get; set; }

        // public float SinglePI { get; set; } // Fails on .Net Framework 471

        public double Double1 { get; set; }
        public double Double2 { get; set; }
        public double Double3 { get; set; }
        public double DoublePI { get; set; }

        public List<int> Array1 { get; set; }
        public List<float> Array2 { get; set; }

        public List<int[]> Array3 { get; set; }

        public List<_TestStructure2> Array4 { get; set; }

        public _TestStructure2 Dict1 { get; set; }
        public _TestStructure3 Dict2 { get; set; }
        
        public static Dictionary<string,object> CreateCompatibleDictionary()
        {
            var dict = new Dictionary<string, Object>();
            dict["author"] = "me";
            dict["integer1"] = 17;

            dict["bool1"] = true;

            dict["single1"] = 15.3f;
            dict["single2"] = 1.1f;
            dict["single3"] = -1.1f;
            // dict["singlePI"] = (float)Math.PI; // Fails on .Net Framework 471

            dict["double1"] = 15.3;
            dict["double2"] = 1.1;
            dict["double3"] = -1.1;
            dict["doublePI"] = Math.PI;

            dict["array1"] = new int[] { 1, 2, 3 };
            dict["array2"] = new float[] { 1.1f, 2.2f, 3.3f };

            dict["array3"] = new object[]
            {
                new int[] { 1,2,3 },
                new int[] { 4,5,6 }
            };

            dict["array4"] = new object[]
            {
                new Dictionary<string, int> { ["a0"] = 5, ["a1"] = 6 },
                new Dictionary<string, int> { ["a0"] = 7, ["a1"] = 8 }
            };

            dict["dict1"] = new Dictionary<string, int> { ["a0"] = 2, ["a1"] = 3 };
            dict["dict2"] = new Dictionary<string, Object>
            {
                ["a"] = 16,
                ["b"] = "delta",
                ["c"] = new List<int>() { 4, 6, 7 },
                ["d"] = new Dictionary<string, int> { ["a0"] = 1, ["a1"] = 2 }
            };

            if (!JsonContentTests.IsJsonRoundtripReady)
            {
                dict["array2"] = new float[] { 1, 2, 3 };
            }

            return dict;
        }
    }

    struct _TestStructure2
    {
        public int A0 { get; set; }
        public int A1 { get; set; }
    }

    class _TestStructure3
    {
        public int A { get; set; }
        public string B { get; set; }

        public int[] C { get; set; }

        public _TestStructure2 D { get; set; }
    }

    [Category("Core.IO")]
    public class JsonContentTests
    {
        public static bool IsJsonRoundtripReady
        {
            get
            {
                // when serializing a JsonContent object, it's important to take into account floating point values roundtrips.
                // it seems that prior NetCore3.1, System.Text.JSon was not roundtrip ready, so some values might have some
                // error margin when they complete a roundtrip.

                // On newer, NetCore System.Text.Json versions, it seems to use "G9" and "G17" text formatting are used.

                // https://devblogs.microsoft.com/dotnet/floating-point-parsing-and-formatting-improvements-in-net-core-3-0/            
                // https://github.com/dotnet/runtime/blob/76904319b41a1dd0823daaaaae6e56769ed19ed3/src/libraries/System.Text.Json/src/System/Text/Json/Writer/Utf8JsonWriter.WriteValues.Float.cs#L101

                // pull requests:
                // https://github.com/dotnet/corefx/pull/40408
                // https://github.com/dotnet/corefx/pull/38322
                // https://github.com/dotnet/corefx/pull/32268

                var framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;
                return !framework.StartsWith(".NET Framework 4");
            }
        }        

        public static bool AreEqual(System.Text.Json.Nodes.JsonNode a, System.Text.Json.Nodes.JsonNode b)
        {
            if (Object.ReferenceEquals(a, b)) return true;
            if (Object.ReferenceEquals(a, null)) return false;
            if (Object.ReferenceEquals(b, null)) return false;

            // A JsonContent ultimately represents a json block, so it seems fit to do the comparison that way.
            // also, there's the problem of floating point json writing, that can slightly change between
            // different frameworks.

            var ajson = a.ToJsonString();
            var bjson = b.ToJsonString();

            if (ajson == bjson) return true;

            // Net6.0 has better roundtrip handling that netstandard/net4
            #if NET6_0_OR_GREATER            
            return false;
            #endif

            return true;            
        }
    }

    [Category("Core.IO")]
    public class JsonSerializationTests
    {
        const string UNESCAPED = "你好";
        const string ESCAPED = "\u4f60\u597d";

        [Test]
        public void TestJsonExtendedCharacters()
        {
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            // create a test model

            var model = Schema2.ModelRoot.CreateModel();

            var extras = new System.Text.Json.Nodes.JsonArray();
            extras.Add(UNESCAPED);
            extras.Add(ESCAPED);
            extras.Add(UNESCAPED);

            model.Asset.Copyright = UNESCAPED;
            model.UseScene(UNESCAPED);
            model.Asset.Extras = extras;
            model.CreateImage().Content = Memory.MemoryImage.DefaultPngImage;

            // create write settings

            var joptions = new System.Text.Json.JsonWriterOptions
            {
                Indented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            var wsettings = new Schema2.WriteSettings();
            wsettings.JsonOptions = joptions;

            // model save-load roundtrip

            var roundtripPath = model.AttachToCurrentTest("extended 你好 characters.gltf", wsettings);
            var roundtripJson = System.IO.File.ReadAllText(roundtripPath);
            var roundtripModel = Schema2.ModelRoot.Load(roundtripPath);

            // checks

            TestContext.WriteLine(roundtripJson);

            Assert.IsTrue(roundtripJson.Contains("你好"));
            
            // https://github.com/KhronosGroup/glTF/issues/1978#issuecomment-831744624
            Assert.IsTrue(roundtripJson.Contains("extended%20%E4%BD%A0%E5%A5%BD%20characters.png"));

            Assert.IsTrue(roundtripModel.LogicalImages[0].Content.IsPng);
        }
    }    
}
