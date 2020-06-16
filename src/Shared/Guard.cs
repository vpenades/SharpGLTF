using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace SharpGLTF
{
    [DebuggerStepThrough]
    internal static class Guard
    {
        #region strings & paths

        public static void NotNullOrEmpty(string target, string parameterName, string message = "")
        {
            NotNull(target, parameterName, message);

            if (!string.IsNullOrWhiteSpace(target)) return;

            if (string.IsNullOrWhiteSpace(message)) message = $"{parameterName} cannot be null or empty and cannot contain only blanks.";
            throw new ArgumentException(message, parameterName);
        }

        public static void FilePathMustBeValid(string filePath, string parameterName, string message = "")
        {
            // based on https://referencesource.microsoft.com/#mscorlib/system/io/file.cs,3360368484a9f131
            // 1.- Checks non null or empty
            // 2.- Relies on GetFullPath() for path checking
            // 3.- checks if it is a directory

            Guard.NotNullOrEmpty(filePath, parameterName, message);

            filePath = System.IO.Path.GetFullPath(filePath);

            bool isDir = false;

            isDir |= filePath.EndsWith(new String(System.IO.Path.DirectorySeparatorChar, 1), StringComparison.Ordinal);
            isDir |= filePath.EndsWith(new String(System.IO.Path.AltDirectorySeparatorChar, 1), StringComparison.Ordinal);

            if (!isDir) return;

            if (string.IsNullOrWhiteSpace(message)) message = $"{filePath} is invalid or does not exist.";
            throw new ArgumentException(message, parameterName);
        }

        public static void FilePathMustExist(string filePath, string parameterName, string message = "")
        {
            if (System.IO.File.Exists(filePath)) return;

            if (string.IsNullOrWhiteSpace(message)) message = $"{filePath} is invalid or does not exist.";
            throw new ArgumentException(message, parameterName);
        }

        public static void DirectoryPathMustExist(string dirPath, string parameterName, string message = "")
        {
            if (System.IO.Directory.Exists(dirPath)) return;

            if (string.IsNullOrWhiteSpace(message)) message = $"{dirPath} is invalid or does not exist.";
            throw new ArgumentException(message, parameterName);
        }

        #endregion

        #region null / empty

        // Preventing CA1062...
        // https://github.com/dotnet/roslyn-analyzers/issues/2691

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NotNull(object target, string parameterName)
        {
            if (target == null) throw new ArgumentNullException(parameterName);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NotNull(object target, string parameterName, string message)
        {
            if (target == null) throw new ArgumentNullException(parameterName, message);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MustBeNull(object target, string parameterName, string message = "")
        {
            if (target == null) return;
            throw new ArgumentException(parameterName, message);
        }

        #endregion

        #region collections

        public static void NotNullOrEmpty<T>(IEnumerable<T> target, string parameterName, string message = "")
        {
            NotNull(target, parameterName, message);

            if (target.Any()) return;

            if (string.IsNullOrWhiteSpace(message)) message = $"{parameterName} cannot be empty.";
            throw new ArgumentException(message, parameterName);
        }

        #endregion

        #region comparison

        public static void MustBeEqualTo<TValue>(TValue value, TValue expected, string parameterName)
                    where TValue : IComparable<TValue>
        {
            if (value.CompareTo(expected) == 0) return;

            throw new ArgumentException(parameterName, $"{parameterName} {value} must be equal to {expected}.");
        }

        public static void MustBePositiveAndMultipleOf(int value, int padding, string parameterName, string message = "")
        {
            if (value < 0)
            {
                if (string.IsNullOrWhiteSpace(message)) message = $"{parameterName} must not be negative";
                throw new ArgumentOutOfRangeException(message, parameterName);
            }

            if ((value % padding) != 0)
            {
                if (string.IsNullOrWhiteSpace(message)) message = $"{parameterName} is {value}; expected to be multiple of {padding}";
                throw new ArgumentOutOfRangeException(message, parameterName);
            }
        }

        public static void MustBeLessThan<TValue>(TValue value, TValue max, string parameterName)
                    where TValue : IComparable<TValue>
        {
            if (value.CompareTo(max) < 0) return;

            throw new ArgumentOutOfRangeException(parameterName, $"{parameterName} {value} must be less than {max}.");
        }

        public static void MustBeLessThanOrEqualTo<TValue>(TValue value, TValue max, string parameterName)
                    where TValue : IComparable<TValue>
        {
            if (value.CompareTo(max) > 0)
            {
                throw new ArgumentOutOfRangeException(parameterName, $"{parameterName} {value} must be less than or equal to {max}.");
            }
        }

        public static void MustBeGreaterThan<TValue>(TValue value, TValue min, string parameterName)
            where TValue : IComparable<TValue>
        {
            if (value.CompareTo(min) > 0) return;

            throw new ArgumentOutOfRangeException(parameterName, $"Value {value} must be greater than {min}.");
        }

        public static void MustBeGreaterThanOrEqualTo<TValue>(TValue value, TValue min, string parameterName)
            where TValue : IComparable<TValue>
        {
            if (value.CompareTo(min) >= 0) return;

            throw new ArgumentOutOfRangeException(parameterName, $"{parameterName} {value} must be greater than or equal to {min}.");
        }

        public static void MustBeBetweenOrEqualTo<TValue>(TValue value, TValue minInclusive, TValue maxInclusive, string parameterName)
            where TValue : IComparable<TValue>
        {
            if (value.CompareTo(minInclusive) >= 0 && value.CompareTo(maxInclusive) <= 0) return;

            throw new ArgumentOutOfRangeException(parameterName, $"{parameterName} {value} must be greater than or equal to {minInclusive} and less than or equal to {maxInclusive}.");
        }

        #endregion

        #region true false

        public static void IsTrue(bool target, string parameterName, string message = "")
        {
            if (target) return;

            throw new ArgumentException(message, parameterName);
        }

        public static void IsFalse(bool target, string parameterName, string message = "")
        {
            if (!target) return;

            throw new ArgumentException(message, parameterName);
        }

        #endregion

        #region specialised

        private static readonly IReadOnlyList<Char> _InvalidRelativePathChars =
            System.IO.Path.GetInvalidFileNameChars()
            .Where(c => c != '/' && c != '\\')
            .ToArray();

        public static void IsValidURI(string parameterName, string gltfURI, params string[] validHeaders)
        {
            if (string.IsNullOrEmpty(gltfURI)) return;

            foreach (var hdr in validHeaders)
            {
                if (gltfURI.StartsWith(hdr, StringComparison.OrdinalIgnoreCase))
                {
                    string value = hdr + ",";
                    if (gltfURI.StartsWith(value, StringComparison.OrdinalIgnoreCase)) return;
                    if (gltfURI.StartsWith(hdr + ";base64,", StringComparison.OrdinalIgnoreCase)) return;

                    throw new ArgumentException($"{parameterName} has invalid URI '{gltfURI}'.");
                }
            }

            if (gltfURI.Any(c => _InvalidRelativePathChars.Contains(c))) throw new ArgumentException($"Invalid URI '{gltfURI}'.");

            if (gltfURI.Any(chr => char.IsWhiteSpace(chr))) gltfURI = Uri.EscapeUriString(gltfURI);

            if (!Uri.TryCreate(gltfURI, UriKind.Relative, out Uri xuri)) throw new ArgumentException($"Invalid URI '{gltfURI}'.");

            return;
        }

        public static void MustShareLogicalParent(Schema2.LogicalChildOfRoot a, Schema2.LogicalChildOfRoot b, string parameterName)
        {
            MustShareLogicalParent(a?.LogicalParent, nameof(a.LogicalParent), b, parameterName);
        }

        public static void MustShareLogicalParent(Schema2.ModelRoot a, string aName, Schema2.LogicalChildOfRoot b, string bName)
        {
            if (a is null) throw new ArgumentNullException(aName);
            if (b is null) throw new ArgumentNullException(bName);

            if (a != b.LogicalParent) throw new ArgumentException("LogicalParent mismatch", bName);
        }

        #endregion
    }

    [DebuggerStepThrough]
    internal static class GuardAll
    {
        public static void NotNull<T>(IEnumerable<T> collection, string parameterName, string message = "")
        {
            Guard.NotNull(collection, nameof(collection));
            foreach (var val in collection) Guard.NotNull(val, parameterName, message);
        }

        public static void MustBeEqualTo<TValue>(IEnumerable<TValue> collection, TValue expected, string parameterName)
            where TValue : IComparable<TValue>
        {
            Guard.NotNull(collection, nameof(collection));
            foreach (var val in collection) Guard.MustBeEqualTo(val, expected, parameterName);
        }

        public static void MustBeGreaterThan<TValue>(IEnumerable<TValue> collection, TValue minExclusive, string parameterName)
            where TValue : IComparable<TValue>
        {
            Guard.NotNull(collection, nameof(collection));
            foreach (var val in collection) Guard.MustBeGreaterThan(val, minExclusive, parameterName);
        }

        public static void MustBeLessThan<TValue>(IEnumerable<TValue> collection, TValue maxExclusive, string parameterName)
            where TValue : IComparable<TValue>
        {
            Guard.NotNull(collection, nameof(collection));
            foreach (var val in collection) Guard.MustBeLessThan(val, maxExclusive, parameterName);
        }

        public static void MustBeLessThanOrEqualTo<TValue>(IEnumerable<TValue> collection, TValue maxInclusive, string parameterName)
            where TValue : IComparable<TValue>
        {
            Guard.NotNull(collection, nameof(collection));
            foreach (var val in collection) Guard.MustBeLessThanOrEqualTo(val, maxInclusive, parameterName);
        }

        public static void MustBeGreaterThanOrEqualTo<TValue>(IEnumerable<TValue> collection, TValue minInclusive, string parameterName)
            where TValue : IComparable<TValue>
        {
            Guard.NotNull(collection, nameof(collection));
            foreach (var val in collection) Guard.MustBeGreaterThanOrEqualTo(val, minInclusive, parameterName);
        }

        public static void MustBeBetweenOrEqualTo<TValue>(IEnumerable<TValue> collection, TValue minInclusive, TValue maxInclusive, string parameterName)
            where TValue : IComparable<TValue>
        {
            Guard.NotNull(collection, nameof(collection));
            foreach (var val in collection) Guard.MustBeBetweenOrEqualTo(val, minInclusive, maxInclusive, parameterName);
        }
    }
}
