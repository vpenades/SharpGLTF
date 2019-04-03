### Memory namespace
 
glTF2 stores array structures as encoded byte buffers that are not easy to handle directly.

Data is accessed by `Accessors` pointing to `BufferViews` which point to `Buffers`.

`Buffers` store raw Byte arrays, meanwhile `BufferViews` can be seen as slices over the
original byte array stored in a `Buffer`

This is equivalent in C# to `Byte[]` and `ArraySegment<Byte>` , a lot of the low level
API exploits this by using this analogy of reusing low level buffers.

Byte buffers is the lowest storage level of glTF, in order to expose the actual data, the
byte buffers need to be decoded into structured data. The information required to decode
the buffers is usually found in `Accessors`.

But the actual encoding and decoding, can be achieved using these wrappers found in the
`SharpGLTF.Memory` namespace:

- `IntegerArray`
- `ScalarArray`
- `Vector2Array`
- `Vector3Array`
- `Vector4Array`
- `QuaternionArray`
- `Matrix4x4Array`
- `MultiArray`
- `SparseArray`

You can use any of these structures to wrap any byte array and expose it as the given type.