using Autofac;
using LibGemcadFileReader.Abstract;
using LibGemcadFileReader.Concrete;

namespace LibGemcadFileReader
{
    public class ContainerModuleRegistration : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<FileOperations>().As<IFileOperations>().SingleInstance();
            builder.RegisterType<GeometryOperations>().As<IGeometryOperations>().SingleInstance();
            builder.RegisterType<GemCadAscImport>().As<IGemCadAscImport>().SingleInstance();
            builder.RegisterType<GemCadGemImport>().As<IGemCadGemImport>().SingleInstance();
            builder.RegisterType<VectorOperations>().As<IVectorOperations>().SingleInstance();
            builder.RegisterType<PolygonSubdivisionProvider>().As<IPolygonSubdivisionProvider>().SingleInstance();
        }
    }
}