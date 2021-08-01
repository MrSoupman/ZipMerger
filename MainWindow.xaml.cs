using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
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
using System.Text.RegularExpressions;
using System.Threading;

namespace ZipMerger
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ObservableCollection<string> Zips { get; } = new ObservableCollection<string>();
        public string NewZipDir, NewName;
        string TempDir = Directory.GetCurrentDirectory() + "\\temp";
        string fmt = "0000"; //format for numbers
        public void AddZip(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "Zip file (*.zip) | *.zip"
            };
            if (dialog.ShowDialog() == true)
            {
                foreach (string file in dialog.FileNames)
                {
                    Zips.Add(file);
                }
                newName.Text = Path.GetFileName(Zips[0]);
                totalZip.Text = Zips.Count.ToString();
            }
        }
        public void MoveUp(object sender, RoutedEventArgs e)
        {
            int index = fileView.SelectedIndex;
            int selSize = fileView.SelectedItems.Count;
            if (index > 0)
                Zips.Move(index - 1, index + selSize - 1);
        }
        public void MoveDown(object sender, RoutedEventArgs e)
        {
            int index = fileView.SelectedIndex;
            if (index != -1 && index < Zips.Count - 1)
                Zips.Move(index, index + 1);
        }
        public void Remove(object sender, RoutedEventArgs e)
        {
            int index = fileView.SelectedIndex;
            if (index != -1)
            {
                string[] temp = new string[fileView.SelectedItems.Count];
                fileView.SelectedItems.CopyTo(temp, 0);
                foreach (string sel in temp)
                {
                    Zips.Remove(sel);
                }

            }

        }
        public void Browse(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = dialog.ShowDialog(); //WPF .netcore still appearently doesn't have a built in directory browser????? for like 8 years now??? ok
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    newZipDir.Text = dialog.SelectedPath;
                }

            }
        }
        public void Merge(object sender, RoutedEventArgs e)
        {
            if (Zips.Count > 1 && Directory.Exists(newZipDir.Text) && !newName.Text.Equals("")) //valid path, and there's more than 1 zip to merge, not a blank name
            {

                if (!newName.Text.Contains(".zip"))
                    newName.Text += ".zip";
                if (!Directory.Exists(Directory.GetCurrentDirectory() + "\\temp"))
                {
                    Directory.CreateDirectory(Directory.GetCurrentDirectory() + "\\temp");
                }

                btnMerge.IsEnabled = false;
                newZipDir.IsEnabled = false;
                newName.IsEnabled = false;
                NewName = newName.Text;
                NewZipDir = newZipDir.Text;
                currFile.Text = "0";
                currZip.Text = "0";
                totalFile.Text = "0";
                BackgroundWorker worker = new BackgroundWorker();
                worker.WorkerReportsProgress = true;
                worker.DoWork += Worker_DoWork;
                worker.ProgressChanged += Worker_ProgressChanged;
                worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
                worker.RunWorkerAsync();
            }
        }

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            btnMerge.IsEnabled = true;
            newZipDir.IsEnabled = true;
            newName.IsEnabled = true;
            Zips.Clear();
            MessageBox.Show("Finished");
        }

        private void Worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            Tuple<int, int> args = e.UserState as Tuple<int, int>; //need to know if progress is for zip or archive
            int isArchive = args.Item1; //0 == image file, 1 == an archive, -1 == initial state
            int current = args.Item2;
            if (isArchive == -1) //special case: Resets file progress bar and resets the total amount of image files
            {
                currFile.Text = "0";
                fileProgress.Value = 0;
                totalFile.Text = current.ToString();
            }
            if (isArchive == 1)
            {
                int CurrentZip = Int32.Parse(currZip.Text);
                int TotalZip = Int32.Parse(totalZip.Text);
                CurrentZip++;
                currZip.Text = CurrentZip.ToString();
                zipProgress.Value = Convert.ToInt32(((double)CurrentZip/(double)TotalZip) * 100);
            }
            else if (isArchive == 0)
            {
                currFile.Text = current.ToString();
                fileProgress.Value = e.ProgressPercentage;
            }
        }

        private int StupidParse(string str)
        {
            //This doesn't take into account any negatives or anything so don't use this for that
            StringBuilder sb = new StringBuilder(4);
            for (int i = 0; i < str.Length; i++)
            {
                if (Char.IsDigit(str[i]))
                    sb.Append(str[i]);
                else
                    break;
            }
            if (sb.Length == 0)
                return -1; //dunno what this file name is, handle it somewhere else
            else

                return Int32.Parse(sb.ToString());
        }

        private void ClearTemp()
        {
            var TempClear = Directory.EnumerateFiles(Directory.GetCurrentDirectory() + "\\temp");
            foreach (string file in TempClear)
                File.Delete(file);
        }

        private string ValidateName(string file, int intOffset)
        {
            Regex regex = new Regex(@"\d\d\d\d"); //regex to check if name is of format xxxx
            string ImgName = Path.GetFileNameWithoutExtension(file);
            string Ext = Path.GetExtension(file);
            int oneSet = 0;
            if (ImgName.Contains("-") || ImgName.Contains("_"))
            {
                int index = ImgName.IndexOf(" -");
                if (index == -1) //some files have it as x-yyyyy.jpg, have to deal with that
                {
                    index = ImgName.IndexOf("-");
                    if (index == -1)
                    {
                        index = ImgName.IndexOf("_"); //occurs when file doesn't have a '-', either because there is some other random file in it like a readme.txt, or the archive is clean
                        if (index != -1)
                            oneSet++;
                    }
                }
            }
            if (!regex.IsMatch(ImgName))
            {
                int PgNum = StupidParse(ImgName);
                if (PgNum == -1) //Something happened, idk
                    return file;
                else
                    PgNum += oneSet + intOffset;
                ImgName = PgNum.ToString(fmt);
                File.Move(file, TempDir + "\\" + ImgName + Ext);
            }
            return TempDir + "\\" + ImgName + Ext;
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            //1. Extract First zip to temp
            //2. Check valid names and count how many into Offset
            //3. If not, rename, else go to step 4
            //4. Createzipfromdirectory(temp) -> destination
            //5. Clean temp folder
            //6. For loop remaining zip files
            //7. Extract to temp -> Rename adding offset -> Add entry to main zip -> clear temp
            //Finish
            
            int Offset = 0; //offset from previous zip files
            int count = 0; //count of current files tracked


            if (!Directory.Exists(TempDir)) //creates a temp dir where this program exists if it doesn't already
                Directory.CreateDirectory(TempDir);

            //Extracts inital archive, renames, and zips
            using (ZipArchive ZipInit = ZipFile.OpenRead(Zips[0]))
            {
                ClearTemp(); //if something happened last run, we clear temp just so we don't get exception
                (sender as BackgroundWorker).ReportProgress(0, new Tuple<int, int>(-1, ZipInit.Entries.Count)); //Sends the amount of total files in the archive

                ZipInit.ExtractToDirectory(TempDir);
                var TempEnum = Directory.EnumerateFiles(TempDir);
                foreach (string file in TempEnum)
                {
                    ValidateName(file, 0); //Dont need offset here, this is initial zip
                    count++;
                    int progress = Convert.ToInt32((double)count / (double)ZipInit.Entries.Count) * 100;
                    (sender as BackgroundWorker).ReportProgress(progress, new Tuple<int, int>(0, count)); //sends progress of current archive
                    Thread.Sleep(10);
                }
                Offset += TempEnum.Count();
            }
            (sender as BackgroundWorker).ReportProgress(0, new Tuple<int, int>(1, 0));
            ZipFile.CreateFromDirectory(TempDir, NewZipDir + "\\" + NewName);
            ClearTemp();

            //Go through rest of zips
            for (int i = 1; i < Zips.Count; i++)
            {
                using (ZipArchive archive = ZipFile.OpenRead(Zips[i]))
                {
                    (sender as BackgroundWorker).ReportProgress(0, new Tuple<int, int>(-1, archive.Entries.Count)); //Sends the amount of total files in the archive
                    count = 0; //count of current files tracked
                    archive.ExtractToDirectory(TempDir);
                    var TempEnum = Directory.EnumerateFiles(TempDir);
                    foreach (string file in TempEnum)
                    {
                        string newName = ValidateName(file, Offset);
                        using (ZipArchive zip = ZipFile.Open(NewZipDir + "\\" + NewName, ZipArchiveMode.Update))
                        {
                            //In the case that we have files that do meet format, adding an offset may cause an issue 
                            //since a file with that name already exists, so we change the file name upon adding it to the new archive
                            int temp = StupidParse(Path.GetFileName(newName)) + Offset;
                            string finalName = temp.ToString(fmt) + Path.GetExtension(newName);
                            zip.CreateEntryFromFile(newName, finalName);
                        }
                        count++;
                        int progress = Convert.ToInt32((double)count / (double)archive.Entries.Count) * 100;
                        (sender as BackgroundWorker).ReportProgress(progress, new Tuple<int, int>(0, count)); //sends progress of current archive
                        Thread.Sleep(30);
                    }
                    Offset += archive.Entries.Count;
                }
                (sender as BackgroundWorker).ReportProgress(0, new Tuple<int, int>(1, 0));
                ClearTemp();
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
        }
    }
}
