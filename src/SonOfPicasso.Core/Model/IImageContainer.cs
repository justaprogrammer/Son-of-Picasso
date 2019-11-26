using System;
using System.Collections;
using System.Collections.Generic;

namespace SonOfPicasso.Core.Model
{
    public interface IImageContainer
    {
        string Key { get; }
        string Name { get; }
        int Year { get; }
        DateTime Date { get; }
        ImageContainerTypeEnum ContainerType { get; }
        IList<ImageRef> ImageRefs { get; }
        int Id { get; }
    }
}