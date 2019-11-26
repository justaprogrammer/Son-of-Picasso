using Autofac;

namespace SonOfPicasso.UI.WPF
{
    public class AutofacDependencyResolver : Splat.Autofac.AutofacDependencyResolver
    {
        private readonly IComponentContext _componentContext;

        public AutofacDependencyResolver(IComponentContext componentContext) : base(componentContext)
        {
            _componentContext = componentContext;
        }

        public void UpdateComponentContext(ContainerBuilder builder)
        {
#pragma warning disable 618
            builder.Update(_componentContext.ComponentRegistry);
#pragma warning restore 618
        }
    }
}