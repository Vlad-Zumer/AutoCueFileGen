using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CueFileGen
{
    internal struct FFTime : IComparable<FFTime>
    {
        public int Hours { get; set; } = 0;
        public int Mins { get; set; } = 0;
        public int Secs { get; set; } = 0;
        public int Micros { get; set; } = 0;

        public FFTime()
        { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="str">H*:MM:SS.ms</param>
        /// <returns></returns>
        public static IResult<FFTime,string> FromString(string str)
        {
            try
            {
                FFTime time = new FFTime();

                string[] timeComponents = str.Split(':');

                time.Hours = int.Parse(timeComponents[0]);
                time.Mins = int.Parse(timeComponents[1]);

                timeComponents = timeComponents[2].Split('.');
                time.Secs = int.Parse(timeComponents[0]);
                time.Micros = int.Parse(timeComponents[1]);

                return new Result.Ok<FFTime,string>(time);
            }
            catch (Exception e)
            {
                return new Result.Error<FFTime, string>($"FFTime::FromString argument '{str}' is not int the correct format: H*:MM:SS.MICSEC");
            }
        }

        public string ToCueStr()
        {
            long cueMins = this.Hours * 60 + this.Mins;
            int cueSecs = this.Secs;
            
            // micros: 0 -> 999999
            // frame:  0 -> 74
            int cueFrames = (this.Micros * 74)/999999;

            return $"{cueMins}:{cueSecs.ToString("D2")}:{cueFrames}";
        }

        public int CompareTo(FFTime other)
        {
            var hrsComp = this.Hours.CompareTo(other.Hours);
            var minComp = this.Mins.CompareTo(other.Mins);
            var secComp = this.Secs.CompareTo(other.Secs);
            var microComp = this.Micros.CompareTo(other.Micros);

            return hrsComp != 0 ? hrsComp : (
                    minComp != 0 ? minComp : (
                    secComp != 0 ? secComp : microComp));
        }
    }

    internal class FFChapter
    {
        public int Id { get; set; } = 0;

        public FFTime StartTime { get; set; }

        internal FFTime EndTime { get; set; }

        public string Title { get; set; } = "";

        public FFChapter()
        {}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="str">
        /// EXAMPLE: 
        /// <br/>
        /// <br/>
        /// [CHAPTER] <br/>
        /// id=0 <br/>
        /// time_base=1/44100 <br/>
        /// start=0 <br/>
        /// start_time=0:00:00.000000 <br/>
        /// end=632790 <br/>
        /// end_time=0:00:14.348980 <br/>
        /// TAG:title=Opening Credits <br/>
        /// [/ CHAPTER]
        /// </param>
        /// <returns></returns>
        public static IResult<FFChapter, string> FromString(string str)
        {
            try
            {
                FFChapter chapter = new FFChapter();

                IEnumerable<string> lines = str.Split('\n').Select(line => line.Trim());

                if (lines.First() != "[CHAPTER]" || lines.Last() != "[/CHAPTER]")
                {
                    return new Result.Error<FFChapter, string>($"FFChapter::FromString argument has wrong structure.\n{str}");
                }

                Dictionary<string, string> propMap = lines
                    .Skip(1)
                    .SkipLast(1)
                    .Where(line => !line.StartsWith("TAG:"))
                    .Where(line => line.Contains("="))
                    .Select(line =>
                        {
                            int indx = line.IndexOf("=");
                            return (line.Substring(0, indx), line.Substring(indx + 1));
                        })
                    .ToDictionary(tuple => tuple.Item1, tuple=>tuple.Item2);

                Dictionary<string, string> tagsMap = lines
                    .Where(line => line.StartsWith("TAG:"))
                    .Select(line =>
                        {
                            line = line.Substring(4);
                            int indx = line.IndexOf("=");
                            return (line.Substring(0, indx), line.Substring(indx + 1));
                        })
                    .ToDictionary(tuple => tuple.Item1, tuple => tuple.Item2);

                chapter.Id = int.Parse(propMap["id"]);
                chapter.Title = tagsMap.GetValueOrDefault("title","");

                var startTimeResult = FFTime.FromString(propMap["start_time"]);
                var endTimeResult = FFTime.FromString(propMap["end_time"]);

                if (startTimeResult.IsError())
                {
                    return new Result.Error<FFChapter, string>(startTimeResult.getError().What);
                }

                if (endTimeResult.IsError())
                {
                    return new Result.Error<FFChapter, string>(endTimeResult.getError().What);
                }

                chapter.StartTime = startTimeResult.getResult().Value;
                chapter.EndTime = endTimeResult.getResult().Value;

                return new Result.Ok<FFChapter, string>(chapter);
            }
            catch (Exception e)
            {
                return new Result.Error<FFChapter, string>($"FFChapter::FromString threw:\n{e.Message}");
            }
        }

        public string ToCueStr(int trackNumber)
        {
            string title = string.IsNullOrWhiteSpace(this.Title) ? $"Chapter {this.Id}" : this.Title;

            return $"TRACK {trackNumber} AUDIO\n" +
                $"  TITLE \"{title}\"\n" +
                $"  INDEX 01 {this.StartTime.ToCueStr()}";
        }

    }
}
