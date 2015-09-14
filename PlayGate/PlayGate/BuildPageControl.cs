using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using MetroFramework.Controls;
using System.Diagnostics;
using MetroFramework;
using System.Windows.Forms;
using System.IO;

namespace PlayGate
{
    public class BuildPageControl : MetroPanel
    {
        #region Public enumerators.

        public enum TargetMode
        {
            Debug,
            Release
        }

        #endregion



        #region Private static data.

        private static readonly Size DEFAULT_TILE_SIZE = new Size(128, 128);
        private static readonly Point DEFAULT_TILE_SEPARATOR = new Point(8, 8);

        #endregion



        #region Private data.

        private Process m_runningProcess;
        private MetroTileIcon m_buildDebugTile;
        private MetroTileIcon m_buildAndRunDebugTile;
        private MetroTileIcon m_runDebugTile;
        private MetroTileIcon m_cleanDebugTile;
        private MetroTileIcon m_buildReleaseTile;
        private MetroTileIcon m_buildAndRunReleaseTile;
        private MetroTileIcon m_cleanReleaseTile;
        private MetroTileIcon m_runReleaseTile;
        private Queue<Action> m_afterBatchBuildQueue = new Queue<Action>();
        private Process m_httpServer;

        #endregion



        #region Public properties.

        public bool IsBatchProcessRunning { get { return m_runningProcess != null; } }

        #endregion



        #region Construction and destruction.

        public BuildPageControl()
        {
            MetroSkinManager.ApplyMetroStyle(this);
            AutoScroll = true;
            Disposed += BuildPageControl_Disposed;

            m_buildDebugTile = new MetroTileIcon();
            MetroSkinManager.ApplyMetroStyle(m_buildDebugTile);
            m_buildDebugTile.Text = "DEBUG\nBUILD";
            m_buildDebugTile.Image = Bitmap.FromFile("resources/icons/appbar.cog.png");
            m_buildDebugTile.Size = DEFAULT_TILE_SIZE;
            m_buildDebugTile.Location = new Point(64, 64);
            m_buildDebugTile.Click += m_buildDebugTile_Click;
            Controls.Add(m_buildDebugTile);

            m_buildReleaseTile = new MetroTileIcon();
            MetroSkinManager.ApplyMetroStyle(m_buildReleaseTile);
            m_buildReleaseTile.Text = "RELEASE\nBUILD";
            m_buildReleaseTile.Image = Bitmap.FromFile("resources/icons/appbar.cog.png");
            m_buildReleaseTile.Size = DEFAULT_TILE_SIZE;
            m_buildReleaseTile.Location = new Point(m_buildDebugTile.Left, m_buildDebugTile.Bottom + DEFAULT_TILE_SEPARATOR.Y);
            m_buildReleaseTile.Click += m_buildReleaseTile_Click;
            Controls.Add(m_buildReleaseTile);

            m_buildAndRunDebugTile = new MetroTileIcon();
            MetroSkinManager.ApplyMetroStyle(m_buildAndRunDebugTile);
            m_buildAndRunDebugTile.Text = "DEBUG\nBUILD AND RUN";
            m_buildAndRunDebugTile.Image = Bitmap.FromFile("resources/icons/appbar.control.play.png");
            m_buildAndRunDebugTile.Size = DEFAULT_TILE_SIZE;
            m_buildAndRunDebugTile.Location = new Point(m_buildDebugTile.Right + DEFAULT_TILE_SEPARATOR.X, m_buildDebugTile.Top);
            m_buildAndRunDebugTile.Click += m_buildAndRunDebugTile_Click;
            Controls.Add(m_buildAndRunDebugTile);

            m_buildAndRunReleaseTile = new MetroTileIcon();
            MetroSkinManager.ApplyMetroStyle(m_buildAndRunReleaseTile);
            m_buildAndRunReleaseTile.Text = "RELEASE\nBUILD AND RUN";
            m_buildAndRunReleaseTile.Image = Bitmap.FromFile("resources/icons/appbar.control.play.png");
            m_buildAndRunReleaseTile.Size = DEFAULT_TILE_SIZE;
            m_buildAndRunReleaseTile.Location = new Point(m_buildReleaseTile.Right + DEFAULT_TILE_SEPARATOR.X, m_buildReleaseTile.Top);
            m_buildAndRunReleaseTile.Click += m_buildAndRunReleaseTile_Click;
            Controls.Add(m_buildAndRunReleaseTile);

            m_runDebugTile = new MetroTileIcon();
            MetroSkinManager.ApplyMetroStyle(m_runDebugTile);
            m_runDebugTile.Text = "DEBUG\nRUN";
            m_runDebugTile.Image = Bitmap.FromFile("resources/icons/appbar.control.play.png");
            m_runDebugTile.Size = DEFAULT_TILE_SIZE;
            m_runDebugTile.Location = new Point(m_buildAndRunDebugTile.Right + DEFAULT_TILE_SEPARATOR.X, m_buildAndRunDebugTile.Top);
            m_runDebugTile.Click += m_runDebugTile_Click;
            Controls.Add(m_runDebugTile);

            m_runReleaseTile = new MetroTileIcon();
            MetroSkinManager.ApplyMetroStyle(m_runReleaseTile);
            m_runReleaseTile.Text = "RELEASE\nRUN";
            m_runReleaseTile.Image = Bitmap.FromFile("resources/icons/appbar.control.play.png");
            m_runReleaseTile.Size = DEFAULT_TILE_SIZE;
            m_runReleaseTile.Location = new Point(m_buildAndRunReleaseTile.Right + DEFAULT_TILE_SEPARATOR.X, m_buildAndRunReleaseTile.Top);
            m_runReleaseTile.Click += m_runReleaseTile_Click;
            Controls.Add(m_runReleaseTile);

            m_cleanDebugTile = new MetroTileIcon();
            MetroSkinManager.ApplyMetroStyle(m_cleanDebugTile);
            m_cleanDebugTile.Text = "DEBUG\nCLEAN";
            m_cleanDebugTile.Image = Bitmap.FromFile("resources/icons/appbar.delete.png");
            m_cleanDebugTile.Size = DEFAULT_TILE_SIZE;
            m_cleanDebugTile.Location = new Point(m_runDebugTile.Right + DEFAULT_TILE_SEPARATOR.X, m_runDebugTile.Top);
            m_cleanDebugTile.Click += m_cleanDebugTile_Click;
            Controls.Add(m_cleanDebugTile);

            m_cleanReleaseTile = new MetroTileIcon();
            MetroSkinManager.ApplyMetroStyle(m_cleanReleaseTile);
            m_cleanReleaseTile.Text = "RELEASE\nCLEAN";
            m_cleanReleaseTile.Image = Bitmap.FromFile("resources/icons/appbar.delete.png");
            m_cleanReleaseTile.Size = DEFAULT_TILE_SIZE;
            m_cleanReleaseTile.Location = new Point(m_runReleaseTile.Right + DEFAULT_TILE_SEPARATOR.X, m_runReleaseTile.Top);
            m_cleanReleaseTile.Click += m_cleanReleaseTile_Click;
            Controls.Add(m_cleanReleaseTile);
        }

        #endregion



        #region Public functionality.

        public void StartServer()
        {
            if (m_httpServer != null)
            {
                try
                {
                    m_httpServer.Kill();
                }
                catch (Exception ex) { Console.Error.WriteLine(ex.ToString()); }
                m_httpServer = null;
            }

            MainForm mainForm = FindForm() as MainForm;
            if (mainForm == null || mainForm.SettingsModel == null || mainForm.ProjectModel == null || !mainForm.SettingsModel.IsNodeJsExists)
                return;

            string workdir = Path.Combine(Path.GetFullPath(mainForm.ProjectModel.WorkingDirectory), "bin");
            if (!Directory.Exists(workdir))
            {
                MetroMessageBox.Show(mainForm, "Cannot run HTTP server because binary folder does not exists!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            m_httpServer = new Process();
            ProcessStartInfo info = new ProcessStartInfo();
            info.WorkingDirectory = workdir;
            info.FileName = "node";
            info.Arguments = "../lib/httpServer.js " + mainForm.SettingsModel.RunnerHttpServerPort;
            info.UseShellExecute = false;
            info.CreateNoWindow = true;
            m_httpServer.StartInfo = info;
            m_httpServer.Exited += m_httpServer_Exited;
            m_httpServer.Start();
        }

        public void BuildProject(TargetMode target, Action afterBuildAction = null)
        {
            MainForm mainForm = FindForm() as MainForm;
            if (mainForm == null || mainForm.SettingsModel == null || mainForm.ProjectModel == null || !mainForm.SettingsModel.IsNodeJsExists)
                return;

            if (m_runningProcess != null && !m_runningProcess.HasExited)
            {
                MetroMessageBox.Show(mainForm, "Cannot build " + target.ToString() + " because another operation is running!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Process proc = new Process();
            ProcessStartInfo info = new ProcessStartInfo();
            info.WorkingDirectory = Path.GetFullPath(mainForm.ProjectModel.WorkingDirectory);
            info.FileName = "node";
            info.Arguments = "build.js " + target.ToString();
            info.UseShellExecute = false;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            proc.StartInfo = info;
            proc.EnableRaisingEvents = true;
            proc.Exited += proc_Exited;
            m_runningProcess = proc;
            if (afterBuildAction != null)
            {
                lock (m_afterBatchBuildQueue)
                {
                    m_afterBatchBuildQueue.Enqueue(afterBuildAction);
                }
            }
            proc.Start();
        }

        public void RunProject(TargetMode target)
        {
            MainForm mainForm = FindForm() as MainForm;
            if (mainForm == null || mainForm.SettingsModel == null)
                return;

            Process.Start("http://localhost:" + mainForm.SettingsModel.RunnerHttpServerPort.ToString() + '/' + target.ToString());
        }

        public void CleanProject(TargetMode target)
        {
            MainForm mainForm = FindForm() as MainForm;
            if (mainForm == null || mainForm.SettingsModel == null || mainForm.ProjectModel == null)
                return;

            if (m_runningProcess != null && !m_runningProcess.HasExited)
            {
                MetroMessageBox.Show(mainForm, "Cannot clean " + target.ToString() + " " + " because another operation is running!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string path = Path.Combine(Path.GetFullPath(mainForm.ProjectModel.WorkingDirectory), "bin", target.ToString());
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
                MetroMessageBox.Show(mainForm, target.ToString() + " build cleaned!", "Operation Complete!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
                MetroMessageBox.Show(mainForm, target.ToString() + " build does not exists!", "Operation Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        #endregion



        #region Private events handlers.

        private void BuildPageControl_Disposed(object sender, EventArgs e)
        {
            if (m_httpServer != null)
            {
                try
                {
                    m_httpServer.Kill();
                }
                catch (Exception ex) { Console.Error.WriteLine(ex.ToString()); }
                m_httpServer = null;
            }
        }

        private void m_httpServer_Exited(object sender, EventArgs e)
        {
            m_httpServer = null;
        }

        private void m_buildDebugTile_Click(object sender, EventArgs e)
        {
            BuildProject(TargetMode.Debug);
        }

        private void m_buildReleaseTile_Click(object sender, EventArgs e)
        {
            BuildProject(TargetMode.Release);
        }

        private void m_buildAndRunDebugTile_Click(object sender, EventArgs e)
        {
            BuildProject(TargetMode.Debug, () => RunProject(TargetMode.Debug));
        }

        private void m_buildAndRunReleaseTile_Click(object sender, EventArgs e)
        {
            BuildProject(TargetMode.Release, () => RunProject(TargetMode.Release));
        }

        private void m_runDebugTile_Click(object sender, EventArgs e)
        {
            RunProject(TargetMode.Debug);
        }

        private void m_runReleaseTile_Click(object sender, EventArgs e)
        {
            RunProject(TargetMode.Release);
        }

        private void m_cleanDebugTile_Click(object sender, EventArgs e)
        {
            CleanProject(TargetMode.Debug);
        }

        private void m_cleanReleaseTile_Click(object sender, EventArgs e)
        {
            CleanProject(TargetMode.Release);
        }

        private void proc_Exited(object sender, EventArgs e)
        {
            MainForm mainForm = FindForm() as MainForm;
            if (m_runningProcess != null)
            {
                string log = m_runningProcess.StandardOutput.ReadToEnd();
                if (!String.IsNullOrEmpty(log))
                {
                    Console.Out.WriteLine(log);
                    if (mainForm != null)
                        MetroMessageBox.Show(mainForm, log, "Operation Complete!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    log = m_runningProcess.StandardError.ReadToEnd();
                    if (!String.IsNullOrEmpty(log))
                    {
                        Console.Error.WriteLine(log);
                        if (mainForm != null)
                            MetroMessageBox.Show(mainForm, log, "Operation Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }

            lock (m_afterBatchBuildQueue)
            {
                if (m_afterBatchBuildQueue.Count > 0)
                {
                    Action action = m_afterBatchBuildQueue.Dequeue();
                    if (m_runningProcess.ExitCode == 0 && action != null)
                        action();
                }
            }

            m_runningProcess = null;
        }

        #endregion
    }
}
