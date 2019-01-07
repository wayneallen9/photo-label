using System.Collections.Generic;

namespace PhotoLabel.Services
{
    public interface IQuickCaptionService
    {
        void Add(string date, string caption);
        void Clear();
        IEnumerable<string> Get(string date);
    }
}