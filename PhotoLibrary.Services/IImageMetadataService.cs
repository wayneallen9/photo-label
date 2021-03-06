﻿using PhotoLabel.Services.Models;
namespace PhotoLabel.Services
{
    public interface IImageMetadataService
    {
        bool Delete(string filename);
        bool Exists(string filename);
        Metadata Load(string filename);
        Metadata Populate(Metadata metadata);
        void Rename(string oldFilename, string newFilename);
        void Save(Metadata metadata, string filename);
    }
}