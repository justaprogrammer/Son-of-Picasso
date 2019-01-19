using System;

namespace SonOfPicasso.UI.Injection
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