using MetroFramework;
using MetroFramework.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PlayGate
{
    public class SceneViewAssetsControl : MetroPanel
    {
        #region Private static data.

        private static readonly int DEFAULT_SIZE = 32;
        private static readonly Size DEFAULT_TILE_SIZE = new Size(148, 74);
        private static readonly int DEFAULT_SEPARATOR = 2;
        private static readonly string[] ASSET_TYPES = new string[]{
            "All",
            "Animation",
            "Audio",
            "Cubemap",
            "Css",
            "Json",
            "Html",
            "Material",
            "Model",
            "Script",
            "Text",
            "Texture"
        };
        private static readonly string[] ASSET_FILTERS = new string[]{
            "*",
            "*.animation",
            "*.mp3;*.ogg",
            "*.cubemap",
            "*.css",
            "*.json",
            "*.html",
            "*.material",
            "*.model",
            "*.js",
            "*.txt",
            "*.png;*.jpeg;*.jpg"
        };
        private static readonly string[] CREATE_ASSET_EXTENSIONS = new string[]{
            "",
            "animation",
            "",
            "cubemap",
            "css",
            "json",
            "html",
            "material",
            "",
            "js",
            "txt",
            ""
        };

        #endregion



        #region Private data.

        private SceneViewPageControl m_owner;
        private MetroPanel m_toolbarPanel;
        private MetroTileIcon m_menuAddTile;
        private MetroComboBox m_menuTypeCombobox;
        private MetroTextBox m_menuSearchTextbox;
        private MetroLabel m_menuPathLabel;
        private MetroPanel m_filesListPanel;
        private MetroScrollBar m_filesListScrollbar;
        private string m_viewPath;
        private Image m_backImage;
        private Image m_dirImage;
        private Image m_fileImage;
        private Image m_fileCodeImage;
        private Image m_fileImageImage;
        private Image m_fileMusicImage;
        private Image m_fileTextImage;
        private Image m_fileDataImage;

        #endregion



        #region Public properties.

        public string ViewPath { get { return m_viewPath; } }

        #endregion



        #region Construction and destruction.

        public SceneViewAssetsControl(SceneViewPageControl owner)
        {
            MetroSkinManager.ApplyMetroStyle(this);
            m_owner = owner;
            m_backImage = Bitmap.FromFile("resources/icons/appbar.arrow.left.png");
            m_dirImage = Bitmap.FromFile("resources/icons/appbar.folder.png");
            m_fileImage = Bitmap.FromFile("resources/icons/appbar.page.png");
            m_fileCodeImage = Bitmap.FromFile("resources/icons/appbar.page.code.png");
            m_fileImageImage = Bitmap.FromFile("resources/icons/appbar.page.image.png");
            m_fileMusicImage = Bitmap.FromFile("resources/icons/appbar.page.music.png");
            m_fileTextImage = Bitmap.FromFile("resources/icons/appbar.page.text.png");
            m_fileDataImage = Bitmap.FromFile("resources/icons/appbar.page.xml.png");
            Resize += SceneViewAssetsControl_Resize;

            m_toolbarPanel = new MetroPanel();
            MetroSkinManager.ApplyMetroStyle(m_toolbarPanel);
            m_toolbarPanel.Dock = DockStyle.Top;
            m_toolbarPanel.Height = DEFAULT_SIZE;

            m_menuAddTile = new MetroTileIcon();
            MetroSkinManager.ApplyMetroStyle(m_menuAddTile);
            m_menuAddTile.Top = 2;
            m_menuAddTile.Width = DEFAULT_SIZE - 4;
            m_menuAddTile.Height = DEFAULT_SIZE - 4;
            m_menuAddTile.Image = Bitmap.FromFile("resources/icons/appbar.add.png");
            m_menuAddTile.IsImageScaled = true;
            m_menuAddTile.ImageScale = new PointF(0.5f, 0.5f);
            m_menuAddTile.MouseUp += m_menuAddTile_MouseUp;
            m_toolbarPanel.Controls.Add(m_menuAddTile);

            m_menuTypeCombobox = new MetroComboBox();
            MetroSkinManager.ApplyMetroStyle(m_menuTypeCombobox);
            m_menuTypeCombobox.Left = m_menuAddTile.Right + DEFAULT_SEPARATOR;
            m_menuTypeCombobox.Width = 100;
            m_menuTypeCombobox.Height = DEFAULT_SIZE;
            m_menuTypeCombobox.BindingContext = new BindingContext();
            m_menuTypeCombobox.DataSource = ASSET_TYPES;
            m_menuTypeCombobox.SelectedValueChanged += m_menuTypeCombobox_SelectedValueChanged;
            m_toolbarPanel.Controls.Add(m_menuTypeCombobox);

            m_menuSearchTextbox = new MetroTextBox();
            MetroSkinManager.ApplyMetroStyle(m_menuSearchTextbox);
            m_menuSearchTextbox.Left = m_menuTypeCombobox.Right + DEFAULT_SEPARATOR;
            m_menuSearchTextbox.Width = 250;
            m_menuSearchTextbox.Height = DEFAULT_SIZE;
            m_menuSearchTextbox.Text = "";
            m_menuSearchTextbox.TextChanged += m_menuSearchTextbox_TextChanged;
            m_toolbarPanel.Controls.Add(m_menuSearchTextbox);

            m_menuPathLabel = new MetroLabel();
            MetroSkinManager.ApplyMetroStyle(m_menuPathLabel);
            m_menuPathLabel.Left = m_menuSearchTextbox.Right + DEFAULT_SEPARATOR;
            m_menuPathLabel.Width = 0;
            m_menuPathLabel.Height = 0;
            m_menuPathLabel.Text = "";
            m_menuPathLabel.AutoSize = true;
            m_toolbarPanel.Controls.Add(m_menuPathLabel);

            m_filesListPanel = new MetroPanel();
            MetroSkinManager.ApplyMetroStyle(m_filesListPanel);
            m_filesListPanel.Dock = DockStyle.Fill;

            m_filesListScrollbar = new MetroScrollBar(MetroScrollOrientation.Horizontal);
            MetroSkinManager.ApplyMetroStyle(m_filesListScrollbar);
            m_filesListScrollbar.Dock = DockStyle.Bottom;
            m_filesListScrollbar.Scroll += m_filesListScrollbar_Scroll;

            Controls.Add(m_filesListPanel);
            Controls.Add(m_toolbarPanel);
            Controls.Add(m_filesListScrollbar);

            UpdateScrollbar();
        }

        #endregion



        #region Public functionality.

        public void RefreshContent()
        {
            m_filesListPanel.Controls.Clear();

            MainForm mainForm = FindForm() as MainForm;
            if (mainForm == null || mainForm.ProjectModel == null)
                return;

            MetroTileIcon tile;
            bool upDownRow = false;
            int x = 0;
            string assetsPath = Path.Combine(mainForm.ProjectModel.WorkingDirectory, "assets");
            if (m_viewPath == null)
                m_viewPath = assetsPath;
            m_menuPathLabel.Text = m_viewPath;

            DirectoryInfo info = new DirectoryInfo(m_viewPath);
            if (!info.Exists)
                return;

            if (m_viewPath != assetsPath)
            {
                tile = new MetroTileIcon();
                MetroSkinManager.ApplyMetroStyle(tile);
                tile.Tag = info.FullName + @"\..";
                tile.Top = DEFAULT_SEPARATOR;
                tile.Left = x;
                tile.Size = DEFAULT_TILE_SIZE;
                tile.Image = m_backImage;
                tile.IsImageScaled = true;
                tile.ImageScale = new PointF(0.85f, 0.85f);
                tile.MouseUp += tile_MouseUp;
                m_filesListPanel.Controls.Add(tile);
                upDownRow = !upDownRow;
            }
            DirectoryInfo[] dirs = info.GetDirectories();
            foreach (var dirInfo in dirs)
            {
                tile = new MetroTileIcon();
                MetroSkinManager.ApplyMetroStyle(tile);
                tile.Tag = dirInfo.FullName;
                tile.Top = upDownRow ? (DEFAULT_SEPARATOR + DEFAULT_TILE_SIZE.Height + DEFAULT_SEPARATOR) : DEFAULT_SEPARATOR;
                tile.Left = x;
                tile.Size = DEFAULT_TILE_SIZE;
                tile.Text = dirInfo.Name;
                tile.TextAlign = ContentAlignment.BottomRight;
                tile.TileTextFontSize = MetroTileTextSize.Small;
                tile.TileTextFontWeight = MetroTileTextWeight.Regular;
                tile.Image = m_dirImage;
                tile.IsImageScaled = true;
                tile.ImageScale = new PointF(0.85f, 0.85f);
                tile.ImageAlign = ContentAlignment.TopLeft;
                tile.ImageOffset = new Point(-10, -10);
                tile.MouseUp += tile_MouseUp;
                m_filesListPanel.Controls.Add(tile);
                upDownRow = !upDownRow;
                if (!upDownRow)
                    x += DEFAULT_TILE_SIZE.Width + DEFAULT_SEPARATOR;
            }
            string ext;
            foreach (var fileInfo in info.GetFiles())
            {
                ext = Path.GetExtension(fileInfo.Name);
                tile = new MetroTileIcon();
                MetroSkinManager.ApplyMetroStyle(tile);
                tile.Tag = fileInfo.FullName;
                tile.Top = upDownRow ? (DEFAULT_SEPARATOR + DEFAULT_TILE_SIZE.Height + DEFAULT_SEPARATOR) : DEFAULT_SEPARATOR;
                tile.Left = x;
                tile.Size = DEFAULT_TILE_SIZE;
                tile.Text = ext + "\n" + Path.GetFileNameWithoutExtension(fileInfo.Name);
                tile.TextAlign = ContentAlignment.BottomRight;
                tile.TileTextFontSize = MetroTileTextSize.Small;
                tile.TileTextFontWeight = MetroTileTextWeight.Regular;
                if (ext == ".js")
                    tile.Image = m_fileCodeImage;
                else if (ext == ".png" || ext == ".jpg" || ext == ".jpeg")
                    tile.Image = m_fileImageImage;
                else if (ext == ".ogg" || ext == ".mp3")
                    tile.Image = m_fileMusicImage;
                else if (ext == ".txt")
                    tile.Image = m_fileTextImage;
                else if (ext == ".json" || ext == ".xml" || ext == ".js")
                    tile.Image = m_fileDataImage;
                else
                    tile.Image = m_fileImage;
                tile.IsImageScaled = true;
                tile.ImageScale = new PointF(0.85f, 0.85f);
                tile.ImageAlign = ContentAlignment.TopLeft;
                tile.ImageOffset = new Point(-10, -10);
                tile.MouseUp += tile_MouseUp;
                m_filesListPanel.Controls.Add(tile);
                upDownRow = !upDownRow;
                if (!upDownRow)
                    x += DEFAULT_TILE_SIZE.Width + DEFAULT_SEPARATOR;
            }

            UpdateScrollbar();
        }

        public void SetViewPath(string path)
        {
            m_viewPath = Path.GetFullPath(path);
            RefreshContent();
        }

        public void UpdateScrollbar()
        {
            Rectangle rect;
            m_filesListPanel.CalculateContentsRectangle(out rect);
            m_filesListScrollbar.Maximum = rect.Width;
            m_filesListScrollbar.LargeChange = m_filesListPanel.Width;
            m_filesListScrollbar.Visible = true;
            m_filesListPanel.HorizontalScroll.Maximum = m_filesListScrollbar.Maximum;
            m_filesListPanel.HorizontalScroll.LargeChange = m_filesListScrollbar.LargeChange;
            m_filesListPanel.HorizontalScroll.Value = Math.Min(m_filesListScrollbar.Value, m_filesListPanel.HorizontalScroll.Maximum);
        }

        #endregion



        #region Private event handlers.

        private void SceneViewAssetsControl_Resize(object sender, EventArgs e)
        {
            UpdateScrollbar();
        }

        private void m_menuAddTile_MouseUp(object sender, EventArgs e)
        {
            MetroTileIcon tile = sender as MetroTileIcon;
            if (tile == null)
                return;

            MetroContextMenu menu = new MetroContextMenu(null);
            MetroSkinManager.ApplyMetroStyle(menu);
            ToolStripMenuItem item;
            string name;
            string[] types = new string[] {
                "Import",
                "Animation",
                "Cubemap",
                "Css",
                "Json",
                "Html",
                "Material",
                "Script",
                "Text"
            };

            for (int i = 0; i < types.Length; ++i)
            {
                name = types[i];
                item = new ToolStripMenuItem(name);
                item.Tag = name;
                item.Click += addAsset_Click;
                menu.Items.Add(item);
            }

            menu.Show(tile, new Point(0, tile.Height));
        }

        private void addAsset_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem item = sender as ToolStripMenuItem;
            if (item == null || !(item.Tag is string))
                return;

            MainForm mainForm = FindForm() as MainForm;
            if (mainForm == null || mainForm.ProjectModel == null)
                return;

            string assetPath = Path.Combine(mainForm.ProjectModel.WorkingDirectory, "assets");
            string name = item.Tag as string;
            if (name == "Import")
            {
                string filter = "All known files|";
                for (int i = 1; i < ASSET_TYPES.Length; ++i)
                    filter += ASSET_FILTERS[i] + (i < ASSET_TYPES.Length - 1 ? ";" : "");
                for (int i = 1; i < ASSET_TYPES.Length; ++i)
                    filter += "|" + ASSET_TYPES[i] + "|" + ASSET_FILTERS[i];
                OpenFileDialog dialog = new OpenFileDialog();
                dialog.CheckPathExists = true;
                dialog.RestoreDirectory = true;
                dialog.Filter = filter;
                dialog.Multiselect = true;
                DialogResult result = dialog.ShowDialog();
                if (result == DialogResult.OK && dialog.FileNames != null)
                {
                    foreach (var fname in dialog.FileNames)
                    {
                        string dstFname = Path.Combine(m_viewPath, Path.GetFileName(fname));
                        if (fname == dstFname)
                            continue;
                        if (File.Exists(dstFname) && MetroMessageBox.Show(mainForm, dstFname + "\nDo you want to overwrite it?", "File already exists!", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                            continue;
                        File.Copy(fname, dstFname, true);
                    }
                    if (dialog.FileNames.Length == 1)
                        m_owner.ShowAssetProperties(Utils.GetRelativePath(dialog.FileNames[0], assetPath + Path.DirectorySeparatorChar));
                    RefreshContent();
                }
            }
            else
            {
                int idx = Array.IndexOf<string>(ASSET_TYPES, name);
                if (idx >= 0)
                {
                    string ext = CREATE_ASSET_EXTENSIONS[idx];
                    if (!String.IsNullOrEmpty(ext))
                    {
                        MetroPromptBox prompt = new MetroPromptBox();
                        prompt.Title = "Create asset";
                        prompt.Message = "File name:";
                        prompt.Value = name;
                        DialogResult result = prompt.ShowDialog();
                        if (result == DialogResult.OK)
                        {
                            string path = Path.Combine(m_viewPath, prompt.Value + "." + ext);
                            if (!File.Exists(path) || MetroMessageBox.Show(mainForm, path + "\nDo you want to overwrite it?", "File already exists!", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                            {
                                Directory.CreateDirectory(Path.GetDirectoryName(path));
                                using (File.AppendText(path)) { }
                                RefreshContent();
                                m_owner.ShowAssetProperties(Utils.GetRelativePath(path, assetPath + Path.DirectorySeparatorChar));
                            }
                        }
                    }
                }
            }
        }

        private void m_menuTypeCombobox_SelectedValueChanged(object sender, EventArgs e)
        {
            RefreshContent();
        }

        private void m_menuSearchTextbox_TextChanged(object sender, EventArgs e)
        {
            RefreshContent();
        }

        private void m_filesListScrollbar_Scroll(object sender, ScrollEventArgs e)
        {
            m_filesListPanel.HorizontalScroll.Value = Math.Min(e.NewValue, m_filesListPanel.HorizontalScroll.Maximum);
        }

        private void tile_MouseUp(object sender, MouseEventArgs e)
        {
            MetroTileIcon tile = sender as MetroTileIcon;
            if (tile == null)
                return;
            MainForm mainForm = FindForm() as MainForm;
            if (mainForm == null || mainForm.ProjectModel == null)
                return;

            string assetPath = Path.Combine(mainForm.ProjectModel.WorkingDirectory, "assets");
            string path = tile.Tag as string;
            if (e.Button == MouseButtons.Left)
            {
                if (Directory.Exists(path))
                    SetViewPath(path);
                else if (File.Exists(path))
                    m_owner.ShowAssetProperties(Utils.GetRelativePath(path, assetPath + Path.DirectorySeparatorChar));//OpenFile(path);
            }
            else if (e.Button == MouseButtons.Right)
            {
                MetroContextMenu menu = new MetroContextMenu(null);
                MetroSkinManager.ApplyMetroStyle(menu);
                ToolStripMenuItem menuItem;

                menuItem = new ToolStripMenuItem("Rename");
                menuItem.Tag = path;
                menuItem.Click += menuItem_rename_Click;
                menu.Items.Add(menuItem);

                menuItem = new ToolStripMenuItem("Delete");
                menuItem.Tag = path;
                menuItem.Click += menuItem_delete_Click;
                menu.Items.Add(menuItem);

                menu.Show(tile, e.Location);
            }
        }

        private void menuItem_rename_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem menuItem = sender as ToolStripMenuItem;
            if (menuItem == null || !(menuItem.Tag is string))
                return;

            string path = menuItem.Tag as string;
            if (Directory.Exists(path))
            {
                DirectoryInfo info = new DirectoryInfo(path);
                if (!info.Exists)
                    return;

                MetroPromptBox dialog = new MetroPromptBox();
                dialog.Title = "Rename Directory";
                dialog.Message = "Type new directory name:";
                dialog.Value = info.Name;
                DialogResult result = dialog.ShowDialog();
                if (result == DialogResult.OK)
                    info.MoveTo(info.Parent.FullName + @"\" + dialog.Value);
            }
            else if (File.Exists(path))
            {
                FileInfo info = new FileInfo(path);
                if (!info.Exists)
                    return;

                MetroPromptBox dialog = new MetroPromptBox();
                dialog.Title = "Rename File";
                dialog.Message = "Type new file name:";
                dialog.Value = info.Name;
                DialogResult result = dialog.ShowDialog();
                if (result == DialogResult.OK)
                    info.MoveTo(info.Directory.FullName + @"\" + dialog.Value);
            }

            RefreshContent();
        }

        private void menuItem_delete_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem menuItem = sender as ToolStripMenuItem;
            if (menuItem == null || !(menuItem.Tag is string))
                return;

            string path = menuItem.Tag as string;
            if (Directory.Exists(path))
            {
                DirectoryInfo info = new DirectoryInfo(path);
                if (!info.Exists)
                    return;

                DialogResult result = MetroMessageBox.Show(FindForm(), info.FullName, "Are you sure to delete directory?", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                    info.Delete(true);
            }
            else if (File.Exists(path))
            {
                FileInfo info = new FileInfo(path);
                if (!info.Exists)
                    return;

                DialogResult result = MetroMessageBox.Show(FindForm(), info.FullName, "Are you sure to delete file?", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                    info.Delete();
            }

            RefreshContent();
        }

        #endregion
    }
}
