using System;
using System.Collections;
using System.Collections.Generic;

namespace SonOfPicasso.Core.Model
{
    public abstract class ImageContainer
    {
        public abstract string Id { get; }
        public abstract string Name { get; }
        public abstract int Year { get; }
        public abstract DateTime Date { get; }
        public abstract ImageContainerTypeEnum ContainerType { get; }
        public abstract IList<ImageRef> ImageRefs { get; }
        public abstract int ContainerTypeId { get; }
    }
}