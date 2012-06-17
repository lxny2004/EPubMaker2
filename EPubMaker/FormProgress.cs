﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace EPubMaker
{
    /// <summary>
    /// ePub生成進捗表示ダイアログ
    /// 実際の生成処理もこちら
    /// </summary>
    public partial class FormProgress : Form
    {
        #region 内部構造体
        private struct WorkerArg
        {
            public List<Page> pages;    /// ページ
            public string title;        /// 書籍タイトル
            public string author;       /// 著者
            public int width;           /// 出力幅
            public int height;          /// 出力高さ
            public string path;         /// 出力ファイルパス
        }
        #endregion

        #region コンストラクタ
        /// <summary>
        /// フォームコンストラクタ
        /// </summary>
        /// <param name="pages">ページ</param>
        /// <param name="title">書籍タイトル</param>
        /// <param name="author">著者</param>
        /// <param name="width">出力幅</param>
        /// <param name="height">出力高さ</param>
        /// <param name="path">出力ファイルパス</param>
        public FormProgress(List<Page> pages, string title, string author, int width, int height, string path)
        {
            InitializeComponent();

            WorkerArg arg = new WorkerArg();
            arg.pages = pages;
            arg.title = title.Trim();
            arg.author = author.Trim();
            arg.width = width;
            arg.height = height;
            arg.path = path;

            this.DialogResult = DialogResult.None;
            backgroundWorker.RunWorkerAsync(arg);
        }
        #endregion

        #region イベント
        #region フォーム
        /// <summary>
        /// フォームが閉じられそう
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FormProgress_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (this.DialogResult == DialogResult.None)
            {
                e.Cancel = true;
                backgroundWorker.CancelAsync();
            }
        }
        #endregion

        #region ボタン
        /// <summary>
        /// キャンセルボタン
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnClose_Click(object sender, EventArgs e)
        {
            backgroundWorker.CancelAsync();
        }
        #endregion

        #region バックグラウンド処理
        /// <summary>
        /// バックグラウンド処理本体
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            backgroundWorker.ReportProgress(-1);    // 開始

            WorkerArg arg = (WorkerArg)e.Argument;

            string tmpdir = Path.Combine(Path.GetTempPath(), "EPubMaker");
            if (Directory.Exists(tmpdir))
            {
                Directory.Delete(tmpdir, true);
            }
            Directory.CreateDirectory(tmpdir);
            backgroundWorker.ReportProgress(50, 100);

            string mime = Path.Combine(tmpdir, "mimetype");
            FileStream fs = File.OpenWrite(mime);
            WriteText(fs, "application/epub+zip");
            fs.Close();
            backgroundWorker.ReportProgress(60, 100);

            string meta = Path.Combine(tmpdir, "META-INF");
            Directory.CreateDirectory(meta);
            fs = File.OpenWrite(Path.Combine(meta, "container.xml"));
            WriteText(fs, "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<container version=\"1.0\" xmlns=\"urn:oasis:names:tc:opendocument:xmlns:container\">\n<rootfiles>\n<rootfile media-type=\"application/oebps-package+xml\" full-path=\"OEBPS/package.opf\" />\n</rootfiles>\n</container>\n");
            fs.Close();
            backgroundWorker.ReportProgress(70, 100);

            string contents = Path.Combine(tmpdir, "OEBPS");
            Directory.CreateDirectory(Path.Combine(contents, "data"));
            backgroundWorker.ReportProgress(80, 100);

            fs = File.OpenWrite(Path.Combine(contents, "package.opf"));
            WriteText(fs, "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n<package xmlns=\"http://www.idpf.org/2007/opf\" version=\"3.0\" unique-identifier=\"BookId\" xml:lang=\"ja\">\n<metadata xmlns:dc=\"http://purl.org/dc/elements/1.1/\">\n<dc:identifier id=\"BookId\">");
            string bookid = Guid.NewGuid().ToString();
            WriteText(fs, Escape(bookid));
            WriteText(fs, "</dc:identifier>\n<dc:title>");
            WriteText(fs, Escape(arg.title));
            WriteText(fs, "</dc:title>\n");
            if (arg.author.Length > 0)
            {
                WriteText(fs, "<dc:creator opf:file-as=\"");
                WriteText(fs, Escape(arg.author));
                WriteText(fs, "\" opf:role=\"aut\">");
                WriteText(fs, Escape(arg.author));
                WriteText(fs, "</dc:creator>\n");
            }
            WriteText(fs, "<dc:language>ja</dc:language>\n</metadata>\n<manifest>\n");
            WriteText(fs, "<item id=\"ncx\" href=\"toc.ncx\" media-type=\"application/x-dtbncx+xml\" />\n");
            backgroundWorker.ReportProgress(100, 100);

            backgroundWorker.ReportProgress(-2);    // 画像変換
            for (int i = 0; i < arg.pages.Count; ++i)
            {
                if (backgroundWorker.CancellationPending)
                {
                    fs.Close();
                    e.Cancel = true;
                    return;
                }

                Image src;
                Image dst = arg.pages[i].GenerateImages(arg.width, arg.height, out src);
                src.Dispose();

                if (src != null)
                {
                    string id = i.ToString("d4");
                    string file = String.Format(id + ".png");
                    string full = Path.Combine(contents, "data", file);
                    dst.Save(full, ImageFormat.Png);
                    dst.Dispose();

                    WriteText(fs, "<item id=\"" + id + "\" href=\"data/" + file + "\" media-type=\"image/png\" fallback=\"" + id + "f\"/>\n");
                    WriteText(fs, "<item id=\"" + id + "f\" href=\"data/" + id + "f.xhtml\" media-type=\"application/xhtml+xml\"/>\n");

                    FileStream xhtml = File.OpenWrite(Path.Combine(contents, "data", id + "f.xhtml"));
                    WriteText(xhtml, "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Strict//EN\"\n\"http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd\">\n<html xmlns=\"http://www.w3.org/1999/xhtml\" xml:lang=\"ja\" lang=\"ja\">\n<head>\n<title>-</title>\n<meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\"/>\n</head>\n<body>\n<img src=\"./");
                    WriteText(xhtml, file);
                    WriteText(xhtml, "\" />\n</body>\n</html>\n");
                    xhtml.Close();
                }
                else
                {
                    DialogResult ret = MessageBox.Show("何らかの理由でページ " + i + " の画像を生成できませんでした。\n処理を継続しますか?", Application.ProductName, MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
                    if (ret != DialogResult.Yes)
                    {
                        fs.Close();
                        e.Cancel = true;
                        return;
                    }
                }

                backgroundWorker.ReportProgress(i, arg.pages.Count);
            }

            backgroundWorker.ReportProgress(-3);    // 目次生成
            WriteText(fs, "</manifest>\n<spine toc=\"ncx\" page-progression-direction=\"rtl\">\n");
            for (int i = 0; i < arg.pages.Count; ++i)
            {
                if (backgroundWorker.CancellationPending)
                {
                    fs.Close();
                    e.Cancel = true;
                    return;
                }

                string id = i.ToString("d4");
                WriteText(fs, "<itemref idref=\"" + id + "f\"/>\n");

                backgroundWorker.ReportProgress(i, arg.pages.Count * 2);
            }
            WriteText(fs, "</spine>\n</package>\n");
            fs.Close();

            fs = File.OpenWrite(Path.Combine(contents, "toc.ncx"));
            WriteText(fs, "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n<!DOCTYPE ncx PUBLIC \"-//NISO//DTD ncx 2005-1//EN\" \"http://www.daisy.org/z3986/2005/ncx-2005-1.dtd\">\n<ncx version=\"2005-1\" xmlns=\"http://www.daisy.org/z3986/2005/ncx/\" xml:lang=\"ja\">\n<head>\n<meta name=\"dtb:uid\" content=\"");
            WriteText(fs, Escape(bookid));
            WriteText(fs, "\"/>\n<meta name=\"dtb:depth\" content=\"1\"/>\n<meta name=\"dtb:totalPageCount\" content=\"0\"/>\n<meta name=\"dtb:maxPageNumber\" content=\"0\"/>\n</head>\n<docTitle><text>");
            WriteText(fs, Escape(arg.title));
            WriteText(fs, "</text></docTitle>\n<navMap>\n");
            for (int i = 0, idx = 0; i < arg.pages.Count; ++i)
            {
                if (backgroundWorker.CancellationPending)
                {
                    fs.Close();
                    e.Cancel = true;
                    return;
                }

                string id = i.ToString("d4");
                if (!String.IsNullOrEmpty(arg.pages[i].Index))
                {
                    WriteText(fs, "<navPoint id=\"" + id + "f\" playOrder=\"" + (++idx).ToString() + "\">\n");
                    WriteText(fs, "<navLabel><text>" + Escape(arg.pages[i].Index) + "</text></navLabel>\n");
                    WriteText(fs, "<content src=\"data/" + id + "f.xhtml\"/>\n");
                    WriteText(fs, "</navPoint>\n");
                }

                backgroundWorker.ReportProgress(i + arg.pages.Count, arg.pages.Count * 2);
            }
            WriteText(fs, "</navMap>\n</ncx>\n");
            fs.Close();

            backgroundWorker.ReportProgress(-4);    // zip生成
            Zip zip = new Zip(arg.path);
            zip.CopyFrom(mime);
            backgroundWorker.ReportProgress(10, 100);
            zip.CopyFrom(meta);
            backgroundWorker.ReportProgress(30, 100);
            zip.CopyFrom(contents);
            backgroundWorker.ReportProgress(80, 100);
            zip.Close();
            backgroundWorker.ReportProgress(90, 100);

            try
            {
                Directory.Delete(tmpdir, true);
            }
            catch
            {
            }
            backgroundWorker.ReportProgress(100, 100);

            e.Result = true;
        }

        /// <summary>
        /// バックグラウンド処理進捗報告
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void backgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            switch (e.ProgressPercentage)
            {
                case -1:
                    totalProgress.Value = 0;
                    label.Text = "メタデータを生成しています...";
                    break;
                case -2:
                    totalProgress.Value = 0;
                    label.Text = "画像を変換しています...";
                    break;
                case -3:
                    totalProgress.Value = 0;
                    label.Text = "目次を生成しています...";
                    break;
                case -4:
                    totalProgress.Value = 0;
                    label.Text = "ePubファイルを生成しています...";
                    break;
                default:
                    totalProgress.Maximum = (int)e.UserState;
                    totalProgress.Value = e.ProgressPercentage;
                    break;
            }
        }

        /// <summary>
        /// バックグラウンド処理完了報告
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                this.DialogResult = DialogResult.Cancel;
            }
            else if (e.Error != null)
            {
                this.DialogResult = DialogResult.Abort;
            }
            else
            {
                this.DialogResult = DialogResult.OK;
            }
            this.Close();
        }
        #endregion
        #endregion

        #region プライベートメソッド
        /// <summary>
        /// 文字列出力
        /// </summary>
        /// <param name="st">出力先ストリーム</param>
        /// <param name="text">出力文字列</param>
        private static void WriteText(Stream st, string text)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text);
            st.Write(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// 文字列エスケープ(XML)
        /// </summary>
        /// <param name="str">対象文字列</param>
        /// <returns>エスケープ結果</returns>
        private static string Escape(string str)
        {
            return str.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }
        #endregion
    }
}
