# SharpGLTF.Runtime

This namespace contains some utility classes to help rendering glTF models with
client graphics engines which typically use GPU accelerated resources.

The process is this; first of all we load a glTF model:

```c#
var model = SharpGLTF.Schema2.ModelRoot.Load("model.gltf");
```

Now, lets say you have an __AwesomeEngine__ which defines an __AwesomeMesh__ that
is the equivalent of a __glTF Mesh__, so for each _logical_ glTF Mesh we find in Model.LogicalMeshes,
we create the equivalent AwesomeMesh:
```c#
var awesomeMeshes = new AwesomeMesh[model.LogicalMeshes.Count];

for(int i=0; i < model.LogicalMeshes.Count; ++i)
{
    awesomeMeshes[i] = new AwesomeMesh(model.LogicalMeshes[i]);
}
```

Next, we create a scene template from the default glTF scene:
```c#

// a SceneTemplate is an immutable object that
// represent the resource asset in memory

var modelTemplate = SharpGLTF.Runtime.SceneTemplate(model.DefaultScene,true);

// SceneInstances are lightweight objects that reference
// the original template and can be animated separately.
// each SceneInstance can be set to a specific animation/time,
// and individual nodes can be edited at will, without affecting
// the state of sibling instances.

var inst1 = modelTemplate.CreateInstance();
    inst1.SetAnimationFrame("Walking", 2.17f);
    
var inst2 = modelTemplate.CreateInstance();
    inst2.SetAnimationFrame("Running", 3.523f);
    
var inst3 = modelTemplate.CreateInstance();
    inst3.SetAnimationFrame("Running", 1.32f);
    inst3.SetWorldMatrix("Head", Matrix.LookAt(...) ); // example of manually setting a single node matrix    
    
```

Finally, we render the instances like this:
```c#

RenderInstance(inst1, Matrix4x4.CreateTranslation(-10,0,0));
RenderInstance(inst2, Matrix4x4.CreateTranslation(  0,0,0));
RenderInstance(inst3, Matrix4x4.CreateTranslation( 10,0,0));

void RenderInstance(SharpGLTF.Runtime.SceneInstance modelInstance, Matrix4x4 modelMatrix)
{
    foreach(var drawable in modelInstance.DrawableInstances)
    {
        var awesomeMesh = awesomeMeshes[drawable.Template.LogicalMeshIndex];

        switch(drawable.Transform) // choosing the appropiate transform mode for each mesh drawing
        {
            case SharpGLTF.Transforms.RigidTransform rigidXform:
                AwesomeEngine.DrawRigidMesh(awesomeMesh, modelMatrix, rigidXform.WorldMatrix);
                break;

            case SharpGLTF.Transforms.SkinnedTransform skinXform:
                AwesomeEngine.DrawSkinnedMesh(awesomeMesh, modelMatrix, skinXform.SkinMatrices);
                break;

            case SharpGLTF.Transforms.InstancingTransform InstancingXform:                
                AwesomeEngine.DrawInstancedMeshes(awesomeMesh, modelMatrix, InstancingXform.WorldTransforms);
                break;
        }
    }
}

```

## Showcase

- [MonoScene](https://github.com/vpenades/MonoScene) integration with MonoGame (outdated)
- [Celeste64](https://github.com/ExOK/Celeste64) integration with a custom engine.








