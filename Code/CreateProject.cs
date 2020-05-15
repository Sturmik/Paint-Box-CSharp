using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PaintBox
{
    public partial class CreateProject : Form
    {
        #region Variables

        public int ProjectWidth { get; set; }
        public int ProjectHeight { get; set; }
        public Color Color { get; set; }

        #endregion

        #region Methods

        private void numericUpDownWidth_ValueChanged(object sender, EventArgs e)
        {
            ProjectWidth = (int)numericUpDownWidth.Value;
        }
        private void numericUpDownHeight_ValueChanged(object sender, EventArgs e)
        {
            ProjectHeight = (int)numericUpDownHeight.Value;
        }
        private void pictureBoxColor_Click(object sender, EventArgs e)
        {
            if (colorDialogChange.ShowDialog() == DialogResult.OK)
            {
                pictureBoxColor.BackColor = colorDialogChange.Color;
                Color = pictureBoxColor.BackColor;
            }
        }

        #endregion

        public CreateProject()
        {
            ProjectWidth = 1;
            ProjectHeight = 1;
            Color = Color.White;

            InitializeComponent();

            buttonCancel.Click += (s, e) => { Close(); };

            numericUpDownWidth.Value = 1;
            numericUpDownHeight.Value = 1;
        }
    }
}
