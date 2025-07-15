using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;

namespace SharpGLTF.Schema2
{
    public static partial class Toolkit
    {
        /// <summary>
        /// Creates a new <see cref="Accessor"/> and fills it with data from <paramref name="memAccessor"/>
        /// </summary>
        /// <remarks>
        /// If enough number of zero values is detected, a sparse accessor will be created instead.
        /// </remarks>
        /// <param name="root">the model where the <see cref="Accessor"/> will be created.</param>
        /// <param name="memAccessor">the data to be used to fill the <see cref="Accessor"/>.</param>
        /// <param name="sparsityPercent">The percentage threshold of zero values that determine whether to use sparse accessors. Or -1 to disable sparse accessors creation</param>
        /// <returns>The created <see cref="Accessor"/>.</returns>
        public static Accessor CreateMorphTargetAccessor(this ModelRoot root, Memory.MemoryAccessor memAccessor, int sparsityPercent = 60)
        {
            Guard.NotNull(root, nameof(root));
            Guard.NotNull(memAccessor, nameof(memAccessor));

            var accessor = root.CreateAccessor(memAccessor.Attribute.Name);            

            var (indices, values) = memAccessor.ConvertToSparse();

            if (indices.Attribute.ItemsCount == 0)
            {
                accessor.SetZeros(memAccessor.Attribute);
                return accessor;
            }

            var sparsity = indices.Attribute.ItemsCount * 100 / memAccessor.Attribute.ItemsCount;

            if (sparsity > sparsityPercent)
            {
                accessor.SetVertexData(memAccessor);
                return accessor;
            }

            // set base data (all zeros)
            accessor.SetZeros(memAccessor.Attribute);

            // set sparse data on top of base data
            accessor.SetSparseData(indices, values);            

            return accessor;            
        }

        /// <summary>
        /// Creates a new <see cref="Accessor"/> and fills it with data from <paramref name="memAccessor"/>
        /// </summary>
        /// <param name="root">the model where the <see cref="Accessor"/> will be created.</param>
        /// <param name="memAccessor">the data to be used to fill the <see cref="Accessor"/>.</param>
        /// <returns>The created <see cref="Accessor"/>.</returns>
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
            Guard.NotNull(root, nameof(root));
            Guard.NotNull(data, nameof(data));

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
