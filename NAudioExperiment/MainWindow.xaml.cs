using NAudio.Lame;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SPath = System.IO.Path;

namespace NAudioExperiment
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            string fileDir = AppContext.BaseDirectory;
            var inPath = SPath.Combine(fileDir, "了解真相快三退.wav");
            var semitone = Math.Pow(2, 1.0 / 12);
            var upOneTone = semitone * semitone * semitone * semitone * semitone;
            var downOneTone = 1.0 / (upOneTone);
            var tfile = TagLib.File.Create(inPath);
            using (var reader = new MediaFoundationReader(inPath))
            {
                var pitch = new SmbPitchShiftingSampleProvider(reader.ToSampleProvider());
                pitch.PitchFactor = (float)(((double)new Random().Next(60, 115)) / 100);
                string tempFileNameGuid = Guid.NewGuid().ToString();
                string tempFilePath = SPath.Combine(fileDir, tempFileNameGuid + ".wav");
                string outputFileNameGuid = Guid.NewGuid().ToString();
                string outWavFilePath = SPath.Combine(fileDir, outputFileNameGuid + ".wav");
                string outMp3FilePath = SPath.Combine(fileDir, outputFileNameGuid + ".mp3");
                WaveFileWriter.CreateWaveFile(tempFilePath, pitch.ToWaveProvider());
                ConvertWavToMp3(tempFilePath, outMp3FilePath, tfile.Properties.AudioBitrate);
                //playSampleProvider(pitch);
                using (var mp3Reader = new Mp3FileReader(outMp3FilePath))
                using (var conversionStream = new WaveFormatConversionStream(reader.WaveFormat, mp3Reader))
                    WaveFileWriter.CreateWaveFile(outWavFilePath, conversionStream);
                if (File.Exists(tempFilePath)) File.Delete(tempFilePath);
            }
        }

        private void playSampleProvider(ISampleProvider sp)
        {
            using (var device = new WaveOutEvent())
            {
                device.Init(sp);
                device.Play();
                while (device.PlaybackState == PlaybackState.Playing)
                {
                    Thread.Sleep(500);
                }
            }
        }
        public string ConvertWavToMp3(string wavFilePath, string mp3FilePath, int bitRate)
        {
            try
            {
                byte[] wavFile = File.ReadAllBytes(wavFilePath);
                using (var retMs = new MemoryStream())
                using (var ms = new MemoryStream(wavFile))
                using (var rdr = new WaveFileReader(ms))
                using (var wtr = new LameMP3FileWriter(retMs, rdr.WaveFormat, bitRate))
                {
                    rdr.CopyTo(wtr);
                    wtr.Flush();
                    File.WriteAllBytes(mp3FilePath, retMs.ToArray());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ConvertWavToMp3", ex);
            }
            return mp3FilePath;
        }
    }
}
