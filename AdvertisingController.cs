using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/advertising")]
public class AdvertisingController : ControllerBase
{
    private readonly AdvertisingStorageTrie _storage;

    public AdvertisingController(AdvertisingStorageTrie storage)
    {
        _storage = storage;
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { Error = "No file uploaded" });
        }
        if (!file.FileName.EndsWith(".txt"))
        {
            return BadRequest(new { Error = "Only .txt files are allowed" });
        }

        try
        {
            using var reader = new StreamReader(file.OpenReadStream());
            var content = await reader.ReadToEndAsync();
            _storage.AddFromContent(content);
            
            return Ok(new { 
                Message = "Data uploaded successfully",
                FileName = file.FileName,
                Size = file.Length
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    [HttpGet("search")]
    public IActionResult Search([FromQuery] string location)
    {
        if (string.IsNullOrEmpty(location))
        {
            return BadRequest(new { Error = "Location parameter is required" });
        }

        try
        {
            var platforms = _storage.GetCompaniesWithHierarchy(location);
            return Ok(new { 
                Location = location, 
                Platforms = platforms,
                Count = platforms.Count()
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = ex.Message });
        }
    }
}