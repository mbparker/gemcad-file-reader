using Autofac;
using LibGemcadFileReader;
using LibGemcadFileReader.Abstract;

namespace TestHarness;

public static class ContainerRegistration
{
    public static IContainer RegisterDependencies()
    {
        var builder = new ContainerBuilder();
        builder.RegisterModule<ContainerModuleRegistration>();
        builder.RegisterType<ConsoleLogger>().As<ILoggerService>().SingleInstance();
        return builder.Build();
    }
}