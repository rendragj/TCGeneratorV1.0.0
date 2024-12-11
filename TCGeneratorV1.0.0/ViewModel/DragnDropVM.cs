using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using System.Reflection;

namespace TCGeneratorV1._0._0.ViewModel
{

    public class DragnDropVM : INotifyPropertyChanged
    {
        private ObservableCollection<FileItem> files;
        private ObservableCollection<FileItem> masterFiles;
        private bool isProcessing;
        private double progress;
        private bool isFileReady;
        private string progressMessage;
        private string outputFilePath;

        public ObservableCollection<FileItem> Files
        {
            get => files;
            set
            {
                files = value;
                OnPropertyChanged();
                RaiseCanExecuteChanged();
            }
        }

        public ObservableCollection<FileItem> MasterFiles
        {
            get => masterFiles;
            set
            {
                masterFiles = value;
                OnPropertyChanged();
                RaiseCanExecuteChanged();
            }
        }

        public bool IsProcessing
        {
            get => isProcessing;
            set
            {
                isProcessing = value;
                OnPropertyChanged();
            }
        }

        public double Progress
        {
            get => progress;
            set
            {
                progress = value;
                OnPropertyChanged();
            }
        }

        public bool IsFileReady
        {
            get => isFileReady;
            set
            {
                isFileReady = value;
                OnPropertyChanged();
            }
        }

        public string ProgressMessage
        {
            get => progressMessage;
            set
            {
                progressMessage = value;
                OnPropertyChanged();
            }
        }

        public ICommand AddFileCommand { get; private set; }
        public ICommand TransformDataCommand { get; private set; }
        public ICommand DragOverCommand { get; private set; }
        public ICommand DropCommand { get; private set; }
        public ICommand MasterAddFileCommand { get; private set; }
        public ICommand MasterDragOverCommand { get; private set; }
        public ICommand MasterDropCommand { get; private set; }
        public ICommand DeleteFileCommand { get; private set; }
        public ICommand DeleteMasterFileCommand { get; private set; }
        public ICommand DownloadFileCommand { get; private set; }

        public DragnDropVM()
        {
            Files = new ObservableCollection<FileItem>();
            MasterFiles = new ObservableCollection<FileItem>();

            AddFileCommand = new RelayCommand(AddFile);
            TransformDataCommand = new RelayCommand(async () => await TransformData(), CanTransformData);
            DragOverCommand = new RelayCommand<DragEventArgs>(OnDragOver);
            DropCommand = new RelayCommand<DragEventArgs>(OnDrop);
            MasterAddFileCommand = new RelayCommand(AddMasterFile);
            MasterDragOverCommand = new RelayCommand<DragEventArgs>(OnMasterDragOver);
            MasterDropCommand = new RelayCommand<DragEventArgs>(OnMasterDrop);
            DeleteFileCommand = new RelayCommand<FileItem>(DeleteFile);
            DeleteMasterFileCommand = new RelayCommand<FileItem>(DeleteMasterFile);
            DownloadFileCommand = new RelayCommand(DownloadFile);

            Files.CollectionChanged += (s, e) => RaiseCanExecuteChanged();
            MasterFiles.CollectionChanged += (s, e) => RaiseCanExecuteChanged();
        }

        private void AddFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Excel and CSV Files (*.xlsx;*.csv)|*.xlsx;*.csv|Excel Files (*.xlsx)|*.xlsx|CSV Files (*.csv)|*.csv";
            if (openFileDialog.ShowDialog() == true)
            {
                Files.Add(new FileItem { FilePath = openFileDialog.FileName });
                Console.WriteLine($"File added: {openFileDialog.FileName}");
                RaiseCanExecuteChanged();
            }
        }

        private void AddMasterFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Excel and CSV Files (*.xlsx;*.csv)|*.xlsx;*.csv|Excel Files (*.xlsx)|*.xlsx|CSV Files (*.csv)|*.csv";
            if (openFileDialog.ShowDialog() == true)
            {
                MasterFiles.Clear();
                MasterFiles.Add(new FileItem { FilePath = openFileDialog.FileName });
                Console.WriteLine($"Master file added: {openFileDialog.FileName}");
                RaiseCanExecuteChanged();
            }
        }

        private string GetEmbeddedPythonScript()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "TCGeneratorV1._0._0.Scripts.pytransform.py"; // Adjust this to the actual namespace and resource name

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
        private async Task TransformData()
        {
            if (Files.Count == 0 || MasterFiles.Count == 0)
            {
                MessageBox.Show("Please add both files before transforming.");
                return;
            }

            var addFilePath = Files.First().FilePath;
            var masterFilePath = MasterFiles.First().FilePath;
            outputFilePath = Path.Combine(Path.GetDirectoryName(addFilePath), "output.xlsx");

            IsProcessing = true;
            Progress = 0;
            ProgressMessage = "Starting transformation...";

            try
            {
                await Task.Run(() =>
                {
                    // Get the Python script from the embedded resource
                    var pythonScript = GetEmbeddedPythonScript();

                    // Write the script to a temporary file (if required)
                    var tempScriptPath = Path.Combine(Path.GetTempPath(), "temp_pytransform.py");
                    File.WriteAllText(tempScriptPath, pythonScript);

                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = "python",
                        Arguments = $"\"{tempScriptPath}\" \"{addFilePath}\" \"{masterFilePath}\" \"{outputFilePath}\" \"progress.txt\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using (Process process = new Process())
                    {
                        process.StartInfo = psi;
                        process.Start();

                        while (!process.HasExited)
                        {
                            UpdateProgress("progress.txt");
                        }

                        string output = process.StandardOutput.ReadToEnd();
                        string error = process.StandardError.ReadToEnd();
                        process.WaitForExit();

                        Console.WriteLine(output);
                        if (!string.IsNullOrEmpty(error))
                        {
                            Console.WriteLine($"Error: {error}");
                            throw new Exception(error);
                        }

                        if (process.ExitCode != 0)
                        {
                            throw new Exception($"Python script exited with code {process.ExitCode}");
                        }
                    }
                });

                if (File.Exists(outputFilePath))
                {
                    IsFileReady = true;
                    Console.WriteLine($"Output file created at: {outputFilePath}");
                }
                else
                {
                    IsFileReady = false;
                    MessageBox.Show("The output file was not created.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                MessageBox.Show($"Error during transformation: {ex.Message}");
            }
            finally
            {
                IsProcessing = false;
                Progress = 100;
                ProgressMessage = "Transformation completed successfully.";
            }
        }
        private void UpdateProgress(string progressFile)
        {
            try
            {
                if (File.Exists(progressFile))
                {
                    var lines = File.ReadAllLines(progressFile);
                    if (lines.Length >= 2)
                    {
                        if (double.TryParse(lines[0], out double progress))
                        {
                            Progress = progress;
                        }
                        ProgressMessage = lines[1];
                    }
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Error reading progress file: {ex.Message}");
            }
        }

        private bool CanTransformData()
        {
            return Files.Count > 0 && MasterFiles.Count > 0;
        }

        private void OnDragOver(DragEventArgs e)
        {
            try
            {
                if (e == null)
                {
                    throw new ArgumentNullException(nameof(e), "DragEventArgs is null.");
                }

                if (e.Data == null)
                {
                    throw new ArgumentNullException(nameof(e.Data), "DragEventArgs.Data is null.");
                }

                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    e.Effects = DragDropEffects.Copy;
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                }
                e.Handled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in OnDragOver: " + ex.Message);
            }
        }

        private void OnDrop(DragEventArgs e)
        {
            try
            {
                if (e == null)
                {
                    throw new ArgumentNullException(nameof(e), "DragEventArgs is null.");
                }

                if (e.Data == null)
                {
                    throw new ArgumentNullException(nameof(e.Data), "DragEventArgs.Data is null.");
                }

                var files = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (files != null)
                {
                    foreach (var file in files)
                    {
                        Files.Add(new FileItem { FilePath = file });
                        Console.WriteLine($"File dropped: {file}");
                    }
                }
                e.Handled = true;
                RaiseCanExecuteChanged();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in OnDrop: " + ex.Message);
            }
        }

        private void OnMasterDragOver(DragEventArgs e)
        {
            try
            {
                if (e == null)
                {
                    throw new ArgumentNullException(nameof(e), "DragEventArgs is null.");
                }

                if (e.Data == null)
                {
                    throw new ArgumentNullException(nameof(e.Data), "DragEventArgs.Data is null.");
                }

                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    e.Effects = DragDropEffects.Copy;
                }
                else
                {
                    e.Effects = DragDropEffects.None;
                }
                e.Handled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in OnMasterDragOver: " + ex.Message);
            }
        }

        private void OnMasterDrop(DragEventArgs e)
        {
            try
            {
                if (e == null)
                {
                    throw new ArgumentNullException(nameof(e), "DragEventArgs is null.");
                }

                if (e.Data == null)
                {
                    throw new ArgumentNullException(nameof(e.Data), "DragEventArgs.Data is null.");
                }

                var files = e.Data.GetData(DataFormats.FileDrop) as string[];
                if (files != null && files.Length > 0)
                {
                    MasterFiles.Clear();
                    MasterFiles.Add(new FileItem { FilePath = files[0] });
                    Console.WriteLine($"Master file dropped: {files[0]}");
                }
                e.Handled = true;
                RaiseCanExecuteChanged();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in OnMasterDrop: " + ex.Message);
            }
        }

        private void DeleteFile(FileItem fileItem)
        {
            if (fileItem != null && Files.Contains(fileItem))
            {
                Files.Remove(fileItem);
                Console.WriteLine($"File deleted: {fileItem.FilePath}");
                RaiseCanExecuteChanged();
            }
        }

        private void DeleteMasterFile(FileItem fileItem)
        {
            if (fileItem != null && MasterFiles.Contains(fileItem))
            {
                MasterFiles.Remove(fileItem);
                Console.WriteLine($"Master file deleted: {fileItem.FilePath}");
                RaiseCanExecuteChanged();
            }
        }

        private void DownloadFile()
        {
            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    FileName = "TCGplayerGenerated.xlsx",
                    Filter = "Excel Files (*.xlsx)|*.xlsx|All Files (*.*)|*.*"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    string selectedPath = saveFileDialog.FileName;

                    if (File.Exists(outputFilePath))
                    {
                        File.Copy(outputFilePath, selectedPath, true);
                        Process.Start(new ProcessStartInfo(selectedPath) { UseShellExecute = true });
                    }
                    else
                    {
                        MessageBox.Show("The output file does not exist.");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error in opening the file: " + ex.Message);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void RaiseCanExecuteChanged()
        {
            if (TransformDataCommand is RelayCommand transformDataRelayCommand)
            {
                transformDataRelayCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public class FileItem
    {
        public string FilePath { get; set; }
        public string FileName => System.IO.Path.GetFileName(FilePath);
    }

}