using MaterialDesignThemes.Wpf;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace MergeScanDoc
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            string sAssemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            AppName.Text = Assembly.GetExecutingAssembly().GetName().Name;
            VersionInfo.Text = "バージョン: " + sAssemblyVersion.Split('.')[0] + "." + sAssemblyVersion.Split('.')[1];
            BuildInfo.Text = "ビルド: " + sAssemblyVersion.Split('.')[2] + "." + sAssemblyVersion.Split('.')[3];
        }


        private async void MergeStart_Click(object sender, RoutedEventArgs e)
        {
            bool blResult = false;
            int iFailCnt = 0;
            CtrlButton(false);

            MyFolderList list = (MyFolderList)DataContext;
            string sBackFolderName = BackTextBox.Text;
            string sFrontFolderName = FrontTextBox.Text;
            bool blDelete = (bool)DeleteCheckBox.IsChecked;

            // リストがなくなるまでループ。処理完了するとリストから削除。
            while (list.FolderPath.Count > 0 + iFailCnt)
            {
                await Task.Run(() =>
                {
                    blResult = MergeScanData(list.FolderPath[iFailCnt], list.FolderPath[iFailCnt] + @"\" + sFrontFolderName, list.FolderPath[iFailCnt] + @"\" + sBackFolderName, blDelete);
                });

                if (blResult)
                {
                    list.FolderPath.Remove(list.FolderPath[iFailCnt]);
                }
                else
                {
                    iFailCnt++;
                }
                if (list.FolderPath == null) break;
            }
            if(iFailCnt == 0) FolderListHint.Visibility = Visibility.Visible;
            CtrlButton(true);
        }

        private bool MergeScanData(string sMergeedFolderPath, string sFrontFolderPath, string sBackFolderPath, bool blDelete)
        {

            if (!Directory.Exists(sFrontFolderPath))
            {
                MessageBox.Show("下記フォルダが存在しないためスキップします\r\n" + sFrontFolderPath);
                return false;
            }

            if (!Directory.Exists(sBackFolderPath))
            {
                MessageBox.Show("下記フォルダが存在しないためスキップします\r\n" + sBackFolderPath);
                return false;
            }

            try
            {
                string[] sOddFilePath = Directory.GetFiles(sFrontFolderPath).OrderBy(name => name).ToArray();
                string[] sEvenFilePath = Directory.GetFiles(sBackFolderPath).OrderBy(name => name).ToArray();

                if (sOddFilePath.Length == sEvenFilePath.Length)
                {
                    for (int iFileInedx = 0; iFileInedx < sOddFilePath.Length; iFileInedx++)
                    {
                        File.Copy(sOddFilePath[iFileInedx], sMergeedFolderPath + @"\" + (((iFileInedx + 1) * 2) - 1).ToString() + Path.GetExtension(sOddFilePath[iFileInedx]));
                    }

                    for (int iFileInedx = 0; iFileInedx < sOddFilePath.Length; iFileInedx++)
                    {
                        File.Copy(sEvenFilePath[iFileInedx], sMergeedFolderPath + @"\" + ((sOddFilePath.Length - iFileInedx) * 2).ToString() + Path.GetExtension(sEvenFilePath[iFileInedx]));
                    }
                }
                else
                {
                    MessageBox.Show("下記フォルダ内の裏面/表面フォルダ内のファイル数が一致しないためスキップします\r\n" + sMergeedFolderPath);
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
                return false;
            }


            if (blDelete)
            {
                try
                {
                    Directory.Delete(sFrontFolderPath, true);
                    Directory.Delete(sBackFolderPath, true);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }

            return true;
        }

        /// <summary>
        /// ファイルリストすべてを削除する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AllDelete_Click(object sender, RoutedEventArgs e)
        {
            MyFolderList list = (MyFolderList)DataContext;

            for (int iIndex = 0; iIndex < list.FolderPath.Count;)
            {
                list.FolderPath.RemoveAt(iIndex);
            }
            FolderListHint.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// 選択されたファイルのみを削除する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            MyFolderList list = (MyFolderList)DataContext;

            if (FolderList.SelectedIndex == -1)
            {
                return;
            }
            int index = FolderList.SelectedIndex;
            list.FolderPath.RemoveAt(index);

            if (list.FolderPath.Count == 0)
            {
                FolderListHint.Visibility = Visibility.Visible;
            }
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            MyFolderList list = this.DataContext as MyFolderList;
            string[] sFiles = (string[])e.Data.GetData(DataFormats.FileDrop);

            // ドラッグされている場合
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // 配列分ループ
                foreach (string sFile in sFiles)
                {
                    // フォルダだった場合はAdd
                    if (Directory.Exists(sFile))
                    {
                        list.FolderPath.Add(sFile);
                    }
                }

                e.Effects = DragDropEffects.Copy;
            }
            if(list.FolderPath.Count > 0) FolderListHint.Visibility = Visibility.Hidden;
        }
        private void Window_PreviewDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, true))
            {
                e.Effects = DragDropEffects.Copy;
            }
            else
            {
                e.Effects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void CtrlButton(bool blIsEnabled)
        {
            MergeStart.IsEnabled = blIsEnabled;
            ButtonProgressAssist.SetIsIndicatorVisible(MergeStart, !blIsEnabled);
            ButtonProgressAssist.SetIsIndeterminate(MergeStart, !blIsEnabled);

            FolderList.IsEnabled = blIsEnabled;
        }
    }
    public class MyFolderList
    {
        public MyFolderList()
        {
            FolderPath = new ObservableCollection<string>();
        }
        public ObservableCollection<string> FolderPath
        {
            get;
            private set;
        }
    }
    }
