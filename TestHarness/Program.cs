using Autofac;
using LibGemcadFileReader.Abstract;
using Newtonsoft.Json;
using TestHarness;

// This is not meant to be a useful program. It's simply demonstrating usage of the core library.

Console.WriteLine("Type full path to a .GEM or .ASC file and press Enter.");
var inputFilename = Console.ReadLine();
Console.WriteLine("Type full path to an output .JSON file and press Enter.");
var outputFilename = Console.ReadLine();

if (!string.IsNullOrWhiteSpace(inputFilename) && 
    File.Exists(inputFilename) && 
    !string.IsNullOrWhiteSpace(outputFilename) && 
    Directory.Exists(Path.GetDirectoryName(outputFilename)))
{
    try
    {
        using (var container = ContainerRegistration.RegisterDependencies())
        {
            var logger = container.Resolve<ILoggerService>();
            var importer = container.Resolve<IGemCadFileImport>();

            var data = importer.Import(inputFilename);
            
            File.WriteAllText(outputFilename,
                JsonConvert.SerializeObject(data, Formatting.Indented));
            logger.Debug("File parsed, and JSON dump of data created.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.ToString());
    }
}
else
{
    Console.WriteLine("Missing or invalid input filename and/or missing or invalid output file parent directory.");
}