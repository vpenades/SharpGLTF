using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;

namespace SharpGLTF.Schema2
{
    public static partial class Toolkit
    {
        public static Accessor CreateVertexAccessor(this ModelRoot root, Memory.MemoryAccessor memAccessor)
        {
            Guard.NotNull(root, nameof(root));
            Guard.NotNull(memAccessor, nameof(memAccessor));

            var accessor = root.CreateAccessor(memAccessor.Attribute.Name);

            accessor.SetVertexData(memAccessor);

            return accessor;
        }

        public static unsafe BufferView CreateBufferView<T>(this ModelRoot root, IReadOnlyList<T> data)
            where T : unmanaged
        {
            var view = root.CreateBufferView(sizeof(T) * data.Count);

            if (typeof(T) == typeof(int))
            {
                new Memory.IntegerArray(view.Content, IndexEncodingType.UNSIGNED_INT).Fill(data as IReadOnlyList<int>);
                return view;
            }

            if (typeof(T) == typeof(Single))
            {
                new Memory.ScalarArray(view.Content).Fill(data as IReadOnlyList<Single>);
                return view;
            }

            if (typeof(T) == typeof(Vector2))
            {
                new Memory.Vector2Array(view.Content).Fill(data as IReadOnlyList<Vector2>);
                return view;
            }

            if (typeof(T) == typeof(Vector3))
            {
                new Memory.Vector3Array(view.Content).Fill(data as IReadOnlyList<Vector3>);
                return view;
            }

            if (typeof(T) == typeof(Vector4))
            {
                new Memory.Vector4Array(view.Content).Fill(data as IReadOnlyList<Vector4>);
                return view;
            }

            if (typeof(T) == typeof(Quaternion))
            {
                new Memory.QuaternionArray(view.Content).Fill(data as IReadOnlyList<Quaternion>);
                return view;
            }

            if (typeof(T) == typeof(Matrix4x4))
            {
                new Memory.Matrix4x4Array(view.Content).Fill(data as IReadOnlyList<Matrix4x4>);
                return view;
            }

            throw new ArgumentException(typeof(T).Name);
        }
    }
}
