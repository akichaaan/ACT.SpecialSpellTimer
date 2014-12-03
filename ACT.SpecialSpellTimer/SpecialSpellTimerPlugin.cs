﻿namespace ACT.SpecialSpellTimer
{
    using System;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Windows.Forms;

    using ACT.SpecialSpellTimer.Properties;
    using Advanced_Combat_Tracker;

    /// <summary>
    /// SpecialSpellTimer Plugin
    /// </summary>
    public class SpecialSpellTimerPlugin : IActPluginV1
    {
        /// <summary>
        /// 設定パネル
        /// </summary>
        public static ConfigPanel ConfigPanel { get; private set; }

        /// <summary>
        /// 表示切り替えボタン
        /// </summary>
        private static Button SwitchVisibleButton { get; set; }

        /// <summary>
        /// プラグインステータス表示ラベル
        /// </summary>
        private Label PluginStatusLabel
        {
            get;
            set;
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public SpecialSpellTimerPlugin()
        {
            // このDLLの配置場所とACT標準のPluginディレクトリも解決の対象にする
            AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
            {
                const string thisName = "ACT.SpecialSpellTimer.dll";

                try
                {
                    var thisDirectory = ActGlobals.oFormActMain.ActPlugins
                        .Where(x => x.pluginFile.Name.ToLower() == thisName.ToLower())
                        .Select(x => Path.GetDirectoryName(x.pluginFile.FullName))
                        .FirstOrDefault();

                    var pluginDirectory = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        @"Advanced Combat Tracker\Plugins");

                    var asm = new AssemblyName(e.Name);
                    var path1 = Path.Combine(thisDirectory, asm.Name + ".dll");
                    var path2 = Path.Combine(pluginDirectory, asm.Name + ".dll");

                    if (File.Exists(path1))
                    {
                        return Assembly.LoadFrom(path1);
                    }

                    if (File.Exists(path2))
                    {
                        return Assembly.LoadFrom(path2);
                    }
                }
                catch (Exception ex)
                {
                    ActGlobals.oFormActMain.WriteExceptionLog(
                        ex,
                        "ACT.SpecialSpellTimer Assemblyの解決で例外が発生しました");
                }

                return null;
            };
        }

        /// <summary>
        /// 初期化する
        /// </summary>
        /// <param name="pluginScreenSpace">Pluginタブ</param>
        /// <param name="pluginStatusText">Pluginステータスラベル</param>
        void IActPluginV1.InitPlugin(
            TabPage pluginScreenSpace,
            Label pluginStatusText)
        {
            try
            {
                pluginScreenSpace.Text = "SpecialSpellTimer(スペスペ)";
                this.PluginStatusLabel = pluginStatusText;

                // アップデートを確認する
                this.Update();

                // 設定Panelを追加する
                ConfigPanel = new ConfigPanel();
                pluginScreenSpace.Controls.Add(ConfigPanel);
                ConfigPanel.Size = pluginScreenSpace.Size;
                ConfigPanel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom | AnchorStyles.Right;

                // 本体を開始する
                SpellTimerCore.Default.Begin();

                this.SetSwitchVisibleButton();
                this.PluginStatusLabel.Text = "Plugin Started";
            }
            catch (Exception ex)
            {
                ActGlobals.oFormActMain.WriteExceptionLog(
                    ex,
                    "ACT.SpecialSpellTimer プラグインの初期化で例外が発生しました。");

                this.PluginStatusLabel.Text = "Plugin Initialize Error";
            }
        }

        /// <summary>
        /// 後片付けをする
        /// </summary>
        void IActPluginV1.DeInitPlugin()
        {
            try
            {
                SpellTimerCore.Default.End();
                this.RemoveSwitchVisibleButton();
                this.PluginStatusLabel.Text = "Plugin Exited";
            }
            catch (Exception ex)
            {
                ActGlobals.oFormActMain.WriteExceptionLog(
                    ex,
                    "ACT.SpecialSpellTimer プラグインの終了で例外が発生しました。");

                this.PluginStatusLabel.Text = "Plugin Exited Error";
            }
        }

        /// <summary>
        /// 表示切り替えボタンを配置する
        /// </summary>
        private void SetSwitchVisibleButton()
        {
            var changeColor = new Action<Button>((button) =>
            {
                if (Settings.Default.OverlayVisible)
                {
                    button.BackColor = Color.OrangeRed;
                    button.ForeColor = Color.WhiteSmoke;
                }
                else
                {
                    button.BackColor = SystemColors.Control;
                    button.ForeColor = Color.Black;
                }
            });

            SwitchVisibleButton = new Button();
            SwitchVisibleButton.Name = "SpecialSpellTimerSwitchVisibleButton";
            SwitchVisibleButton.Size = new Size(90, 24);
            SwitchVisibleButton.Text = "スペスペ";
            SwitchVisibleButton.TextAlign = ContentAlignment.MiddleCenter;
            SwitchVisibleButton.UseVisualStyleBackColor = true;
            SwitchVisibleButton.Click += (s, e) =>
            {
                var button = s as Button;

                Settings.Default.OverlayVisible = !Settings.Default.OverlayVisible;
                Settings.Default.Save();

                FF14PluginHelper.RefreshPlayer();
                LogBuffer.RefreshPTList();

                if (Settings.Default.OverlayVisible)
                {
                    SpellTimerCore.Default.ActivatePanels();
                    OnePointTelopController.ActivateTelops();
                }

                changeColor(s as Button);
            };

            changeColor(SwitchVisibleButton);

            ActGlobals.oFormActMain.Resize += this.oFormActMain_Resize;
            ActGlobals.oFormActMain.Controls.Add(SwitchVisibleButton);
            ActGlobals.oFormActMain.Controls.SetChildIndex(SwitchVisibleButton, 1);

            this.oFormActMain_Resize(this, null);
        }

        /// <summary>
        /// 表示切り替えボタンを除去する
        /// </summary>
        private void RemoveSwitchVisibleButton()
        {
            if (SwitchVisibleButton != null)
            {
                ActGlobals.oFormActMain.Resize -= this.oFormActMain_Resize;
                ActGlobals.oFormActMain.Controls.Remove(SwitchVisibleButton);
            }
        }

        /// <summary>
        /// ACTメインフォーム Resize
        /// </summary>
        /// <param name="sender">イベント発生元</param>
        /// <param name="e">イベント引数</param>
        private void oFormActMain_Resize(object sender, EventArgs e)
        {
            SwitchVisibleButton.Location = new Point(
                ActGlobals.oFormActMain.Width - 533,
                0);
        }

        /// <summary>
        /// アップデートを行う
        /// </summary>
        private void Update()
        {
            if ((DateTime.Now - Settings.Default.LastUpdateDateTime).TotalHours >= 6d)
            {
                var message = UpdateChecker.Update();
                if (!string.IsNullOrWhiteSpace(message))
                {
                    ActGlobals.oFormActMain.WriteExceptionLog(
                        new Exception(),
                        message);
                }

                Settings.Default.LastUpdateDateTime = DateTime.Now;
                Settings.Default.Save();
            }
        }
    }
}
