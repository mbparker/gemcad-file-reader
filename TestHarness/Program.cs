using Autofac;
using LibGemcadFileReader.Abstract;
using Newtonsoft.Json;
using TestHarness;

Console.WriteLine("Type full path to a .GEM file and press Enter.");
var inputFilename = Console.ReadLine();
Console.WriteLine("Type full path to an output .JSON file and press Enter.");
var outputFilename = Console.ReadLine();

if (!string.IsNullOrWhiteSpace(inputFilename) && !string.IsNullOrWhiteSpace(outputFilename))
{
    try
    {
        using (var container = ContainerRegistration.RegisterDependencies())
        {
            var logger = container.Resolve<ILoggerService>();
            var binImporter = container.Resolve<IGemCadGemImport>();
            var polygonContainer = binImporter.Import(inputFilename);
            File.WriteAllText(outputFilename,
                JsonConvert.SerializeObject(polygonContainer.Triangles, Formatting.Indented));
            logger.Debug("File parsed, and JSON dump of polygons created.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.ToString());
    }
}