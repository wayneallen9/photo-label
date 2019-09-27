using System.Collections.Generic;
using System.IO;

namespace Shared
{
    public class FileCreationDateComparer : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            var xCreationDate = File.GetCreationTimeUtc(x);
            var yCreationDate = File.GetCreationTimeUtc(y);

            return xCreationDate.CompareTo(yCreationDate);
        }
    }
}
