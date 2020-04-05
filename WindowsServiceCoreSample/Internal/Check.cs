using System;
using System.Linq;
using System.Collections.Generic;

namespace WindowsServiceCoreSample.Internal
{
    internal static class Check
    {
        public static void NotNull<T>(T value, string parameterName)
        {
            if (ReferenceEquals(value, null))
            {
                NotEmpty(parameterName, "parameterName");
                throw new ArgumentNullException(parameterName);
            }
        }

        public static void NotEmpty<T>(IReadOnlyCollection<T> value, string parameterName)
        {
            NotNull(value, parameterName);

            if (value.Count == 0)
            {
                NotEmpty(parameterName, "parameterName");
                throw new ArgumentException($"The collection argument '{parameterName}' must contain at least one element.", parameterName);
            }
        }

        public static void NotEmpty(string value, string parameterName)
        {
            Exception ex = null;
            if (ReferenceEquals(value, null))
            {
                ex = new ArgumentNullException(parameterName);
            }
            else if (value.Trim().Length == 0)
            {
                ex = new ArgumentException($"The string argument '{parameterName}' cannot be empty.", parameterName);
            }

            if (ex != null)
            {
                NotEmpty(parameterName, "parameterName");
                throw ex;
            }
        }

        public static void NullButNotEmpty(string value, string parameterName)
        {
            if (!ReferenceEquals(value, null) && value.Trim().Length == 0)
            {
                NotEmpty(parameterName, "parameterName");
                throw new ArgumentException($"The string argument '{parameterName}' cannot be empty.", parameterName);
            }
        }

        public static void HasNoNulls<T>(IReadOnlyList<T> value, string parameterName)
            where T : class
        {
            NotNull(value, parameterName);

            if (value.Any(e => e == null))
            {
                NotEmpty(parameterName, "parameterName");
                throw new ArgumentException(parameterName);
            }
        }
    }
}