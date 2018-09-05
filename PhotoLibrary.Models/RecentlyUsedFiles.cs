using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoLibrary.Models
{
    [Serializable]
    public class RecentlyUsedFiles : IEnumerable<string>
    {
        #region variables
        private readonly List<string> _filenames;
        #endregion

        public RecentlyUsedFiles()
        {
            _filenames = new List<string>();
        }

        public void Add(string filename)
        {
            // if it already exists, remove it
            if (_filenames.Contains(filename)) _filenames.Remove(filename);

            // insert it at the top
            _filenames.Insert(0, filename);

            // keep the maximum
            if (_filenames.Count > 10) _filenames.RemoveAt(10);
        }

        public IEnumerator<string> GetEnumerator()
        {
            return _filenames.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _filenames.GetEnumerator();
        }
    }
}
