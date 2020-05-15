using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PaintBox
{
    public partial class TextLabelCreation : Form
    {
        public string TextToWorkWith { get; set; }
        public Brush BrushStyle { get; set; }
        public Font FontStyle { get; set; }

        public TextLabelCreation()
        {
            InitializeComponent();

            TextToWorkWith = "";
            BrushStyle = new SolidBrush(Color.Black);
            FontStyle = textBoxEnter.Font;

            buttonCancel.Click += (s, e) => { Close(); };
        }

        private void buttonFont_Click(object sender, EventArgs e)
        {
            if (fontDialogTo.ShowDialog() == DialogResult.OK)
            {
                FontStyle = fontDialogTo.Font;
                textBoxEnter.Font = fontDialogTo.Font;
            }
        }
        private void buttonColor_Click(object sender, EventArgs e)
        {
            if (colorDialogTo.ShowDialog() == DialogResult.OK)
            {
                BrushStyle = new SolidBrush(colorDialogTo.Color);
                textBoxEnter.ForeColor = colorDialogTo.Color;
            }
        }
        private void buttonAccept_Click(object sender, EventArgs e)
        {
            TextToWorkWith = textBoxEnter.Text;
        }
    }
}
