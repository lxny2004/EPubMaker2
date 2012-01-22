﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace EPubMaker
{
    public partial class FormMain : Form
    {
        private List<Page> pages;
        private bool gridChanging;

        private Setting setting;

        public FormMain()
        {
            InitializeComponent();

            setting = Setting.Load();

            pages = null;
            gridChanging = false;

            editWidth.Value = setting.PageWidth;
            editHeight.Value = setting.PageHeight;

            splitContainer_Panel1_ClientSizeChanged(null, null);
            splitContainer_Panel2_ClientSizeChanged(null, null);

            menuItemClose.Enabled = false;
            menuItemGenerate.Enabled = false;
        }

        private void FormMain_ClientSizeChanged(object sender, EventArgs e)
        {
            splitContainer.Left = pageLabel.Left;
            splitContainer.Width = pageLabel.Width;
            splitContainer.Top = pageLabel.Bottom;
            splitContainer.Height = ClientRectangle.Bottom - splitContainer.Top;
        }

        private void menuItemOpen_Click(object sender, EventArgs e)
        {
            if (!String.IsNullOrEmpty(setting.PrevSrc) && Directory.Exists(setting.PrevSrc))
            {
                folderBrowserDialog.SelectedPath = setting.PrevSrc;
            }
            else
            {
                folderBrowserDialog.SelectedPath = null;
            }
            if (folderBrowserDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }
            setting.PrevSrc = folderBrowserDialog.SelectedPath;
            setting.Save();

            if (pages != null)
            {
                pages.Clear();
            }
            else
            {
                pages = new List<Page>();
            }

            DirectoryInfo dir = new DirectoryInfo(folderBrowserDialog.SelectedPath);
            foreach (FileInfo file in dir.GetFiles())
            {
                if (String.Compare(file.Extension, ".jpg", true) == 0 || String.Compare(file.Extension, ".png", true) == 0 || String.Compare(file.Extension, ".bmp", true) == 0)
                {
                    Page page = new Page(file.FullName);
                    pages.Add(page);
                }
            }
            pages.Sort(delegate(Page a, Page b)
            {
                return String.Compare(a.Name, b.Name, true);
            });

            UpdatePageList();

            string name = Path.GetFileNameWithoutExtension(folderBrowserDialog.SelectedPath);
            if (name.Contains('-'))
            {
                string[] ary = name.Split("-".ToCharArray(), 2);
                editTitle.Text = ary[0];
                editAuthor.Text = ary[1];
            }
            else
            {
                editTitle.Text = name;
            }

            menuItemClose.Enabled = true;
            menuItemGenerate.Enabled = true;
        }

        private void menuItemClose_Click(object sender, EventArgs e)
        {
            pages.Clear();
            pagesGrid.Rows.Clear();

            pageLabel.Text = "";
            srcPicture.Image = null;
            previewPicture.Image = null;
            srcLabel.Text = "";
            previewLabel.Text = "";

            menuItemClose.Enabled = false;
            menuItemGenerate.Enabled = false;
        }

        private void menuItemExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void MenuItemGenerate_Click(object sender, EventArgs e)
        {
            saveFileDialog.FileName = Path.GetFileName(folderBrowserDialog.SelectedPath);
            if (!String.IsNullOrEmpty(setting.OutPath) && Directory.Exists(setting.OutPath))
            {
                saveFileDialog.InitialDirectory = setting.OutPath;
            }
            else
            {
                saveFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }
            if (saveFileDialog.ShowDialog() != DialogResult.OK)
            {
                return;
            }
            setting.OutPath = Path.GetDirectoryName(saveFileDialog.FileName);
            setting.PageWidth = (int)editWidth.Value;
            setting.PageHeight = (int)editHeight.Value;
            setting.Save();

            FormProgress formProgress = new FormProgress(pages, editTitle.Text, editAuthor.Text, (int)editWidth.Value, (int)editHeight.Value, saveFileDialog.FileName);
            formProgress.ShowDialog(this);
            formProgress.Dispose();
        }

        private void UpdatePageList()
        {
            pagesGrid.Rows.Clear();
            for (int i = 0; i < pages.Count; ++i)
            {
                int idx = pagesGrid.Rows.Add(i + 1, pages[i].Name, pages[i].Index, false);
            }
        }

        private void btnSelectAll_Click(object sender, EventArgs e)
        {
            if (gridChanging)
            {
                return;
            }

            gridChanging = true;
            for (int i = pagesGrid.Rows.Count - 1; i >= 0; --i)
            {
                if ((bool)pagesGrid.Rows[i].Cells[3].Value)
                {
                    pagesGrid.Rows[i].Selected = false;
                }
                else
                {
                    pagesGrid.Rows[i].Selected = true;
                }
            }
            gridChanging = false;
            pagesGrid_SelectionChanged(sender, e);
        }

        private void btnSelectOdd_Click(object sender, EventArgs e)
        {
            if (gridChanging)
            {
                return;
            }

            gridChanging = true;
            for (int i = pagesGrid.Rows.Count - 1; i >= 0; --i)
            {
                if ((bool)pagesGrid.Rows[i].Cells[3].Value)
                {
                    pagesGrid.Rows[i].Selected = false;
                }
                else
                {
                    pagesGrid.Rows[i].Selected = i % 2 == 0;
                }
            }
            gridChanging = false;
            pagesGrid_SelectionChanged(sender, e);
        }

        private void btnSelectEven_Click(object sender, EventArgs e)
        {
            if (gridChanging)
            {
                return;
            }

            gridChanging = true;
            for (int i = pagesGrid.Rows.Count - 1; i >= 0; --i)
            {
                if ((bool)pagesGrid.Rows[i].Cells[3].Value)
                {
                    pagesGrid.Rows[i].Selected = false;
                }
                else
                {
                    pagesGrid.Rows[i].Selected = i % 2 != 0;
                }
            }
            gridChanging = false;
            pagesGrid_SelectionChanged(sender, e);
        }

        private void pagesGrid_SelectionChanged(object sender, EventArgs e)
        {
            if (gridChanging)
            {
                return;
            }

            gridChanging = true;
            int idx = -1;
            foreach (DataGridViewRow row in pagesGrid.SelectedRows)
            {
                if (/*row.Index < idx ||*/ idx < 0)
                {
                    idx = row.Index;
                }
            }
            if (idx >= 0 && idx < pages.Count)
            {
                rotateCombo.SelectedIndex = pages[idx].Rotate;
                RedrawImages(idx);
            }
            gridChanging = false;
        }

        private void rotateCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (rotateCombo.SelectedIndex < 0 || gridChanging)
            {
                return;
            }

            gridChanging = true;
            int idx = -1;
            foreach (DataGridViewRow row in pagesGrid.SelectedRows)
            {
                if (row.Index < pages.Count)
                {
                    pages[row.Index].Rotate = rotateCombo.SelectedIndex;
                    if (row.Index < idx || idx < 0)
                    {
                        idx = row.Index;
                    }
                }
            }

            if (idx >= 0)
            {
                RedrawImages(idx);
            }
            gridChanging = false;
        }

        private void formatCombo_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (rotateCombo.SelectedIndex < 0 || gridChanging)
            {
                return;
            }

            gridChanging = true;
            int idx = -1;
            foreach (DataGridViewRow row in pagesGrid.SelectedRows)
            {
                if (row.Index < pages.Count)
                {
                    pages[row.Index].Format = (Page.PageFormat)formatCombo.SelectedIndex;
                    if (row.Index < idx || idx < 0)
                    {
                        idx = row.Index;
                    }
                }
            }

            if (idx >= 0)
            {
                RedrawImages(idx);
            }
            gridChanging = false;
        }

        private void splitContainer_Panel1_ClientSizeChanged(object sender, EventArgs e)
        {
            srcPicture.Width = splitContainer.Panel1.ClientSize.Width;
            srcPicture.Height = splitContainer.Panel1.ClientSize.Height - srcLabel.Height;
        }

        private void splitContainer_Panel2_ClientSizeChanged(object sender, EventArgs e)
        {
            previewPicture.Width = splitContainer.Panel2.ClientSize.Width;
            previewPicture.Height = splitContainer.Panel2.ClientSize.Height - previewLabel.Height;
        }

        private void srcPicture_ClientSizeChanged(object sender, EventArgs e)
        {
            if (srcPicture.Image != null)
            {
                int zoom;
                if (srcPicture.ClientRectangle.Width < srcPicture.Image.Width || srcPicture.ClientRectangle.Height < srcPicture.Image.Height)
                {
                    srcPicture.SizeMode = PictureBoxSizeMode.Zoom;
                    double dw = (double)srcPicture.ClientRectangle.Width / srcPicture.Image.Width;
                    double dh = (double)srcPicture.ClientRectangle.Height / srcPicture.Image.Height;
                    zoom = (int)((dw < dh ? dw : dh) * 100);
                }
                else
                {
                    srcPicture.SizeMode = PictureBoxSizeMode.CenterImage;
                    zoom = 100;
                }

                srcLabel.Text = String.Format("{0}x{1} ({2}%)", srcPicture.Image.Width, srcPicture.Image.Height, zoom);
            }
        }

        private void previewPicture_ClientSizeChanged(object sender, EventArgs e)
        {
            if (previewPicture.Image != null)
            {
                int zoom;
                if (previewPicture.ClientRectangle.Width < previewPicture.Image.Width || previewPicture.ClientRectangle.Height < previewPicture.Image.Height)
                {
                    previewPicture.SizeMode = PictureBoxSizeMode.Zoom;
                    double dw = (double)previewPicture.ClientRectangle.Width / previewPicture.Image.Width;
                    double dh = (double)previewPicture.ClientRectangle.Height / previewPicture.Image.Height;
                    zoom = (int)((dw < dh ? dw : dh) * 100);
                }
                else
                {
                    previewPicture.SizeMode = PictureBoxSizeMode.CenterImage;
                    zoom = 100;
                }

                previewLabel.Text = String.Format("{0}x{1} ({2}%)", previewPicture.Image.Width, previewPicture.Image.Height, zoom);
            }
        }

        private void pagesGrid_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            // 「目次」のみ扱う
            if (gridChanging || e.ColumnIndex != 2 || pages == null || e.RowIndex >= pages.Count)
            {
                return;
            }

            gridChanging = true;

            pages[e.RowIndex].Index = pagesGrid.Rows[e.RowIndex].Cells[2].Value.ToString();

            gridChanging = false;
        }

        private void RedrawImages(int idx)
        {
            Image src;
            Image preview;
            pages[idx].GenerateImages(out src, (int)editWidth.Value, (int)editHeight.Value, out preview);

            srcPicture.Image = src;
            srcPicture_ClientSizeChanged(null, null);

            previewPicture.Image = preview;
            previewPicture_ClientSizeChanged(null, null);

            pageLabel.Text = String.Format("{0} ({1}ページ)", pages[idx].Name, idx + 1);
            if (!String.IsNullOrEmpty(pages[idx].Index))
            {
                pageLabel.Text += " " + pages[idx].Index;
            }

            formatCombo.SelectedIndex = (int)pages[idx].Format;
        }
    }
}
