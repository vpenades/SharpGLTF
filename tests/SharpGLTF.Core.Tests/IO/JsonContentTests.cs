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

        

        public static bool AreEqual(JsonContent a, JsonContent b)
        {
            if (Object.ReferenceEquals(a.Content, b.Content)) return true;
            if (Object.ReferenceEquals(a.Content, null)) return false;
            if (Object.ReferenceEquals(b.Content, null)) return false;

            // A JsonContent ultimately represents a json block, so it seems fit to do the comparison that way.
            // also, there's the problem of floating point json writing, that can slightly change between
            // different frameworks.

            var ajson = a.ToJson();
            var bjson = b.ToJson();

            if (ajson != bjson) return false;

            Assert.IsTrue(JsonContent.AreEqualByContent(a, b, 0.0001f));

            return true;
        }

        [Test]
        public void TestFloatingPointJsonRoundtrip()
        {
            float value = 1.1f; // serialized by system.text.json as 1.1000002f

            var valueTxt = value.ToString("G9", System.Globalization.CultureInfo.InvariantCulture);

            var dict = new Dictionary<string, Object>();            
            dict["value"] = value;            

            JsonContent a = JsonContent.CreateFrom(dict);

            // roundtrip to json
            var json = a.ToJson();
            TestContext.Write(json);
            var b = IO.JsonContent.Parse(json);            

            Assert.IsTrue(AreEqual(a, b));            
        }

        [Test]
        public void CreateJsonContent()
        {
            JsonContent a = JsonContent.CreateFrom(_TestStructure.CreateCompatibleDictionary());
            
            // roundtrip to json
            var json = a.ToJson();
            TestContext.Write(json);
            var b = IO.JsonContent.Parse(json);            

            // roundtrip to a runtime object
            var x = a.Deserialize(typeof(_TestStructure));
            var c = JsonContent.Serialize(x);

            Assert.IsTrue(AreEqual(a, b));
            Assert.IsTrue(AreEqual(a, c));            

            foreach (var dom in new[] { a, b, c })
            {
                Assert.AreEqual("me", dom.GetValue<string>("author"));
                Assert.AreEqual(17, dom.GetValue<int>("integer1"));
                Assert.AreEqual(15.3f, dom.GetValue<float>("single1"));
                Assert.AreEqual(3, dom.GetValue<int>("array1", 2));
                Assert.AreEqual(2, dom.GetValue<int>("dict2", "d", "a1"));
            }

            Assert.AreEqual(b.GetHashCode(), c.GetHashCode());
            Assert.AreEqual(b, c);

            // clone & compare

            var d = c.DeepClone();

            Assert.AreEqual(c.GetHashCode(), d.GetHashCode());
            Assert.AreEqual(c, d);
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
            TestContext.CurrentContext.AttachShowDirLink();
            TestContext.CurrentContext.AttachGltfValidatorLinks();

            // create a test model

            var model = Schema2.ModelRoot.CreateModel();

            model.Asset.Copyright = UNESCAPED;
            model.UseScene(UNESCAPED);
            model.Asset.Extras = JsonContent.CreateFrom(new string[] { UNESCAPED, ESCAPED, UNESCAPED });
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

    [Category("Core.IO")]
    public class ContextTests
    {
        [Test]
        public void TestCurrentDirectoryLoad()
        {
            // for some reason, System.IO.Path.GetFullPath() does not recogninze an empty string as the current directory.

            var currDirContext0 = Schema2.ReadContext.CreateFromDirectory(string.Empty);
            Assert.NotNull(currDirContext0);            
        }
    }
}
