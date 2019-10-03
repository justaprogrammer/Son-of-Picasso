using System;

namespace SonOfPicasso.Core.Model
{
    public abstract class ImageContainer
    {
        public abstract string Id { get; }
        public abstract string Name { get; }
        public abstract DateTime Date { get; }
        public abstract ImageContainerTypeEnum ContainerType { get; }
    }
}