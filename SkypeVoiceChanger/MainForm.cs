﻿// n.b. icons used are Milky Icons, courtesy of Min Tran - http://min.frexy.com/

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows.Forms;
using MetroFramework.Forms;
using SKYPE4COMLib;
using SkypeVoiceChanger.Audio;
using SkypeVoiceChanger.Effects;
using SkypeVoiceChanger.Views;

namespace SkypeVoiceChanger
{
    [Export(typeof(MainForm))]
    public partial class MainForm : MetroForm
    {
        readonly AudioPipeline audioPipeline;
        readonly List<ToolStripItem> playbackButtons;
        private readonly EffectChain effects;
        private readonly AudioPlaybackGraph audioPlaybackGraph;
        private SkypeAudioInterceptor audioInterceptor;
        private readonly ILog log;
        private ConnectionStatusPage connectionStatusPage;

        [Import]
        public ICollection<Effect> Effects { get; set; }

        public MainForm()
        {
            InitializeComponent();
            tabPageRecord.Controls.Add(new RecordingPage() { Dock = DockStyle.Fill });
            tabPageAbout.Controls.Add(new AboutPage() { Dock = DockStyle.Fill });
            connectionStatusPage = new ConnectionStatusPage() {Dock = DockStyle.Fill};
            tabPage1.Controls.Add(connectionStatusPage);
            log = connectionStatusPage.Log;
            this.effects = new EffectChain();
            audioPlaybackGraph = new AudioPlaybackGraph(effects);
            audioPipeline = new AudioPipeline(effects);
            playbackButtons = new List<ToolStripItem>();
            playbackButtons.Add(buttonPlay);
            playbackButtons.Add(buttonPause);
            playbackButtons.Add(buttonOpen);
            playbackButtons.Add(buttonStop);
            playbackButtons.Add(buttonRewind);
        }

        public void ConnectToSkype()
        {
            audioPlaybackGraph.Stop();
            DisconnectFromSkype();
            if (audioInterceptor == null)
            {
                var skype = new Skype();

                audioInterceptor = new SkypeAudioInterceptor(skype, skype, log, audioPipeline);

                audioInterceptor.SkypeStatusChanged += AudioInterceptorOnSkypeStatusChanged;
                AudioInterceptorOnSkypeStatusChanged(this, EventArgs.Empty);// get initial state set up
            }
            audioInterceptor.Attach();
        }

        public void DisconnectFromSkype()
        {
            if (audioInterceptor != null)
            {
                audioInterceptor.SkypeStatusChanged -= AudioInterceptorOnSkypeStatusChanged;
                audioInterceptor.Dispose();
                audioInterceptor = null;
            }
        }

        private void buttonOpen_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog();
            ofd.Filter = "Supported Files (*.mp3;*.wav)|*.mp3;*.wav";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                string fileName = ofd.FileName;
                audioPlaybackGraph.LoadFile(fileName);
            }
        }

        private void buttonPlay_Click(object sender, EventArgs e)
        {
            if (!audioPlaybackGraph.FileLoaded)
            {
                buttonOpen_Click(sender, e);
            }
            if (audioPlaybackGraph.FileLoaded)
            {
                audioPlaybackGraph.Play();
            }
        }

        private void buttonPause_Click(object sender, EventArgs e)
        {
            audioPlaybackGraph.Pause();
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            audioPlaybackGraph.Stop();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            audioPlaybackGraph.Dispose();
        }

        private void buttonRewind_Click(object sender, EventArgs e)
        {
            audioPlaybackGraph.Rewind();
        }

        private void buttonAddEffect_Click(object sender, EventArgs e)
        {
            var effectSelectorForm = new EffectSelectorForm(Effects);
            if (effectSelectorForm.ShowDialog(this) == DialogResult.OK)
            {
                // create a new instance of the selected effect as we may want multiple copies of one effect
                var effect = (Effect)Activator.CreateInstance(effectSelectorForm.SelectedEffect.GetType());
                effects.Add(effect);
                int index = checkedListBox1.Items.Add(effect, true);
                checkedListBox1.SelectedIndex = index;
            }
            //MessageBox.Show(String.Format("I have {0} effects", Effects.Count));
        }
        
        private void buttonRemoveEffect_Click(object sender, EventArgs e)
        {
            var selectedEffect = (Effect)checkedListBox1.SelectedItem;
            if (selectedEffect != null)
            {
                int index = checkedListBox1.SelectedIndex;
                checkedListBox1.Items.Remove(selectedEffect);
                effects.Remove(selectedEffect);
                if(index < checkedListBox1.Items.Count)
                    checkedListBox1.SelectedIndex = index;
                else
                    checkedListBox1.SelectedIndex = index-1;
            }
        }

        private void checkedListBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            var effect = (Effect)checkedListBox1.SelectedItem;
            if (effect != null)
            {
                effectPanel1.Initialize(effect);
            }
            else
            {
                effectPanel1.Clear();
            }
        }

        private void checkedListBox1_ItemCheck(object sender, ItemCheckEventArgs e)
        {            
            var effect = (Effect)checkedListBox1.Items[e.Index];
            effect.Enabled = e.NewValue == CheckState.Checked;
        }

        private void buttonMoveEffectUp_Click(object sender, EventArgs e)
        {
            var selectedEffect = (Effect)checkedListBox1.SelectedItem;
            if (selectedEffect != null)
            {
                if (effects.MoveUp(selectedEffect))
                {
                    int index = checkedListBox1.SelectedIndex;
                    MoveEffect(selectedEffect, index, index - 1);
                }                
            }
        }

        private void MoveEffect(Effect selectedEffect, int index, int newIndex)
        {
            bool isChecked = checkedListBox1.GetItemChecked(index);
            checkedListBox1.Items.Remove(selectedEffect);
            checkedListBox1.Items.Insert(newIndex, selectedEffect);
            checkedListBox1.SetItemChecked(newIndex, isChecked);
            checkedListBox1.SelectedIndex = newIndex;
        }

        private void buttonMoveEffectDown_Click(object sender, EventArgs e)
        {
            var selectedEffect = (Effect)checkedListBox1.SelectedItem;
            if (selectedEffect != null)
            {
                if (effects.MoveDown(selectedEffect))
                {
                    int index = checkedListBox1.SelectedIndex;
                    MoveEffect(selectedEffect, index, index + 1);
                }
            }
        }

        private void toolStripButtonSkype_Click(object sender, EventArgs e)
        {
            SkypeMode = !SkypeMode;
        }

        private bool SkypeMode
        {
            get
            {
                return toolStripButtonSkype.Checked;
            }
            set
            {
                if (SkypeMode != value)
                {
                    toolStripButtonSkype.Checked = value;
                    if (value)
                    {
                        ConnectToSkype();
                    }
                    else
                    {
                        DisconnectFromSkype();
                    }
                    EnablePlaybackButtons(!value);
                }            
            }
        }

        private void EnablePlaybackButtons(bool enable)
        {
            foreach (ToolStripItem button in toolStrip1.Items)
            {
                if (playbackButtons.Contains(button))
                {
                    button.Enabled = enable;
                }
            }
        }

        private void MainForm_Load(object sender, EventArgs e) {

            Properties.Settings appSettings = Properties.Settings.Default;

            ConnectToSkype();


            if (appSettings.FirstRun) {

                appSettings.FirstRun = false;
                appSettings.Save();
            }
        }

        private void AudioInterceptorOnSkypeStatusChanged(object sender, EventArgs eventArgs)
        {
            connectionStatusPage.ConnectionStatus = audioInterceptor.SkypeStatus;
        }
    }
}