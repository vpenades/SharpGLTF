# Mesh Building

One of key aspects of building a GPU optimized gltf model is to ensure that all
meshes in the model share as few vertex and index buffers as possible; in this
way, the number of state changes required to render a particular model is
reduced to a minimum.

Given the hierarchical nature of gltf, it is very difficult to populate the meshes
of a gltf model one by one, since we would need to "grow" the target buffers and
buffer views, which is far from trivial.

So, in order to solve the problem, we could have an external structure that could
prepare all the stuff required to fill the data; example:

```c#
class VertexColumn
{
    private string _Attribute;    
    private dimensions _Dimensions;    
    private readonly List<Vector4> _Rows = new List<Vector4>();
}

class VertexView
{
public VertexView(IEnumerable<VertexColumn> columns)
{
fill columns
}

private VertexColumn _Position;
private VertexColumn _Normals;

public int UseRow(params Vector4[] row);

public int UseRow(in SomeVertexStruct vertex);

}

class Indices
{    
    private readonly List<int> _Rows = new List<int();
    private primitiveType;
}

class MeshPrimitive
{
    private VertexBufferCreationMode _VbCreationMode; // split columns, interleaved, etc
    private List<VertexColumn> _Columns = new List<VertexColumn>();
    private Indices _Indices;
    private int _MaterialIndex;

    // morphing? how
}

class Mesh
{
private string Name;
private List<MeshPrimitive> _Primitives;
private List<float> _MorphWeights;

private List<Schema2.Node> _TargetNodes; // created mesh will be assigned to these
}


class VertexEncoding
{
Encoding;
bool normalized;
}

class MeshCollection
{
private Dicionaty<string,VertexEncoding> _VertexEncoding();

private readonly List<Mesh> _Meshes;


public IEnumerable<Schema2.Mesh> Commit(Schema2.ModelRoot root);
}



```






 