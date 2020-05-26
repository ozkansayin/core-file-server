using System.Collections.Generic;
using System.Linq;
using CoreServer.Services;
using Microsoft.AspNetCore.Mvc;

namespace CoreServer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FilesController : ControllerBase
    {
        private readonly IFileService _fileService;

        public FilesController(IFileService fileService)
        {
            _fileService = fileService;
        }

        [HttpGet]
        [Route("list")]
        public IEnumerable<string> List()
        {
            var files = _fileService.GetList(true);

            var results = files.Select(GetFileDownloadLink).ToList();

            return results;
        }

        [HttpGet]
        [Route("download/{path}")]
        public IActionResult Download(string path)
        {
            var stream = _fileService.GetFileStream(path);

            if (stream == null)
            {
                return NotFound();
            }

            return File(stream, "application/octet-stream");
        }

        private string GetFileDownloadLink(string path)
        {
            return Url.Action("Download", new { path }).ToLowerInvariant();
        }
    }
}
