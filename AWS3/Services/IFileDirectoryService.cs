using AWS3.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AWS3.Services
{
    public interface IFileDirectoryService
    {
        Task<FileDirectoryEntry> CreateFileEntryAsync(FileDirectoryEntry fileEntry);
        Task<FileDirectoryEntry> GetFileEntryByIdAsync(string id);
        Task<FileDirectoryEntry> GetFileEntryByObjectKeyAsync(string objectKey);
        Task<List<FileDirectoryEntry>> GetAllFileEntriesAsync();
        Task<bool> DeleteFileEntryAsync(string id);
    }
}
