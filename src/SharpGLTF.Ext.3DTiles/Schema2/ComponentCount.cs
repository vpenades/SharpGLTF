using SharpGLTF.Schema2.Tiles3D;

namespace SharpGLTF.Schema2
{
    public static class ComponentCount
    {
        public static int ByteSizeForComponentType(DataType? dataType)
        {
            switch (dataType)
            {
                case DataType.INT8:
                    return 1;
                case DataType.UINT8:
                    return 1;
                case DataType.INT16:
                    return 2;
                case DataType.UINT16:
                    return 2;
                case DataType.INT32:
                    return 4;
                case DataType.UINT32:
                    return 4;
                case DataType.INT64:
                    return 8;
                case DataType.UINT64:
                    return 8;
                case DataType.FLOAT32:
                    return 4;
                case DataType.FLOAT64:
                    return 8;
                default: return 0;
            }
        }

        public static int ElementCountForType(ElementType t)
        {
            switch (t)
            {
                case ElementType.SCALAR:
                case ElementType.STRING:
                case ElementType.ENUM:
                case ElementType.BOOLEAN:
                    return 1;
                case ElementType.VEC2:
                    return 2;
                case ElementType.VEC3:
                    return 3;
                case ElementType.VEC4:
                    return 4;
                case ElementType.MAT2:
                    return 4;
                case ElementType.MAT3:
                    return 9;
                case ElementType.MAT4:
                    return 16;
                default: return 0;
            }
        }

    }
}
