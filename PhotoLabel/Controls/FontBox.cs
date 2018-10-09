using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Text;
using System.Windows.Forms;
namespace PhotoLabel.Controls
{
    public class FontBox : ToolStripComboBox
    {
        #region  Private Member Declarations  

        private Dictionary<string, Font> _fontCache;
        private int _itemHeight;
        private int _previewFontSize;
        private StringFormat _stringFormat;

        #endregion  Private Member Declarations  

        #region  Public Constructors  

        public FontBox()
        {
            _fontCache = new Dictionary<string, Font>();

            DrawMode = System.Windows.Forms.DrawMode.OwnerDrawVariable;
            Sorted = true;
            PreviewFontSize = 12;

            CalculateLayout();
            CreateStringFormat();
        }

        #endregion  Public Constructors  

        #region  Events  

        public event EventHandler PreviewFontSizeChanged;

        #endregion  Events  

        #region  Protected Overridden Methods  

        protected override void Dispose(bool disposing)
        {
            ClearFontCache();

            if (_stringFormat != null)
                _stringFormat.Dispose();

            base.Dispose(disposing);
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            base.OnDrawItem(e);

            if (e.Index > -1 && e.Index < Items.Count)
            {
                e.DrawBackground();

                if ((e.State & System.Windows.Forms.DrawItemState.Focus) == System.Windows.Forms.DrawItemState.Focus)
                    e.DrawFocusRectangle();

                using (SolidBrush textBrush = new SolidBrush(e.ForeColor))
                {
                    string fontFamilyName;

                    fontFamilyName = Items[e.Index].ToString();
                    e.Graphics.DrawString(fontFamilyName, GetFont(fontFamilyName),
                  textBrush, e.Bounds, _stringFormat);
                }
            }
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);

            CalculateLayout();
        }

        protected override void OnGotFocus(EventArgs e)
        {
            LoadFontFamilies();

            base.OnGotFocus(e);
        }

        protected override void OnMeasureItem(MeasureItemEventArgs e)
        {
            base.OnMeasureItem(e);

            if (e.Index > -1 && e.Index < Items.Count)
            {
                e.ItemHeight = _itemHeight;
            }
        }

        protected override void OnRightToLeftChanged(EventArgs e)
        {
            base.OnRightToLeftChanged(e);

            CreateStringFormat();
        }

        protected override void OnTextChanged(EventArgs e)
        {
            base.OnTextChanged(e);

            if (Items.Count == 0)
            {
                int selectedIndex;

                LoadFontFamilies();

                selectedIndex = FindStringExact(Text);
                if (selectedIndex != -1)
                    SelectedIndex = selectedIndex;
            }
        }

        #endregion  Protected Overridden Methods  

        #region  Public Methods  

        public virtual void LoadFontFamilies()
        {
            if (Items.Count == 0)
            {
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor;

                foreach (FontFamily fontFamily in FontFamily.Families)
                    Items.Add(fontFamily.Name);

                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.Default;
            }
        }

        #endregion  Public Methods  

        #region  Public Properties  

        [Browsable(false), DesignerSerializationVisibility
        (DesignerSerializationVisibility.Hidden),
        EditorBrowsable(EditorBrowsableState.Never)]
        public new DrawMode DrawMode
        {
            get { return base.DrawMode; }
            set { base.DrawMode = value; }
        }

        [Category("Appearance"), DefaultValue(12)]
        public int PreviewFontSize
        {
            get { return _previewFontSize; }
            set
            {
                _previewFontSize = value;

                OnPreviewFontSizeChanged(EventArgs.Empty);
            }
        }

        [Browsable(false), DesignerSerializationVisibility
        (DesignerSerializationVisibility.Hidden),
        EditorBrowsable(EditorBrowsableState.Never)]
        public new bool Sorted
        {
            get { return base.Sorted; }
            set { base.Sorted = value; }
        }

        #endregion  Public Properties  

        #region  Private Methods  

        private void CalculateLayout()
        {
            ClearFontCache();

            using (Font font = new Font(Font.FontFamily, (float)PreviewFontSize))
            {
                Size textSize;

                textSize = System.Windows.Forms.TextRenderer.MeasureText("yY", font);
                _itemHeight = textSize.Height + 2;
            }
        }

        private bool IsUsingRTL(Control control)
        {
            bool result;

            if (control.RightToLeft == System.Windows.Forms.RightToLeft.Yes)
                result = true;
            else if (control.RightToLeft == System.Windows.Forms.RightToLeft.Inherit && control.Parent != null)
                result = IsUsingRTL(control.Parent);
            else
                result = false;

            return result;
        }

        #endregion  Private Methods  

        #region  Protected Methods  

        protected virtual void ClearFontCache()
        {
            if (_fontCache != null)
            {
                foreach (string key in _fontCache.Keys)
                    _fontCache[key].Dispose();
                _fontCache.Clear();
            }
        }

        protected virtual void CreateStringFormat()
        {
            if (_stringFormat != null)
                _stringFormat.Dispose();

            _stringFormat = new StringFormat(StringFormatFlags.NoWrap);
            _stringFormat.Trimming = StringTrimming.EllipsisCharacter;
            _stringFormat.HotkeyPrefix = HotkeyPrefix.None;
            _stringFormat.Alignment = StringAlignment.Near;
            _stringFormat.LineAlignment = StringAlignment.Center;

            if (this.IsUsingRTL(this))
                _stringFormat.FormatFlags |= StringFormatFlags.DirectionRightToLeft;
        }

        protected virtual Font GetFont(string fontFamilyName)
        {
            lock (_fontCache)
            {
                if (!_fontCache.ContainsKey(fontFamilyName))
                {
                    Font font;

                    font = GetFont(fontFamilyName, FontStyle.Regular);
                    if (font == null)
                        font = GetFont(fontFamilyName, FontStyle.Bold);
                    if (font == null)
                        font = GetFont(fontFamilyName, FontStyle.Italic);
                    if (font == null)
                        font = GetFont(fontFamilyName, FontStyle.Bold | FontStyle.Italic);
                    if (font == null)
                        font = (Font)Font.Clone();

                    _fontCache.Add(fontFamilyName, font);
                }
            }

            return _fontCache[fontFamilyName];
        }

        protected virtual Font GetFont(string fontFamilyName, FontStyle fontStyle)
        {
            Font font;

            try
            {
                font = new Font(fontFamilyName, PreviewFontSize, fontStyle);
            }
            catch
            {
                font = null;
            }

            return font;
        }

        protected virtual void OnPreviewFontSizeChanged(EventArgs e)
        {
            if (PreviewFontSizeChanged != null)
                PreviewFontSizeChanged(this, e);

            CalculateLayout();
        }

        #endregion  Protected Methods  
    }
}