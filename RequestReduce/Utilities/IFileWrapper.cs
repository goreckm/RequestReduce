﻿using System.Collections.Generic;
using System.IO;
using StructureMap;

namespace RequestReduce.Utilities
{
    public interface IFileWrapper
    {
        void Save(string content, string fileName);
        void Save(byte[] content, string fileName);
        Stream OpenStream(string fileName);
        bool DirectoryExists(string directoryName);
        void CreateDirectory(string directoryName);
        string[] GetDirectories(string dir);
        string[] GetFiles(string dir);
        void DeleteDirectory(string path);
        void DeleteFile(string path);
        void RenameFile(string originalName, string newName);
        bool FileExists(string path);
        byte[] GetFileBytes(string path);
		IList<DatedFileEntry> GetDatedFiles(string directoryPath, string search);
    }
}