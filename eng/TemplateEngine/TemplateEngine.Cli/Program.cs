using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace TemplateEngine.Cli
{
    class Program
    {
        static void Main(string[] args)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    Run(args);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
            else
            {
                Console.WriteLine("Windows only.");
            }

            if (Debugger.IsAttached)
            {
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }

        private static void Run(string[] args)
        {
            const string argumentExampleMessage = "Type a string to define topic and block namespace e.g. Http\\Extensions.HttpClient";

            if (args.Length == 0)
            {
                Console.WriteLine(argumentExampleMessage);
                args = new [] { Console.ReadLine() };
                if (args[0].Length == 0)
                {
                    return;
                }
            }
            
            if (args.Length > 1)
            {
                Console.WriteLine("Too much arguments passed.");
                return;
            }

            string[] parts;
            bool continueLoop;
            do
            {
                continueLoop = false;
                parts = args[0].Split("\\");

                if (parts.Length != 2)
                {
                    Console.WriteLine(argumentExampleMessage);
                    args = new[] { Console.ReadLine() };
                    if (args[0].Length == 0)
                    {
                        return;
                    }
                    continueLoop = true;
                }
            }
            while (continueLoop);

            var topic = parts[0];
            var buildingBlockNamespace = parts[1];

            ApplyTemplate(topic, buildingBlockNamespace);
        }

        private static void ApplyTemplate(string topic, string buildingBlockNamespace)
        {
            if (topic is null)
            {
                throw new ArgumentNullException(nameof(topic));
            }

            if (string.IsNullOrWhiteSpace(topic))
            {
                throw new ArgumentException("Argument cannot be empty or consists only of white-space characters.", nameof(topic));
            }

            if (buildingBlockNamespace is null)
            {
                throw new ArgumentNullException(nameof(buildingBlockNamespace));
            }

            if (string.IsNullOrWhiteSpace(buildingBlockNamespace))
            {
                throw new ArgumentException("Argument cannot be empty or consists only of white-space characters.", nameof(buildingBlockNamespace));
            }

            var workingDirectory = Environment.CurrentDirectory;
            var response = RunGitCommand("git rev-parse --show-toplevel", workingDirectory);

            if (response.ExitCode == 0)
            {
                var repoPath = response.StdOut.Replace("/", "\\", StringComparison.Ordinal);
                var repoName = Path.GetFileName(repoPath);

                if (repoName == "StackReloaded")
                {
                    var context = new TopicTemplateContext(repoPath, topic, buildingBlockNamespace);

                    WriteTopicFolder(context);
                    WriteSolutionFiles(context);

                    Console.WriteLine("DONE!");
                }
            }
        }

        private static void WriteSolutionFiles(TopicTemplateContext context)
        {
            var allSlnFilePath = Path.Combine(context.AllPath, "StackReloaded.BuildingBlocks.sln");
            var slnFilePath = Path.Combine(context.TopicPath, $"{context.Topic}.sln");
            var pathToCsProj = $@"src\StackReloaded.{context.BuildingBlockNamespace}\StackReloaded.{context.BuildingBlockNamespace}.csproj";
            var pathToUnitTestsCsProj = $@"test\StackReloaded.{context.BuildingBlockNamespace}.UnitTests\StackReloaded.{context.BuildingBlockNamespace}.UnitTests.csproj";
            var csProjExists = File.Exists(Path.Combine(context.TopicPath, pathToCsProj));
            var unitTestsCsProjExists = File.Exists(Path.Combine(context.TopicPath, pathToUnitTestsCsProj));

            if (!File.Exists(allSlnFilePath))
            {
                WriteTemplateFile(manifestResourceStreamName: "Topic.Topic.sln.txt", filePath: allSlnFilePath);
            }

            if (!File.Exists(slnFilePath))
            {
                WriteTemplateFile(manifestResourceStreamName: "Topic.Topic.sln.txt", filePath: slnFilePath);
            }

            if (csProjExists)
            {
                RunDotNetCommand($@"dotnet sln add ""{pathToCsProj}""", context.TopicPath);
            }

            if (unitTestsCsProjExists)
            {
                RunDotNetCommand($@"dotnet sln add ""{pathToUnitTestsCsProj}""", context.TopicPath);
            }

            if (csProjExists)
            {
                RunDotNetCommand($@"dotnet sln StackReloaded.BuildingBlocks.sln add --solution-folder ""src\{context.Topic}\src"" ""..\{context.Topic}\{pathToCsProj}""", context.AllPath);
            }

            if (unitTestsCsProjExists)
            {
                RunDotNetCommand($@"dotnet sln StackReloaded.BuildingBlocks.sln add --solution-folder ""src\{context.Topic}\test"" ""..\{context.Topic}\{pathToUnitTestsCsProj}""", context.AllPath);
            }

            //if (csProjExists)
            //{
            //    RunDotNetCommand($@"dotnet sln StackReloaded.BuildingBlocks.Packages.sln add --solution-folder ""src\{context.Topic}\src"" ""..\{context.Topic}\{pathToCsProj}""", context.AllPath);
            //}
        }

        private static void WriteTopicFolder(TopicTemplateContext context)
        {
            if (!Directory.Exists(context.TopicPath))
            {
                Directory.CreateDirectory(context.TopicPath);
            }
            
            if (WriteSrcFolder(context))
            {
                WriteTopicReadMeFile(context);
            }
            WriteRepoReadMeFile(context);
            WriteTestFolder(context);
        }

        private static void WriteTopicReadMeFile(TopicTemplateContext context)
        {
            var readMeFilePath = Path.Combine(context.TopicPath, "README.md");
            var readMeInitContent = GetManifestResourceText("Topic.README.init.md.txt");
            readMeInitContent = readMeInitContent.Replace("##Topic##", context.Topic, StringComparison.Ordinal);

            var readMeAppendContent = GetManifestResourceText("Topic.README.append.md.txt");
            readMeAppendContent = readMeAppendContent.Replace("##BuildingBlock##", context.BuildingBlockNamespace, StringComparison.Ordinal);

            if (File.Exists(readMeFilePath))
            {
                using var writer = File.AppendText(readMeFilePath);
                writer.WriteLine(readMeAppendContent);
            }
            else
            {
                using var writer = File.CreateText(readMeFilePath);
                writer.WriteLine(readMeInitContent);
                writer.WriteLine(readMeAppendContent);
            }
        }

        private static void WriteRepoReadMeFile(TopicTemplateContext context)
        {
            var topic = context.Topic;
            var topicReadMeFilePath = Path.Combine(context.TopicPath, "README.md");

            if (!File.Exists(topicReadMeFilePath))
            {
                return;
            }

            var repoReadMeFilePath = Path.Combine(context.RepoPath, "README.md");

            if (!File.Exists(repoReadMeFilePath))
            {
                return;
            }

            var content = File.ReadAllText(repoReadMeFilePath);
            var topicTitle = "\r\n# Topics (in alfabetische volgorde)\r\n";
            var indexTopics = content.IndexOf(topicTitle, StringComparison.Ordinal);

            if (indexTopics < 0)
            {
                return;
            }

            var startIndex = indexTopics + topicTitle.Length;
            var contentEdited = false;

            while (true)
            {
                var endIndex = content.IndexOf("\r\n", startIndex, StringComparison.Ordinal);

                if (endIndex < 0)
                {
                    if (startIndex >= content.Length - 1)
                    {
                        break;
                    }

                    endIndex = content.Length - 1;
                }

                var length = endIndex - startIndex;
                var line = content.Substring(startIndex, length);

                if (!line.StartsWith('-'))
                {
                    content = content.Insert(startIndex - "\r\n".Length, $"\r\n- [{topic}](src/{topic}/README)");
                    contentEdited = true;
                    break;
                }

                var topicToCompare = line.TrimStart('-', ' ');

                if (topicToCompare.StartsWith('['))
                {
                    topicToCompare = topicToCompare.Substring(0, topicToCompare.IndexOf(']', StringComparison.Ordinal)).Trim('[');
                }

                var compareResult = string.Compare(topic, topicToCompare, StringComparison.InvariantCultureIgnoreCase);

                if (compareResult > 0)
                {
                    if (endIndex == content.Length - 1)
                    {
                        content = content.Insert(endIndex + 1, $"\r\n- [{topic}](src/{topic}/README)");
                        contentEdited = true;
                        break;
                    }

                    startIndex = endIndex + "\r\n".Length;
                }
                else if (compareResult == 0)
                {
                    break;
                }
                else
                {
                    content = content.Insert(startIndex, $"- [{topic}](src/{topic}/README)\r\n");
                    contentEdited = true;
                    break;
                }
            }

            if (contentEdited)
            {
                File.WriteAllText(repoReadMeFilePath, content, Encoding.Unicode);
            }
        }

        private static bool WriteSrcFolder(TopicTemplateContext context)
        {
            var directoryPath = Path.Combine(context.TopicPath, "src", $"StackReloaded.{context.BuildingBlockNamespace}");

            if (Directory.Exists(directoryPath) && !IsDirectoryEmpty(directoryPath))
            {
                Console.WriteLine($"Directory \"{directoryPath}\" already exists and is not empty.");
                return false;
            }

            Directory.CreateDirectory(directoryPath);

            WriteTemplateFile(
                manifestResourceStreamName: "Topic.src.StackReloaded.BuildingBlock.StackReloaded.BuildingBlock.csproj.txt",
                filePath: Path.Combine(directoryPath, $"StackReloaded.{context.BuildingBlockNamespace}.csproj"),
                transform: content => content.Replace("##BuildingBlock##", context.BuildingBlockNamespace, StringComparison.Ordinal));

            WriteTemplateFile(
                manifestResourceStreamName: "Topic.src.StackReloaded.BuildingBlock.StackReloaded.BuildingBlock.version.txt",
                filePath: Path.Combine(directoryPath, $"StackReloaded.{context.BuildingBlockNamespace}.version"));

            return true;
        }

        private static void WriteTestFolder(TopicTemplateContext context)
        {
            var pathToCsProj = $@"src\StackReloaded.{context.BuildingBlockNamespace}\StackReloaded.{context.BuildingBlockNamespace}.csproj";
            var csProjExists = File.Exists(Path.Combine(context.TopicPath, pathToCsProj));

            if (!csProjExists)
            {
                return;
            }

            var directoryPath = Path.Combine(context.TopicPath, "test", $"StackReloaded.{context.BuildingBlockNamespace}.UnitTests");

            if (Directory.Exists(directoryPath) && !IsDirectoryEmpty(directoryPath))
            {
                Console.WriteLine($"Directory \"{directoryPath}\" already exists and is not empty.");
                return;
            }

            Directory.CreateDirectory(directoryPath);

            WriteTemplateFile(
                manifestResourceStreamName: "Topic.test.StackReloaded.BuildingBlock.UnitTests.StackReloaded.BuildingBlock.UnitTests.csproj.txt",
                filePath: Path.Combine(directoryPath, $"StackReloaded.{context.BuildingBlockNamespace}.UnitTests.csproj"),
                transform: content => content.Replace("##BuildingBlock##", context.BuildingBlockNamespace, StringComparison.Ordinal));
        }

        private static void WriteTemplateFile(string manifestResourceStreamName, string filePath, Func<string, string> transform = null)
        {
            var content = GetManifestResourceText(manifestResourceStreamName);

            if (transform != null)
            {
                content = transform(content);
            }

            File.WriteAllText(filePath, content);
        }

        private static bool IsDirectoryEmpty(string path)
        {
            return !Directory.EnumerateFiles(path).Any();
        }

        private static string GetManifestResourceText(string manifestResourceStreamName)
        {
            using var inputStream = GetManifestResourceStream(manifestResourceStreamName);
            using var reader = new StreamReader(inputStream);
            return reader.ReadToEnd();
        }

        private static Stream GetManifestResourceStream(string manifestResourceStreamName)
        {
            var assembly = typeof(Program).Assembly;
            return assembly.GetManifestResourceStream($"{assembly.GetName().Name}.Templates.{manifestResourceStreamName}");
        }

        internal class TopicTemplateContext
        {
            public TopicTemplateContext(string repoPath, string topic, string buildingBlockNamespace)
            {
                RepoPath = repoPath;
                AllPath = Path.Combine(repoPath, "src", "All");
                Topic = topic;
                TopicPath = Path.Combine(repoPath, "src", topic);
                BuildingBlockNamespace = buildingBlockNamespace;
            }

            public string RepoPath { get; }
            public string AllPath { get; }
            public string Topic { get; }
            public string TopicPath { get; }
            public string BuildingBlockNamespace { get; }
        }

        internal static Response RunDotNetCommand(string cmd, string dir)
        {
            if (cmd is null)
            {
                throw  new ArgumentNullException(nameof(cmd));
            }

            if (!cmd.StartsWith("dotnet ", StringComparison.Ordinal))
            {
                throw new ArgumentException("Command doesn't starts with 'dotnet '.", nameof(cmd));
            }

            var arguments = cmd.Substring("dotnet ".Length);

            return RunCommand("dotnet.exe", cmd, arguments, dir);
        }

        internal static Response RunGitCommand(string cmd, string dir)
        {
            if (cmd is null)
            {
                throw  new ArgumentNullException(nameof(cmd));
            }

            if (!cmd.StartsWith("git ", StringComparison.Ordinal))
            {
                throw new ArgumentException("Command doesn't starts with 'git '.", nameof(cmd));
            }

            var arguments = cmd.Substring("git ".Length);

            return RunCommand("git.exe", cmd, arguments, dir);
        }

        internal static Response RunCommand(string fileName, string cmd, string arguments, string dir)
        {
            if (cmd is null)
            {
                throw  new ArgumentNullException(nameof(cmd));
            }

            if (dir is null)
            {
                throw new ArgumentNullException(nameof(dir));
            }

            var stderr = new StringBuilder();
            var stderrFirstLine = true;
            var stdout = new StringBuilder();
            var stdoutFirstLine = true;
            try
            {
                Console.WriteLine("Run command: " + cmd);

                var processStartInfo = new ProcessStartInfo
                {
                    WorkingDirectory = dir,
                    FileName = fileName,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                };

                using var process = Process.Start(processStartInfo);

                // ReSharper disable once PossibleNullReferenceException
                while (!process.StandardOutput.EndOfStream)
                {
                    var line = process.StandardOutput.ReadLine();
                    if (stdoutFirstLine)
                    {
                        stdoutFirstLine = false;
                    }
                    else
                    {
                        stdout.AppendLine();
                    }
                    stdout.Append(line);
                    Console.WriteLine(line);
                }

                while (!process.StandardError.EndOfStream)
                {
                    var line = process.StandardError.ReadLine();
                    if (stderrFirstLine)
                    {
                        stderrFirstLine = false;
                    }
                    else
                    {
                        stderr.AppendLine();
                    }
                    stderr.Append(line);
                    Console.WriteLine(line);
                }

                process.WaitForExit();

                return new Response
                {
                    ExitCode = process.ExitCode,
                    StdOut = stdout.ToString(),
                    StdErr = stderr.ToString()
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);

                return new Response
                {
                    ExitCode = -1,
                    Exception = ex
                };
            }
        }
        
        internal class Response
        {
            public int ExitCode { get; set; }
            public string StdOut { get; set; }
            public string StdErr { get; set; }
            public Exception Exception { get; set; }
        }
    }
}
