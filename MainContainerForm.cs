using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FileComparerAppWin
{
    public partial class MainContainerForm: Form
    {
        private TabControl tabControl;
        private Point mousePosition = Point.Empty; // 🖱️ track mouse for hover


        public MainContainerForm()
        {
            this.Text = "🔍 File Comparison Tool";
            this.Size = new System.Drawing.Size(1200, 800);

            tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                DrawMode = TabDrawMode.OwnerDrawFixed
            };

            tabControl.DrawItem += TabControl_DrawItem;
            tabControl.MouseDown += TabControl_MouseDown;
            tabControl.MouseMove += TabControl_MouseMove;


            this.Controls.Add(tabControl);

            // 🟢 First tab = Folder Compare tab
            AddTab("Compare Folders", new MainForm(this), false); // pass reference
        }

        // 📌 Call this to open a new diff tab
        public void OpenDiffTab(string title, string primaryCode, string secondaryCode)
        {
            var diffForm = new DiffViewerForm(primaryCode, secondaryCode);
            var tabPage = new TabPage(title);
            {
                Tag = true;
             }   
            ;
            diffForm.TopLevel = false;
            diffForm.FormBorderStyle = FormBorderStyle.None;
            diffForm.Dock = DockStyle.Fill;
            tabPage.Controls.Add(diffForm);
            tabControl.TabPages.Add(tabPage);
            tabControl.SelectedTab = tabPage;
            diffForm.Show();
        }

        // 📌 Add a Form into tab
        public void AddTab(string title, Form innerForm, bool closable = false)
        {
            var tabPage = new TabPage(title)
            {
                Tag = closable // 👈 store whether it's closable
            };

            innerForm.TopLevel = false;
            innerForm.FormBorderStyle = FormBorderStyle.None;
            innerForm.Dock = DockStyle.Fill;
            tabPage.Controls.Add(innerForm);
            tabControl.TabPages.Add(tabPage);
            innerForm.Show();
        }

        private void TabControl_DrawItem(object sender, DrawItemEventArgs e)
        {
            var tabPage = tabControl.TabPages[e.Index];
            var tabBounds = tabControl.GetTabRect(e.Index);
            bool isSelected = (e.Index == tabControl.SelectedIndex);
            bool isClosable = tabPage.Tag is bool closable && closable;

            // 🌫️ Glassy gradient background
            Color baseColor = isSelected ? Color.FromArgb(200, 230, 240, 255) : Color.FromArgb(180, 250, 250, 250);
            Color shineColor = isSelected ? Color.FromArgb(100, 255, 255, 255) : Color.FromArgb(80, 255, 255, 255);

            using (LinearGradientBrush backgroundBrush = new LinearGradientBrush(tabBounds, baseColor, shineColor, LinearGradientMode.Vertical))
            {
                e.Graphics.FillRectangle(backgroundBrush, tabBounds);
            }

            // 🖋 Optional border
            e.Graphics.DrawRectangle(Pens.Gray, tabBounds);

            // 🆎 Small smooth font
            using (Font smallFont = new Font(this.Font.FontFamily, this.Font.Size - 1.5f, FontStyle.Regular))
            {
                Rectangle textRect = new Rectangle(tabBounds.X + 2, tabBounds.Y + 2, tabBounds.Width - (isClosable ? 24 : 10), tabBounds.Height - 2);
                TextRenderer.DrawText(
                    e.Graphics,
                    tabPage.Text,
                    smallFont,
                    textRect,
                    Color.Black,
                    TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter
                );
            }

            // ❌ Close button on hover
            if (isClosable)
            {
                Rectangle closeRect = new Rectangle(tabBounds.Right - 20, tabBounds.Top + 6, 16, 16);

                if (closeRect.Contains(mousePosition)) // only show on hover
                {
                    // 🔴 Draw red circular background
                    using (Brush redBrush = new SolidBrush(Color.Red))
                    {
                        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        e.Graphics.FillEllipse(redBrush, closeRect);
                    }

                    // ❌ Draw white "X" centered
                    TextRenderer.DrawText(
                        e.Graphics,
                        "X",
                        this.Font,
                        closeRect,
                        Color.White,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter
                    );
                }
            }

        }





        // 🖱 Detect click on close icon and remove tab
        private void TabControl_MouseDown(object sender, MouseEventArgs e)
        {
            for (int i = 0; i < tabControl.TabPages.Count; i++)
            {
                Rectangle rect = tabControl.GetTabRect(i);
                Rectangle closeRect = new Rectangle(rect.Right - 15, rect.Top + 4, 12, 12);

                if (closeRect.Contains(e.Location))
                {
                    var tab = tabControl.TabPages[i];
                    if (tab.Controls.Count > 0 && tab.Controls[0] is Form form)
                    {
                        form.Close();
                        form.Dispose();
                    }
                    tabControl.TabPages.RemoveAt(i);
                    break;
                }
            }
        }

        private void TabControl_MouseMove(object sender, MouseEventArgs e)
        {
            // 🧠 Update stored mouse position and trigger redraw
            mousePosition = e.Location;
            tabControl.Invalidate(); // force redraw
        }
    }
}
