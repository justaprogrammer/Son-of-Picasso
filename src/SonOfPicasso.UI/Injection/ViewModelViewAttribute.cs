using System;
using System.Diagnostics.CodeAnalysis;

namespace SonOfPicasso.UI.Injection
{
    [ExcludeFromCodeCoverage]
    public class ViewModelViewAttribute : Attribute
    {
        public ViewModelViewAttribute(Type type)
        {
            Type = type;
        }

        public Type Type { get; }
    }
}