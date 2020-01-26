using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
using BencodeNET.Objects;
using BencodeNET.Parsing;
using BencodeNET.Torrents;
using Ionic.Zip;
using System.Collections.Concurrent;

namespace TorrentCollector
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

        string[] hashes = null;
        string torrentFolder = null;

        private void BtnSelectTextFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if(ofd.ShowDialog() == true)
            {
                hashes = File.ReadAllLines(ofd.FileName);
                btnSelectTextFile.Background = Brushes.Green;
            }

        }

        private void BtnSelectTorrentFolder_Click(object sender, RoutedEventArgs e)
        {
            var fbd = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog();
            bool? result = fbd.ShowDialog();

            if (result == true && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
            {
                torrentFolder = fbd.SelectedPath;
                btnSelectTorrentFolder.Background = Brushes.Green;
            }
            if(hashes != null && torrentFolder != null)
            {
                btnZipIt.IsEnabled = true;
            }
        }
            

        private void BtnZipIt_Click(object sender, RoutedEventArgs e)
        {
            string[] torrentFiles = Directory.GetFiles(torrentFolder);

            var parser = new BencodeParser(); // Default encoding is Encoding.UTF8, but you can specify another if you need to

            ConcurrentBag<string> matchingTorrents = new ConcurrentBag<string>();

            //foreach (string torrentFile in torrentFiles)
            Parallel.ForEach(torrentFiles,
                new ParallelOptions { }, (torrentFile, loopState) =>
                // foreach (string srcFileName in filesInSourceFolder)
                {
                    Torrent torrent;
                    // Parse torrent by specifying the file path
                    try
                    {

                        torrent = parser.Parse<Torrent>(torrentFile);
                    }
                    catch (Exception)
                    {
                        return;
                    }

                    // Calculate the info hash
                    string infoHash = torrent.GetInfoHash();

                    int index = Array.FindIndex(hashes, t => t.Trim().Equals(infoHash, StringComparison.InvariantCultureIgnoreCase));

                    if (index != -1)
                    {
                        matchingTorrents.Add(torrentFile);
                    }
                });

            if(matchingTorrents.Count == 0)
            {
                MessageBox.Show("No matching torrents found.");
            } else
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "Zip file (.zip)|*.zip";
                sfd.Title = "Where to save .zip file of matching torrents?"; 
                if(sfd.ShowDialog() == true)
                {
                    using (ZipFile zip = new ZipFile())
                    {
                        foreach(string matchingTorrentFile in matchingTorrents)
                        {
                            zip.AddFile(matchingTorrentFile,"");
                        }
                        zip.Save(sfd.FileName);
                    }
                }
            }
        }
    }
}
