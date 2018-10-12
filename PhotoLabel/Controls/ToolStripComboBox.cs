using System.Windows.Forms;
using System.ComponentModel;
namespace PhotoLabel.Controls
{
    /// <summary>
    /// Standard ToolStripComboBox with DrawItem event.
    /// </summary>
    public class ToolStripComboBox : System.Windows.Forms.ToolStripComboBox
    {
        #region events
        public event DrawItemEventHandler DrawItem;
        #endregion

        #region variables
        private readonly ComboBox _comboBox;
        #endregion

        public ToolStripComboBox()
        {
            // get the underlying control
            _comboBox = Control as ComboBox;

            // add event handlers
            _comboBox.DrawItem += (sender, e) =>
            {
                // bubble the event up
                OnDrawItem(e);
            };
        }

        [DefaultValue(DrawMode.Normal)]
        public DrawMode DrawMode
        {
            get => _comboBox.DrawMode;
            set
            {
                _comboBox.DrawMode = value;
            }
        }

        protected virtual void OnDrawItem(DrawItemEventArgs e)
        {
            DrawItem?.Invoke(this, e);
        }
    }
}