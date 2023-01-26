using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Text;

namespace CueFileGen
{
    public class Program
    {
        public static string Usage() => $"{System.AppDomain.CurrentDomain.FriendlyName} <INPUT_FILE>\n---\n" +
            $"Generate a `.cue` file from the metadata of the INPUT_FILE\n" +
            $"ARGS:\n" +
            $"\tINPUT_FILE - the input file to read the metadata from, for generating the `.cue` file.";

        public static bool CheckFFProbe()
        {
            Process process = new Process();
            process.StartInfo.FileName = "ffprobe";
            process.StartInfo.Arguments = "-L"; // print license
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.CreateNoWindow = false;
            process.StartInfo.UseShellExecute = false;
            bool started = process.Start();

            if (!started)
            {
                return false;
            }

            process.WaitForExit();

            return process.ExitCode == 0;
        }

        static public IResult<string, string> ProbeFile(string filePath, out List<string> log)
        {
            string commandArgs = $"-v error -show_chapters -pretty \"{filePath}\"";
            Process process = new Process();
            process.StartInfo.FileName = "ffprobe";
            process.StartInfo.Arguments = commandArgs;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.CreateNoWindow = false;
            process.StartInfo.UseShellExecute = false;

            log = new List<string>();
            log.Add("Starting 'ffprobe'!");
            log.Add($"cmd: ffprobe {commandArgs}");

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            bool started = process.Start();

            if (!started)
            {
                return new Result.Error<string, string>("Cannot start 'ffprobe'.");
            }

            log.Add("Process started successfully.");
            log.Add("Started reading output.");

            string processOutput = process.StandardOutput.ReadToEnd().Trim();
            string processErr = process.StandardError.ReadToEnd().Trim();

            process.WaitForExit();

            stopWatch.Stop();

            log.Add($"Process took {stopWatch.ElapsedMilliseconds / 1000f}s to finish.");

            if (process.ExitCode != 0)
            {
                log.Add("");
                log.Add("[STD OUT]");
                log.Add("");
                log.AddRange(processOutput.Split('\n'));
                log.Add("");
                log.Add("[/STD OUT]");

                log.Add("");
                log.Add("[STD ERR]");
                log.Add("");
                log.AddRange(processErr.Split('\n'));
                log.Add("");
                log.Add("[/STD ERR]");

                return new Result.Error<string, string>($"ffprobe returned with exit code: {process.ExitCode}.");
            }

            return new Result.Ok<string, string>(processOutput);
        }

        public static List<string> SplitChapters(string ffprobeOutput)
        {
            List<string> output = new List<string>();

            IEnumerable<string> lines = ffprobeOutput.Split('\n').Select(line => line.Trim());
            lines = lines.SkipWhile(line => line != "[CHAPTER]");

            while (lines.Any())
            {
                List<string> chapterVals = new List<string>();
                chapterVals.Add(lines.First());
                lines = lines.Skip(1);
                chapterVals.AddRange(lines.TakeWhile(line => line != "[CHAPTER]"));
                lines = lines.SkipWhile(line => line != "[CHAPTER]");
                output.Add(string.Join('\n', chapterVals));
            }

            return output;
        }

        public static string? CreateCueFile(string parentDir, string originalFileName, string tracksString)
        {
            try
            {
                string cueFileName = Path.ChangeExtension(originalFileName, ".cue");
                string cueFilePath = Path.Combine(parentDir, cueFileName);

                using (FileStream cueFile = File.Create(cueFilePath))
                {
                    var writer = new StreamWriter(cueFile, Encoding.UTF8);
                    writer.WriteLine($"FILE \"{originalFileName}\" MP3");
                    writer.Write(tracksString);
                    writer.Flush();
                }
            }
            catch (Exception e)
            {
                return $"Error while writing `.cue` file: {e.Message}";
            }

            return null;
        }

        public static int Main(string[] args)
        {
            if (args.Any(arg => Regex.IsMatch(arg, @"^-*h(elp)?.*", RegexOptions.IgnoreCase)))
            {
                Console.WriteLine(Usage());
                return 0;
            }

            if (args.Length != 1)
            {
                Console.WriteLine("Invalid number of arguments\n");
                Console.WriteLine($"ARGS:{args.ArrToString()}");
                Console.WriteLine($"USAGE: {Usage()}");
                return 1;
            }

            string filePath = args.First();

            if (!Path.IsPathFullyQualified(filePath))
            {
                filePath = Path.GetFullPath(filePath);
            }

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"File `{filePath}` does not exist.\n");
                return 1;
            }

            if (!CheckFFProbe())
            {
                Console.WriteLine(
                    "Could not find `ffprobe` on yout system.\n" +
                    "Ensure `ffmpeg` package is installed and added to your path.\n" +
                    "(ffmpeg download: https://ffmpeg.org/download.html)");
                return 1;
            }

            var result = ProbeFile(filePath, out List<string> log);

            if (result.IsError())
            {
                Console.WriteLine(
                    $"Error while probing the file:\n" +
                    $"\t{result.getError()?.What}\n");
                Console.WriteLine(
                    $"Log:\n" +
                    $"\t{String.Join("\n\t", log)}");

                return 1;
            }


            string ffprobeOutput = result.getResult().Value;

            List<string> chapterStrings = SplitChapters(ffprobeOutput);
            List<IResult<FFChapter, string>> chapters = chapterStrings.Select(str => FFChapter.FromString(str)).ToList();

            if (chapters.Any(chr => chr.IsError()))
            {
                IEnumerable<string> errs = chapters.Where(chr => chr.IsError()).Select((chr, indx) => $"ERROR: {indx}: {chr.getError().What}\n");
                string innerErrs = string.Join('\n', errs);

                Console.WriteLine($"Errors while parsing `ffprobe` output:\n\n{innerErrs}");
                return 1;
            }

            List<FFChapter> sortedChapters = chapters.Select(chr => chr.getResult().Value).OrderBy(chr => chr.StartTime).ToList();

            string chaptersString = string.Join('\n', sortedChapters.Select((chr, indx) => chr.ToCueStr(indx)));
            string fileName = Path.GetFileName(filePath);
            string parentDir = Path.GetDirectoryName(filePath);

            string? err = CreateCueFile(parentDir,fileName, chaptersString);

            if (!string.IsNullOrWhiteSpace(err))
            {
                Console.WriteLine(err);
                return 1;
            }

            return 0;
        }
    }
}