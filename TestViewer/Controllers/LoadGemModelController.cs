using Microsoft.AspNetCore.Mvc;
using LibGemcadFileReader.Abstract;

namespace TestViewer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LoadGemModelController : ControllerBase
{
    private readonly ILogger<LoadGemModelController> logger;
    private readonly IFileOperations fileOperations;
    private readonly IGemCadFileImport importer;

    public LoadGemModelController(ILogger<LoadGemModelController> logger,
        IFileOperations fileOperations, IGemCadFileImport importer)
    {
        this.logger = logger;
        this.fileOperations = fileOperations;
        this.importer = importer;
    }

    [HttpPost]
    public IActionResult Post([FromForm] IFormFile file)
    {
        try
        {
            var filename = fileOperations.CreateTempFilename();
            try
            {
                using (var stream = fileOperations.CreateFileStream(filename, FileMode.Create))
                {
                    file.CopyTo(stream);
                }
                    
                var data = importer.Import(filename);
                return Ok(data);
            }
            finally
            {
                fileOperations.DeleteFile(filename);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to read file.");
            return new StatusCodeResult(500);
        }
    }
}