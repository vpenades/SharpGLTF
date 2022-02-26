using System;
using System.Collections.Generic;
using System.Text;

namespace SharpGLTF
{
    static class DownloadUtils
    {
        private static readonly Object _DownloadMutex = new object();

        public static void SyncronizeGitRepository(string remoteUrl, string localDirectoryPath)
        {
            Console.WriteLine($"Sync with {remoteUrl}...");

            if (!System.IO.Path.IsPathRooted(localDirectoryPath)) throw new ArgumentException(nameof(localDirectoryPath));

            lock (_DownloadMutex)
            {
                if (LibGit2Sharp.Repository.Discover(localDirectoryPath) == null)
                {
                    Console.WriteLine($"Cloning {remoteUrl} can take several minutes; Please wait...");

                    LibGit2Sharp.Repository.Clone(remoteUrl, localDirectoryPath);

                    Console.WriteLine($"... Clone Completed");

                    return;
                }

                using (var repo = new LibGit2Sharp.Repository(localDirectoryPath))
                {
                    var options = new LibGit2Sharp.PullOptions
                    {
                        FetchOptions = new LibGit2Sharp.FetchOptions()
                    };

                    var r = LibGit2Sharp.Commands.Pull(repo, new LibGit2Sharp.Signature("Anonymous", "anon@anon.com", new DateTimeOffset(DateTime.Now)), options);

                    Console.WriteLine($"{remoteUrl} is {r.Status}");
                }
            }
        }

        public static string DownloadFile(string remoteUri, string localFilePath)
        {
            if (!System.IO.Path.IsPathRooted(localFilePath)) throw new ArgumentException(nameof(localFilePath));

            lock (_DownloadMutex)
            {
                if (System.IO.File.Exists(localFilePath)) return localFilePath; // we check again because we could have downloaded the file while waiting.

                Console.WriteLine($"Downloading {remoteUri}... Please Wait...");

                var dir = System.IO.Path.GetDirectoryName(localFilePath);
                System.IO.Directory.CreateDirectory(dir);

                using (var wc = new System.Net.WebClient())
                {
                    wc.DownloadFile(remoteUri, localFilePath);
                }

                if (localFilePath.ToLowerInvariant().EndsWith(".zip"))
                {
                    Console.WriteLine($"Extracting {localFilePath}...");

                    var extractPath = System.IO.Path.Combine(dir, System.IO.Path.GetFileNameWithoutExtension(localFilePath));

                    System.IO.Compression.ZipFile.ExtractToDirectory(localFilePath, extractPath);
                }

                return localFilePath;
            }
        }
    }
}
