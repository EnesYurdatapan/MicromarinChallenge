using Autofac;
using Autofac.Extras.DynamicProxy;
using Business.Abstract;
using Business.Concrete;

public class AutofacModule : Module
{
    protected override void Load(ContainerBuilder builder)
    {
   
        builder.RegisterType<ObjectDataService>()
            .As<IObjectDataService>()
            .EnableInterfaceInterceptors() 
            .InterceptedBy(typeof(TransactionAspect)); 


        
        builder.RegisterType<TransactionAspect>();
    }
}
