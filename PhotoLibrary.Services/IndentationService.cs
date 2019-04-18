namespace PhotoLabel.Services
{
    public class IndentationService : IIndentationService
    {
        #region variables

        private int _indentation;

        #endregion

        public int Decrement()
        {
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