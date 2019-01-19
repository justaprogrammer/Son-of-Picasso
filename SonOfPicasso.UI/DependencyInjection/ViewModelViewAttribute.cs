using System;

namespace SonOfPicasso.UI.DependencyInjection
{
    public class ViewModelViewAttribute : Attribute
    {
        public ViewModelViewAttribute(Type type)
        {
            Type = type;
        }

        public Type Type { get; }
    }
}