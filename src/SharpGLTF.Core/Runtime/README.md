# SharpGLTF.Runtime

This namespace contains some utility classes to help rendering glTF models with
client graphics engines which typically use GPU accelerated resources.

The process is this; first of all we load a glTF model:

```c#
var model = SharpGLTF.Schema2.ModelRoot.Load("model.gltf");
```

Now, lets say you have an __AwesomeEngine__ which defines an __AwesomeMesh__ that
is the equivalent of a __glTF Mesh__, so for each glTF Mesh we find in Model.LogicalMeshes,
we create the equivalent AwesomeMesh:
```c#
var gpuMeshes = new AwesomeMesh[model.LogicalMeshes.Count];
for(int i=0; i < model.LogicalMeshes.Count; ++i)
{
    gpuMeshes = new AwesomeMesh(model.LogicalMeshes[i]);
}
```

Next, we create a scene template from the default glTF scene:
```c#
var modelTemplate = SharpGLTF.Runtime.SceneTemplate(model.DefaultScene,true);

var modelInstance = modelTemplate.CreateInstance();
```

Finally, in our render call, we render the meshes like this:
```c#
void RenderFrame(Matrix4x4 modelMatrix)
{
    foreach(var drawable in modelInstance.DrawableReferences)
    {
        var gpuMesh = gpuMeshes[drawable.Item1];

        if (drawable.Item2 is SharpGLTF.Transforms.StaticTransform statXform)
        {
            AwesomeEngine.DrawMesh(gpuMesh, modelMatrix, statXform.WorldMatrix);
        }

        if (drawable.Item2 is SharpGLTF.Transforms.SkinTransform skinXform)
        {
            AwesomeEngine.DrawMesh(gpuMesh, modelMatrix, skinXform.SkinMatrices);
        }
    }
}

```







