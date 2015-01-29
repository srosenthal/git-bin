using System;
using System.IO;

namespace GitBin.Remotes
{
    public interface IRemote
    {
        GitBinFileInfo[] ListFiles();

        void UploadFile(string sourceFilePath, string destinationFileName);
        byte[] DownloadFile(string fileName);

        event Action<int> ProgressChanged;
    }
}