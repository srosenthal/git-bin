using System;
using System.Collections.Generic;
using System.IO;

namespace GitBin.Remotes
{
    public interface IRemote
    {
        GitBinFileInfo[] ListFiles();

        void UploadFile(string sourceFilePath, string destinationFileName, Action<int> progressListener);
        byte[] DownloadFile(string fileName, Action<int> progressListener);
    }
}