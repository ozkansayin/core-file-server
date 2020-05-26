using System.Collections.Generic;
using System.IO;

namespace CoreServer.Services
{
    public interface IFileService
    {
        IEnumerable<string> GetList(bool relative);
        Stream GetFileStream(string filePath);
    }
}