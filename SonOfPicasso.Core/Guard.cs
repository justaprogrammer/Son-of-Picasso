using System;
using System.Diagnostics;
using JetBrains.Annotations;

namespace SonOfPicasso.Core
{
    public static class Guard
    {
        //https://stackoverflow.com/a/32139346

        [DebuggerStepThrough]
        [ContractAnnotation("halt <= argument:null")]
        public static void ArgumentNotNull(object argument, [InvokerParameterName] string argumentName)
        {
            if (argument == null)
            {
                throw new ArgumentNullException(argumentName);
            }
        }

        [DebuggerStepThrough]
        [ContractAnnotation("halt <= argument:null")]
        public static void ArgumentNotNullOrEmpty(string argument, [InvokerParameterName] string argumentName)
        {
            NotNull(argument, argumentName);

            if (string.IsNullOrEmpty(argument))
            {
                throw new ArgumentException(argumentName);
            }
        }

        [DebuggerStepThrough]
        [ContractAnnotation("halt <= argument:null")]
        public static void NotNull(object argument, string argumentName)
        {
            if (argument == null)
            {
                throw new ArgumentNullException(argumentName);
            }
        }

        [DebuggerStepThrough]
        [ContractAnnotation("halt <= argument:null")]
        public static void NotNullOrEmpty(string argument, string argumentName)
        {
            NotNull(argument, argumentName);

            if (string.IsNullOrEmpty(argument))
            {
                throw new ArgumentException(argumentName);
            }
        }
    }
}