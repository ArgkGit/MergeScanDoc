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

        /// <summary>
        /// マージボタンをクリック後の処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void MergeStart_Click(object sender, RoutedEventArgs e)
        {
            bool blResult;                  // マージ結果
            bool blDelete;                  // マージ完了後に表面/裏面フォルダを削除する場合はtrue

            int iFailCnt;                   // マージ失敗回数

            string sBackFolderName;         // 表面フォルダ名
            string sFrontFolderName;        // 裏面フォルダ名

            MyFolderListBox myFolderList;   // ドラッグ・アンド・ドロップ先のListBox

            CtrlButton(false);

            #region 初期化
            blResult = false;
            blDelete = (bool)DeleteCheckBox.IsChecked;

            iFailCnt = 0;

            sBackFolderName = BackTextBox.Text;
            sFrontFolderName = FrontTextBox.Text;

            myFolderList = (MyFolderListBox)DataContext;
            #endregion

            // リストがなくなるまでループ。処理完了するとリストから削除。
            while (myFolderList.sPath.Count > 0 + iFailCnt)
            {
                await Task.Run(() =>
                {
                    blResult = MergeScanData(myFolderList.sPath[iFailCnt],
                                             myFolderList.sPath[iFailCnt] + @"\" + sFrontFolderName,
                                             myFolderList.sPath[iFailCnt] + @"\" + sBackFolderName,
                                             blDelete);
                });

                if (blResult)
                {
                    myFolderList.sPath.Remove(myFolderList.sPath[iFailCnt]);
                }
                else
                {
                    iFailCnt++;
                }
                if (myFolderList.sPath == null) break;
            }

            if(iFailCnt == 0) FolderListHint.Visibility = Visibility.Visible;

            CtrlButton(true);
        }

        /// <summary>
        /// 表面/裏面フォルダ内のファイルをリネームし、マージする
        /// </summary>
        /// <param name="sMergedFolderPath">マージ先のフォルダパス</param>
        /// <param name="sFrontFolderPath">表面フォルダパス</param>
        /// <param name="sBackFolderPath">裏面フォルダパス</param>
        /// <param name="blDelete">マージ完了後に表面/裏面フォルダを削除する場合はtrue</param>
        /// <returns></returns>
        private bool MergeScanData(string sMergedFolderPath, string sFrontFolderPath, string sBackFolderPath, bool blDelete)
        {
            string[] sOddFilePathArr;  // 奇数ページのファイルパス格納先(ファイル名順にソートされた状態で格納)
            string[] sEvenFilePathArr; // 偶数ページのファイルパス格納先(ファイル名順にソートされた状態で格納)

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
                sOddFilePathArr = Directory.GetFiles(sFrontFolderPath).OrderBy(name => name).ToArray();
                sEvenFilePathArr = Directory.GetFiles(sBackFolderPath).OrderBy(name => name).ToArray();

                if (sOddFilePathArr.Length == sEvenFilePathArr.Length)
                {
                    // トータル10ページの場合、 1, 3, 5, 7, 9ページの順にコピー
                    for (int iFileInedx = 0; iFileInedx < sOddFilePathArr.Length; iFileInedx++)
                    {
                        File.Copy(sOddFilePathArr[iFileInedx], sMergedFolderPath + @"\" + (((iFileInedx + 1) * 2) - 1).ToString() + Path.GetExtension(sOddFilePathArr[iFileInedx]));
                    }

                    // トータル10ページの場合、10, 8, 6, 4, 2ページの順にコピー
                    for (int iFileInedx = 0; iFileInedx < sOddFilePathArr.Length; iFileInedx++)
                    {
                        File.Copy(sEvenFilePathArr[iFileInedx], sMergedFolderPath + @"\" + ((sOddFilePathArr.Length - iFileInedx) * 2).ToString() + Path.GetExtension(sEvenFilePathArr[iFileInedx]));
                    }
                }
                else
                {
                    MessageBox.Show("下記フォルダ内の裏面/表面フォルダ内のファイル数が一致しないためスキップします\r\n" + sMergedFolderPath);
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
        /// リストからすべてのパスを削除する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AllDelete_Click(object sender, RoutedEventArgs e)
        {
            MyFolderListBox myFolderList;   // ドラッグ・アンド・ドロップ先のListBox

            myFolderList = (MyFolderListBox)DataContext;

            for (int iListIndex = 0; iListIndex < myFolderList.sPath.Count;)
            {
                myFolderList.sPath.RemoveAt(iListIndex);
            }

            FolderListHint.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// リストから選択されたフォルダパスのみを削除する
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            int iListIndex;                 // 選択されたListBoxのインデックス値
            MyFolderListBox myFolderList;   // ドラッグ・アンド・ドロップ先のListBox

            myFolderList = (MyFolderListBox)DataContext;

            if (FolderList.SelectedIndex == -1)
            {
                return;
            }
            iListIndex = FolderList.SelectedIndex;
            myFolderList.sPath.RemoveAt(iListIndex);

            if (myFolderList.sPath.Count == 0)
            {
                FolderListHint.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// ドラッグ・アンド・ドロップされたときに発生するイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Drop(object sender, DragEventArgs e)
        {
            string[] sFilePathArr;          // ドラッグ・アンド・ドロップされたファイル/フォルダパス
            MyFolderListBox myFolderList;   // ドラッグ・アンド・ドロップ先のListBox

            myFolderList = (MyFolderListBox)DataContext;
            sFilePathArr = (string[])e.Data.GetData(DataFormats.FileDrop);

            // ドラッグされている場合
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // 配列分ループ
                foreach (string sFile in sFilePathArr)
                {
                    // フォルダだった場合はAdd
                    if (Directory.Exists(sFile))
                    {
                        myFolderList.sPath.Add(sFile);
                    }
                }
                e.Effects = DragDropEffects.Copy;
            }
            if(myFolderList.sPath.Count > 0) FolderListHint.Visibility = Visibility.Hidden;
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

        /// <summary>
        /// ボタンの状態を切り替える
        /// </summary>
        /// <param name="blIsEnabled">押せなくする場合はfalse</param>
        private void CtrlButton(bool blIsEnabled)
        {
            MergeStart.IsEnabled = blIsEnabled;
            ButtonProgressAssist.SetIsIndicatorVisible(MergeStart, !blIsEnabled);
            ButtonProgressAssist.SetIsIndeterminate(MergeStart, !blIsEnabled);

            FolderList.IsEnabled = blIsEnabled;
        }
    }

    /// <summary>
    /// ドラッグ・アンド・ドロップ先のListBox構造
    /// </summary>
    public class MyFolderListBox
    {
        public MyFolderListBox()
        {
            sPath = new ObservableCollection<string>();
        }
        public ObservableCollection<string> sPath
        {
            get;
            private set;
        }
    }
}
