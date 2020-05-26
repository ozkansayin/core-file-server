using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CoreServer.Services
{
    public class FileService : IFileService
    {
        private readonly ILogger<FileService> _logger;
        private readonly CoreServerSettings _settings;

        private string _basePath;
        private string _servePath;

        public FileService(ILogger<FileService> logger, IOptions<CoreServerSettings> options)
        {
            _logger = logger;
            _settings = options.Value;
        }

        public string BasePath => !string.IsNullOrEmpty(_basePath) ? _basePath : (_basePath = Directory.GetCurrentDirectory());
        private string ServePath => !string.IsNullOrEmpty(_servePath) ? _servePath : (_servePath = GetServePath());

        public IEnumerable<string> GetList(bool relative) => GetFilesList(relative);

        public Stream GetFileStream(string filePath)
        {
            if (!Regex.IsMatch(filePath, _settings.AllowedFilesRegex))
            {
                return null;
            }

            var fullPath = Path.GetRelativePath(ServePath, filePath);
            if (!fullPath.StartsWith(ServePath))
            {
                _logger.LogWarning($"File not served for safety. Relative path: {filePath}");
                return null;
            }

            if (!File.Exists(fullPath))
            {
                _logger.LogWarning($"Requested file could not be found: {fullPath}");
                return null;
            }

            return File.OpenRead(fullPath);
        }

        private IEnumerable<string> GetFilesList(bool relative)
        {
            var allFiles = Directory.GetFiles(ServePath, "*.*", SearchOption.AllDirectories).ToList();
            if (_settings.IgnoreApplicationPath && BasePath.StartsWith(ServePath))
            {
                allFiles = allFiles.Where(x => !x.StartsWith(BasePath)).ToList();
            }

            if (relative)
            {
                allFiles = allFiles.Select(x => x.Substring(ServePath.Length)).ToList();
            }

            return allFiles;
        }

        private string GetServePath()
        {
            var servePath = _settings.ServePath;
            if (servePath.StartsWith("\\"))
            {
                servePath = servePath.Substring(1);
            }

            var combinePath = Path.Combine(BasePath, servePath);
            return Path.GetFullPath(combinePath);
        }
    }
}
