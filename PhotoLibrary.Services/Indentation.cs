namespace PhotoLabel.Services
{
    public class Indentation : IIndentation
    {
        #region variables

        private int _indentation;

        #endregion

        public Indentation()
        {
            // initialise variables
            _indentation = 0;
        }

        public int Decrement()
        {
            // bounds check
            if (_indentation == 0) return 0;

            return --_indentation;
        }

        public int Increment()
        {
            return ++_indentation;
        }

        public override string ToString()
        {
            return new string('\t', _indentation);
        }
    }
}