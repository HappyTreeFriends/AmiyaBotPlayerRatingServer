using AmiyaBotPlayerRatingServer.GameLogic.SchulteGrid;
using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;

namespace AmiyaBotPlayerRatingServer.Data
{
    public partial class ArknightsMemoryCache
    {
        // abs path is /app/Resources
        private readonly string _directoryPath = "Resources/amiya-bot-assets/repo";
        private readonly string _gitRepoUrl = "https://gitee.com/amiya-bot/amiya-bot-assets.git";
        private readonly string _zipFilePath = "Resources/amiya-bot-assets/repo/gamedata.zip";
        private readonly string _extractPath = "Resources/amiya-bot-assets/repo/gamedata";


        private void InitializeAssets()
        {
            //输出目录绝对路径
            var dir = new DirectoryInfo(_directoryPath);
            _logger.LogInformation($"Directory Path: {dir.FullName}");


            if (!Directory.Exists(_directoryPath))
            {
                Directory.CreateDirectory(_directoryPath);
                CloneRepo();
                ExtractGameData();
            }
            else
            {
                if (!IsGitRepo())
                {
                    CleanDirectory(_directoryPath);
                    CloneRepo();
                }
                else
                {
                    PullRepo(null);
                }
                ExtractGameData();
            }

            _logger.LogInformation("InitializeAssets Completed");
        }

        private void CloneRepo()
        {
            ExecuteShellCommand($"git clone {_gitRepoUrl} {_directoryPath}");
        }

        private void PullRepo(String? commit)
        {
            ExecuteShellCommand($"rm -f {_directoryPath}/.git/index.lock");
            if (String.IsNullOrWhiteSpace(commit))
            {
                ExecuteShellCommand($"cd {_directoryPath} && git fetch origin && git reset --hard origin/$(git rev-parse --abbrev-ref HEAD)");
            }
            else
            {
                ExecuteShellCommand($"cd {_directoryPath} && git fetch origin && git reset --hard origin/{commit}");
            }
            
        }

        private bool IsGitRepo()
        {
            return Directory.Exists(Path.Combine(_directoryPath, ".git"));
        }

        private void CleanDirectory(string directoryPath)
        {
            ExecuteShellCommand($"rm -rf {_directoryPath}/*");
        }

        private void ExtractGameData()
        {
            if (Directory.Exists(_extractPath))
            {
                Directory.Delete(_extractPath, true);
            }
            ZipFile.ExtractToDirectory(_zipFilePath, _extractPath);
        }

        private void ExecuteShellCommand(string command)
        {
            string exec;
            string param;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                exec = "bash";
                param = $"-c \"{command}\"";
            }
            else
            {
                exec = "cmd";
                param = $"/c {command}";
                
            }
            var processInfo = new ProcessStartInfo(exec, param)
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var process = new Process { StartInfo = processInfo };
            process.Start();
            process.WaitForExit();
        }

        public void UpdateAssets(String commit)
        {
            PullRepo(commit);
            ExtractGameData();
        }

        private void LoadJsonFromDir(String dirPath)
        {
            Directory.GetFiles(dirPath).ToList().ForEach(file =>
            {
                var fileName = Path.GetFileName(file);
                if (fileName.EndsWith(".json"))
                {
                    LoadFile(file, fileName);
                }
            });
        }

    }
}
