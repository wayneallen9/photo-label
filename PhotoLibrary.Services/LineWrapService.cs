using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
namespace PhotoLibrary.Services
{
    public class LineWrapService : ILineWrapService
    {
        #region variables
        private readonly ILogService _logService;
        #endregion

        public LineWrapService(
            ILogService logService)
        {
            // save dependency injections
            _logService = logService;
        }

        public List<string> WrapToFitFromBottom(Graphics graphics, Size imageSize, string source, Font font)
        {
            var lineNumber = 0;
            var lines = new List<string>(new[] { source });

            _logService.TraceEnter();
            try
            {
                while (lineNumber > -1)
                {
                    while (graphics.MeasureString(lines[lineNumber], font).Width > imageSize.Width)
                    {
                        // get the current line
                        var line = lines[lineNumber];

                        // is there somewhere to break the line?
                        var firstWordMatch = Regex.Match(line, @"^(\s*\S+)\s");
                        if (!firstWordMatch.Success) break;

                        // do we need to add the line above this one?
                        if (lineNumber == 0)
                        {
                            // add the new line above
                            lines.Insert(0, string.Empty);

                            // the target line has been moved down one
                            lineNumber++;
                        }

                        // put the word on the end of the previous line
                        lines[lineNumber - 1] += firstWordMatch.Groups[1].Value;
                        lines[lineNumber] = line.Substring(firstWordMatch.Length - 1);
                    }

                    // go to the next line
                    lineNumber--;
                }

                return lines.SelectMany(s => s.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).Select(s1 => s1.Trim())).ToList();
            }
            finally
            {
                _logService.TraceExit();
            }
        }

        public List<string> WrapToFitFromTop(Graphics graphics, Size imageSize, string source, Font font)
        {
            var lineNumber = 0;
            var lines = new List<string>(new[] { source });

            _logService.TraceEnter();
            try
            {
                _logService.Trace("Checking if a caption has been specified...");
                if (string.IsNullOrEmpty(source))
                {
                    _logService.Trace("No caption has been specified.  Exiting...");
                    return new List<string>();
                }

                while (lineNumber < lines.Count)
                {
                    while (graphics.MeasureString(lines[lineNumber], font).Width > imageSize.Width)
                    {
                        // get the current line
                        var line = lines[lineNumber];

                        // is there somewhere to break the line?
                        var lastWordMatch = Regex.Match(line, @"\s(\S+\s*)$");
                        if (!lastWordMatch.Success) break;

                        // do we need to add the line below this one?
                        if (lineNumber == lines.Count - 1) lines.Add(string.Empty);

                        // put the word on the end of the previous line
                        lines[lineNumber + 1] = lastWordMatch.Groups[1].Value + lines[lineNumber + 1];
                        lines[lineNumber] = line.Substring(0, lines[lineNumber].Length - lastWordMatch.Length + 1);
                    }

                    // go to the next line
                    lineNumber++;
                }

                return lines.SelectMany(s => s.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).Select(s1 => s1.Trim())).ToList();
            }
            finally
            {
                _logService.TraceExit();
            }
        }
    }
}