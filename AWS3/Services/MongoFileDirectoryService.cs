using AWS3.Models;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AWS3.Services
{
    public class MongoFileDirectoryService : IFileDirectoryService
    {
        private readonly IMongoCollection<FileDirectoryEntry> _fileEntries;

        public MongoFileDirectoryService(IConfiguration configuration)
        {
            var mongoClient = new MongoClient(
                configuration.GetConnectionString("MongoDbConnection"));

            var mongoDatabase = mongoClient.GetDatabase(
                configuration["MongoDB:DatabaseName"]);

            _fileEntries = mongoDatabase.GetCollection<FileDirectoryEntry>(
                configuration["MongoDB:FileEntriesCollection"]);
        }

        public async Task<FileDirectoryEntry> CreateFileEntryAsync(FileDirectoryEntry fileEntry)
        {
            await _fileEntries.InsertOneAsync(fileEntry);
            return fileEntry;
        }

        public async Task<FileDirectoryEntry> GetFileEntryByIdAsync(string id)
        {
            return await _fileEntries.Find(entry => entry.Id == id).FirstOrDefaultAsync();
        }

        public async Task<FileDirectoryEntry> GetFileEntryByObjectKeyAsync(string objectKey)
        {
            return await _fileEntries.Find(entry => entry.ObjectKey == objectKey).FirstOrDefaultAsync();
        }

        public async Task<List<FileDirectoryEntry>> GetAllFileEntriesAsync()
        {
            return await _fileEntries.Find(_ => true).ToListAsync();
        }

        public async Task<bool> DeleteFileEntryAsync(string id)
        {
            var result = await _fileEntries.DeleteOneAsync(entry => entry.Id == id);
            return result.DeletedCount > 0;
        }
    }
}
