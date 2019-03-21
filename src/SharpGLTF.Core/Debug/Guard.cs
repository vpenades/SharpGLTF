using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

            isDir |= filePath.EndsWith(System.IO.Path.DirectorySeparatorChar.ToString());
            isDir |= filePath.EndsWith(System.IO.Path.AltDirectorySeparatorChar.ToString());

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

        #endregion

        #region null / empty

        public static void NotNull(object target, string parameterName, string message = "")
        {
            if (target != null) return;
            if (string.IsNullOrWhiteSpace(message)) throw new ArgumentNullException(parameterName);
            throw new ArgumentNullException(parameterName, message);
        }

        public static void MustBeNull(object target, string parameterName, string message = "")
        {
            if (target == null) return;
            if (string.IsNullOrWhiteSpace(message)) throw new ArgumentNullException(parameterName);
            throw new ArgumentNullException(parameterName, message);
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
            if (value.CompareTo(minInclusive) >= 0 || value.CompareTo(maxInclusive) <= 0) return;

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

        public static void MustShareLogicalParent(Schema2.LogicalChildOfRoot a, Schema2.LogicalChildOfRoot b, string parameterName)
        {
            MustShareLogicalParent(a?.LogicalParent, b, parameterName);
        }

        public static void MustShareLogicalParent(Schema2.ModelRoot a, Schema2.LogicalChildOfRoot b, string parameterName)
        {
            if (a is null) throw new ArgumentNullException("this");
            if (b is null) throw new ArgumentNullException(parameterName);

            if (a != b.LogicalParent) throw new ArgumentException("LogicalParent mismatch", parameterName);
        }

        #endregion
    }
}
