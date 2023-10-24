using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Diagnostics;
using Microsoft.Win32;
using System.IO;
using dll_injector;

namespace dll_injector_gui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private BackgroundWorker processScanner;
        private DateTime scanTimer = DateTime.Now;
        private TimeSpan preferredScanInterval = new TimeSpan(0, 0, 0, 0, 250);
        private int scanCount = 0;
        private Process selectedProcess = null;
        private Process tmpSelectedProcess = null;
        public MainWindow()
        {
            InitializeComponent();
            processScanner = new BackgroundWorker();
            processScanner.DoWork += ProcessScanner_DoWork;
            processScanner.RunWorkerCompleted += ProcessScanner_RunWorkerCompleted;
            processScanner.RunWorkerAsync();
            //listProcesses.
            listDLLs.KeyDown += ListDLLs_KeyDown;
            listProcesses.KeyDown += ListProcesses_KeyDown;
            listProcesses.MouseDoubleClick += ListProcesses_MouseDoubleClick;
            listProcesses.SelectionChanged += ListProcesses_SelectionChanged;
        }

        private void ListProcesses_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listProcesses.SelectedItem is FormattedProcess)
            {
                FormattedProcess selected = (FormattedProcess)listProcesses.SelectedItem;
                tmpSelectedProcess = selected.Process;
            }
        }

        private void ListProcesses_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Enter)
            {
                SelectProcess();
            }
        }
        private void SelectProcess()
        {
            if (listProcesses.SelectedItem is FormattedProcess)
            {
                FormattedProcess selected = (FormattedProcess)listProcesses.SelectedItem;
                selectedProcess = selected.Process;
                radioProcessPID.IsChecked = true;
                lblTarget.Text = selectedProcess.Id.ToString();
            }
        }
        private void ListProcesses_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            SelectProcess();
        }
        private void UpdateProcessInfo()
        {
            StringBuilder sb = new StringBuilder();
            Process selected = tmpSelectedProcess;
            if(selected == null && selectedProcess != null)
            {
                selected = selectedProcess;
            }
            if(selected != null)
            {
                try
                {
                    sb.AppendLine("Process Info:");
                    sb.AppendLine($"Name: {selected.ProcessName} ID: {selected.Id}");
                    sb.AppendLine($"Window Name: {selected.MainWindowTitle}");
                    sb.AppendLine($"Location: {selected.MainModule.FileName}");
                    sb.AppendLine($"Handle: {selected.Handle}");
                    sb.AppendLine($"Start Time: {selected.StartTime}");
                    sb.AppendLine($"Handle: {selected.Handle}");
                    sb.AppendLine($"Main Window Handle: {selected.MainWindowHandle}");
                    sb.AppendLine($"RAM Usage: {(selected.PrivateMemorySize64 / 1024 / 1024).ToString("###,##0.00 MB")}");
                    // sb.AppendLine($"Virtual Mem Usage: {(selected.VirtualMemorySize64 / 1024 / 1024).ToString("###,##0.00 MB")}"); // This gives corrupt (absurdly large) values.  Ref: https://github.com/dotnet/runtime/issues/22184
                    sb.AppendLine($"Threads: {selected.Threads.Count}");
                    sb.AppendLine($"Priority: {selected.PriorityClass}");
                }
                catch (Exception E)
                {
                    sb.AppendLine("Access Denied");
                }
            }
            txtProcessInfo.Text = sb.ToString();
        }
        private bool CheckReflexInject()
        {
            if(selectedProcess is Process && (bool)chkReflex.IsChecked)
            {
                DllInjector dllInjector = new DllInjector(selectedProcess);
                foreach (FormattedFileInfo fileInfo in listDLLs.Items)
                {
                    if (fileInfo.FileInfo.Exists)
                    {
                        DllInjector.InjectReturnStatus status = dllInjector.InjectDll(fileInfo.FileInfo);
                        //TODO: Display errors
                    }
                }
                // TODO: Display message, automatically close window to avoid detection.
                return true;
            }
            return false;
        }
        private void ListDLLs_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key == Key.Delete)
            {
                listDLLs.Items.Remove(listDLLs.SelectedItem);
            }
        }
        ProcessComparer processComparer = new ProcessComparer();
        private void ProcessScanner_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            List<Process> processes = new List<Process>();
            processes.AddRange((Process[])e.Result);
            int selectedIndex = listProcesses.SelectedIndex;
            processes.Sort(processComparer);
            listProcesses.Items.Clear();
            foreach (Process process in processes)
            {
                listProcesses.Items.Add(new FormattedProcess(process));
            }
            listProcesses.SelectedIndex = selectedIndex;
            TimeSpan scanTime = DateTime.Now.Subtract(scanTimer);
            lblScanTime.Content = $"Scan time: {(int)scanTime.TotalMilliseconds}ms ago"; // scan #: {++scanCount}";
            scanTimer = DateTime.Now;
            UpdateProcessInfo();
            if (!CheckReflexInject())
            {
                if (scanTime < preferredScanInterval)
                {
                    processScanner.RunWorkerAsync(preferredScanInterval - scanTime);
                }
                else
                {
                    processScanner.RunWorkerAsync();
                }
            }
        }

        private void ProcessScanner_DoWork(object sender, DoWorkEventArgs e)
        {
            if(e.Argument != null)
            {
                TimeSpan timeSpan = (TimeSpan)e.Argument;
                System.Threading.Thread.Sleep(timeSpan);
            }
            Process[] processes = Process.GetProcesses();
            e.Result = processes;
        }

        private void lblBrowseDLL_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Filter = "DLL files (*.dll)|*.dll|All files (*.*)|*.*";
            if ((bool)openFile.ShowDialog())
            {
                string[] fileNames = openFile.FileNames;
                foreach(string fileName in fileNames)
                {
                    FileInfo fileInfo = new FileInfo(fileName);
                    if (fileInfo.Exists)
                    {
                        listDLLs.Items.Add(new FormattedFileInfo(fileInfo));
                    }
                }
            }
        }
    }
    public class ProcessComparer : IComparer<Process>
    {
        public int Compare(Process a, Process b)
        {
            return a.ProcessName.CompareTo(b.ProcessName);
        }
    }
}
