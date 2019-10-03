using System;
using System.Collections.Generic;

namespace PhotoLabel.Wpf
{
    public class DateTakenComparer : IComparer<ImageViewModel>
    {
        public int Compare(ImageViewModel x, ImageViewModel y)
        {
            var xDateTaken = DateTime.TryParse(x.DateTaken, out DateTime resultX) ? resultX : x.DateCreated;
            var yDateTaken = DateTime.TryParse(y.DateTaken, out DateTime resultY) ? resultY : y.DateCreated;

            return xDateTaken.CompareTo(yDateTaken);
        }
    }
}
