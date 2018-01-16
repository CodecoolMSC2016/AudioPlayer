using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Forms;
using System.Windows.Threading;
using System.Threading;
using MahApps.Metro.Controls;
using Newtonsoft.Json;
using System.Windows.Media.Effects;
using System.Windows.Media.Animation;
using System.Net;
using System.Net.Http;
using System.Windows.Input;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;

namespace AudioPlayer
{

    public partial class MainWindow : MetroWindow
    {
        List<Mp3> pl = new List<Mp3>();
        MediaPlayer me = new MediaPlayer();
        DispatcherTimer timer = new DispatcherTimer();
        private bool userIsDraggingSlider = false;
        private int currentIndex = -1;
        private bool shuffle = false;
        private int generatedNumber;
        private bool repeateOn = false;
        private int rowindex = -1;
        private int nowPlayingIndex = -1;
        private List<int> shuffleList = new List<int>();

        private List<object> _selItems = new List<object>();
        private Point start;
        private TimeSpan totalTime;
        private DispatcherTimer doubleClickTimer = new DispatcherTimer();
      
        

        private class Mp3 : INotifyPropertyChanged
        {

            
            int id;
            string name;
            string path;
            string artist;
            string album;
            string length;
            bool isPlaying = false;
            bool isPaused = false;
            TimeSpan duration;
            
            
           

            public Mp3(string path)
            {
                TagLib.File tagLib = TagLib.File.Create(path);
                this.Name = tagLib.Tag.Title;
                this.Artist = tagLib.Tag.FirstPerformer;
                this.Path = path;
                this.length = tagLib.Properties.Duration.ToString(@"mm\:ss");
                this.Album = tagLib.Tag.Album;
                this.Duration = tagLib.Properties.Duration;
                
            }

            
            public string Name { get => name; set => name = value; }
            public string Path { get => path; set => path = value; }
            public string Length { get => length; set => length = value; }
            public string Artist { get => artist; set => artist = value; }
            public string Album { get => album; set => album = value; }
            public bool IsPlaying { get => isPlaying; set { isPlaying = value; OnPropertyChanged("isPlaying"); }   }

            public bool IsPaused { get => isPaused; set => isPaused = value; }
            public TimeSpan Duration { get => duration; set => duration = value; }

            public event PropertyChangedEventHandler PropertyChanged = delegate { };
            protected void OnPropertyChanged(string property)
            {
                var e = new PropertyChangedEventArgs(property);
                PropertyChangedEventHandler changed = PropertyChanged;
                if (changed != null) changed(this, e);
            }

        }



       

        public delegate Point GetPosition(IInputElement element);


           


        public MainWindow()
        {
            InitializeComponent();
            Get_Background_Color();
            PlayList.PreviewMouseLeftButtonDown += new MouseButtonEventHandler(Playlist_PreviewLeftMouseButtonDown);
            //PlayList.Drop += new System.Windows.DragEventHandler(trackDataGrid_Drop);
        }


        private void Playlist_PreviewLeftMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            start = e.GetPosition(null);
            _selItems.Clear();
            _selItems.AddRange(PlayList.SelectedItems.Cast<object>());
        }

        //Drag and Drop Function (if on, multiple selection not working)


        //private void trackDataGrid_Drop(object sender, System.Windows.DragEventArgs e)
        //{
        //    if (rowindex < 0)
        //    {
        //        return;
        //    }
        //    int index = this.GetCurrentRowIndex(e.GetPosition);
        //    if (index < 0)
        //    {
        //        return;
        //    }
        //    if (index == rowindex)
        //    {
        //        return;
        //    }
        //    if (index == PlayList.Items.Count - 1)
        //    {
        //        return;
        //    }
        //    Mp3 changedTrack = pl[rowindex];
        //    pl.RemoveAt(rowindex);
        //    pl.Insert(index, changedTrack);
        //}


        //private void Playlist_PreviewLeftMouseButtonDown(object sender, MouseButtonEventArgs e)
        //{
        //    rowindex = GetCurrentRowIndex(e.GetPosition);
        //    if (rowindex < 0)
        //    {
        //        return;
        //    }
        //    PlayList.SelectedIndex = rowindex;
        //    Mp3 selectedTrack = PlayList.Items[rowindex] as Mp3;
        //    if (selectedTrack == null)
        //    {
        //        return;
        //    }
        //    System.Windows.DragDropEffects dragDropEffects = System.Windows.DragDropEffects.Move;
        //    if (DragDrop.DoDragDrop(PlayList, selectedTrack, dragDropEffects) != System.Windows.DragDropEffects.None)
        //    {
        //        PlayList.SelectedItem = selectedTrack;
        //    }
        //    PlayList.Items.Refresh();
        //}

        //private bool GetMouseTargetRow(Visual theTarget, GetPosition position)
        //{
        //    Rect rect = VisualTreeHelper.GetDescendantBounds(theTarget);
        //    Point point = position((IInputElement)theTarget);
        //    return rect.Contains(point);
        //}

        //private DataGridRow GetRowItem(int index)
        //{
        //    if (PlayList.ItemContainerGenerator.Status != System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
        //    {
        //        return null;
        //    }
        //    return PlayList.ItemContainerGenerator.ContainerFromIndex(index) as DataGridRow;
        //}

        //private int GetCurrentRowIndex(GetPosition pos)
        //{
        //    int curIndex = -1;
        //    for (int i = 0; i < PlayList.Items.Count; i++)
        //    {
        //        DataGridRow itm = GetRowItem(i);
        //        if (GetMouseTargetRow(itm, pos))
        //        {
        //            curIndex = i;
        //            break;
        //        }
        //    }
        //    return curIndex;
        //}




        private void Play_Button_Click(object sender, RoutedEventArgs e)
        {
            if (pl.Count != 0)
            {
                if(nowPlayingIndex == -1 || pl[nowPlayingIndex].IsPaused == true)
                {
                    if (StatusSlider.Value == 0)
                    {
                        timer.Stop();
                        PlayTrack(sender, e);
                    }
                    else
                    {
                        me.Play();
                        timer.Start();
                    }
                }
                else
                {
                    Stop_Button_Click(sender, e);
                    pl[nowPlayingIndex].IsPlaying = false;
                    nowPlayingIndex = currentIndex;
                    PlayTrack(sender, e);

                }
               
            }
            
        }

        private void PlayTrack(object sender, RoutedEventArgs e)
        {
            if (currentIndex == -1)
            {
                currentIndex = 0;
                nowPlayingIndex = 0;
                pl[currentIndex].IsPlaying = true;
                PlayList.SelectedItem = pl[currentIndex];
                me.Open(new Uri(pl[currentIndex].Path));
                Thread.Sleep(500);
                StatusSlider.Maximum = me.NaturalDuration.TimeSpan.TotalSeconds;
                New_timer(sender, e);
                me.Play();
                timer.Start();
                current_track.Content = pl[nowPlayingIndex].Artist + "-" + pl[nowPlayingIndex].Name;
            }
            else
            {
                if(nowPlayingIndex >= 0)
                {
                    pl[nowPlayingIndex].IsPlaying = false;
                }
                
                Mp3 item = (Mp3)PlayList.SelectedItem;
                item.IsPlaying = true;
                nowPlayingIndex = PlayList.SelectedIndex;
                me.Open(new Uri(item.Path));
                Thread.Sleep(500);
                StatusSlider.Maximum = me.NaturalDuration.TimeSpan.TotalSeconds;
                New_timer(sender, e);
                me.Play();
                timer.Start();
                current_track.Content = pl[nowPlayingIndex].Artist + "-" + pl[nowPlayingIndex].Name;
            }
        }

        private void Pause_Button_Click(object sender, RoutedEventArgs e)
        {
            me.Pause();
            timer.Stop();
            pl[nowPlayingIndex].IsPaused = true;
        }

        private void Stop_Button_Click(object sender, RoutedEventArgs e)
        {
            if (pl.Count != 0)
            {
                me.Stop();
                timer.Stop();
                StatusSlider.Value = 0;
                lblStatus.Content = String.Format("{0} / {1}", me.Position.ToString(@"mm\:ss"), me.NaturalDuration.TimeSpan.ToString(@"mm\:ss"));
            }
        }

        void Player_MediaEnded(object sender, EventArgs e)
        {
         
        }

        private void Open_Window(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();

            DialogResult res = dialog.ShowDialog();
            
            if (res == System.Windows.Forms.DialogResult.OK)
            {
                if (sender.Equals(Open_Folder))
                {
                    totalTime = new TimeSpan();
                    pl = new List<Mp3>();
                    GetMp3Files(dialog.SelectedPath);
                    total_length.Content = "Total time: " + totalTime.ToString(@"hh\:mm\:ss");
                }
                else
                {
                    GetMp3Files(dialog.SelectedPath);
                    total_length.Content = "Total time: " + totalTime.ToString(@"hh\:mm\:ss");
                }
                currentIndex = -1;
            }
            
        
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            Open_Window(sender, e);
            me.Stop();
            timer.Stop();
            StatusSlider.Value = 0;
            lblStatus.Content = "00:00/00:00";
        }

        void timer_Tick(object sender, EventArgs e)
        {
            if (me.Source != null)
            {
                if (me.NaturalDuration.HasTimeSpan)
                {
                    lblStatus.Content = String.Format("{0} / {1}", me.Position.ToString(@"mm\:ss"), me.NaturalDuration.TimeSpan.ToString(@"mm\:ss"));
                    StatusSlider.Value = me.Position.TotalSeconds;
                }
                if (me.NaturalDuration.HasTimeSpan)
                {
                    if (StatusSlider.Value == me.NaturalDuration.TimeSpan.TotalSeconds)
                    {
                        me.Stop();
                        StatusSlider.Value = 0;
                        timer.Stop();
                        Next_Track(sender, e);
                    }
                }
            }
            else
                lblStatus.Content = "00:00/00:00";
        }

        private void GetMp3Files(string path)
        {
            FileAttributes attr = File.GetAttributes(path);
            if (attr.HasFlag(FileAttributes.Directory))
            {
                string[] files = Directory.GetFiles(path); 
                string[] dirs = Directory.GetDirectories(path); 

                foreach (var mp3 in files)
                {
                    if (mp3.EndsWith("mp3"))
                    {
                        MediaPlayer mp = new MediaPlayer();
                        Mp3 temp = new Mp3(mp3);
                        mp.Open(new Uri(temp.Path));

                        totalTime += temp.Duration;

                        if(temp.Name == null)
                        {
                            temp.Name = System.IO.Path.GetFileName(temp.Path);
                        }
                        if(temp.Artist == null)
                        {
                            temp.Artist = "Unknown Artist";
                        }
                        if(temp.Album == null)
                        {
                            temp.Album = "Unknown Album";
                        }

                        if (temp.Length == null)
                        {
                            while (mp.NaturalDuration.HasTimeSpan == false)
                            {

                            }
                            temp.Length = CalculateTrackLenght(mp);
                            totalTime += mp.NaturalDuration.TimeSpan;
                        }





                        pl.Add(temp);
                        PlayList.ItemsSource = pl;
                        PlayList.Items.Refresh();
                    }
                }

                foreach (var item in dirs)
                {
                    GetMp3Files(System.IO.Path.GetFullPath(item));
                }
            }
            else
            {
                MediaPlayer mp = new MediaPlayer();
                Mp3 temp = new Mp3(path);
                totalTime += mp.NaturalDuration.TimeSpan;

                while (mp.NaturalDuration.HasTimeSpan == false)
                {

                }
                temp.Length = CalculateTrackLenght(mp);
                pl.Add(temp);
              
                PlayList.ItemsSource = pl;
                PlayList.Items.Refresh();
            }
        }

        private string CalculateTrackLenght(MediaPlayer mp)
        {
            string sec;
            int durationInSec = Convert.ToInt32(Math.Floor(mp.NaturalDuration.TimeSpan.TotalSeconds));
            int minutes = durationInSec / 60;
            int seconds = durationInSec % 60;
            if (seconds < 10)
            {
                sec = "0" + seconds.ToString();
            }
            else
            {
                sec = seconds.ToString();
            }
            return minutes.ToString() + ":" + sec;
        }

        private void FileItem_Click(object sender, RoutedEventArgs e)
        {
            totalTime = new TimeSpan();
            System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog();
            DialogResult res = dialog.ShowDialog();

            if (res.Equals(System.Windows.Forms.DialogResult.OK))
            {

                MediaPlayer mp = new MediaPlayer();
                Mp3 temp = new Mp3(System.IO.Path.GetFullPath(dialog.FileName));
                mp.Open(new Uri(temp.Path));

                totalTime += temp.Duration;

                if (temp.Name == null)
                {
                    temp.Name = System.IO.Path.GetFileName(temp.Path);
                }
                if (temp.Artist == null)
                {
                    temp.Artist = "Unknown Artist";
                }
                if (temp.Album == null)
                {
                    temp.Album = "Unknown Album";
                }

                if (temp.Length == null)
                {
                    while (mp.NaturalDuration.HasTimeSpan == false)
                    {

                    }
                    temp.Length = CalculateTrackLenght(mp);
                    totalTime += mp.NaturalDuration.TimeSpan;
                }


                pl = new List<Mp3>();
                pl.Add(temp);
            }

            PlayList.ItemsSource = pl;
            total_length.Content = "Total time: " + totalTime.ToString(@"hh\:mm\:ss");
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            currentIndex = PlayList.SelectedIndex;
        }

        private void New_timer(object sender, RoutedEventArgs e)
        {
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += timer_Tick;
        }
        
        private void Next_Track(object sender, EventArgs e)
        {
            if (shuffle == false && repeateOn == false)
            {
                timer.Stop();
                me.Stop();
                pl[nowPlayingIndex].IsPlaying = false;
                nowPlayingIndex ++;
                StatusSlider.Value = 0;
                if (currentIndex == -1)
                {
                    currentIndex = 0;
                }
                ShuffleOff();
            }
            if(repeateOn == true)
            {
                timer.Stop();
                me.Stop();
                StatusSlider.Value = 0;
                if(currentIndex == -1)
                {
                    currentIndex = 0;
                }
                ShuffleOff();
            }

            if(shuffle == true)
            {
                ShuffleOn();
            }
        }

        private void ShuffleOff()
        {
            if (currentIndex < PlayList.Items.Count)
            {
                pl[nowPlayingIndex].IsPlaying = true;
                me.Open(new Uri(pl[nowPlayingIndex].Path));
                Thread.Sleep(500);
                PlayList.SelectedItem = pl[currentIndex];
                StatusSlider.Maximum = me.NaturalDuration.TimeSpan.TotalSeconds;
                me.Play();
                timer.Start();
                current_track.Content = pl[nowPlayingIndex].Artist + "-" + pl[nowPlayingIndex].Name;
            }
            else
            {
                nowPlayingIndex = 0;
                PlayList.SelectedItem = pl[nowPlayingIndex];
                me.Open(new Uri(pl[nowPlayingIndex].Path));
                timer.Stop();
                lblStatus.Content = "00:00/00:00";
                current_track.Content = pl[nowPlayingIndex].Artist + "-" + pl[nowPlayingIndex].Name;
            }
        }

        private void ShuffleOn()
        {
            if(shuffleList.Capacity == PlayList.Items.Count)
            {
                shuffleList.Clear();
            }
            Generate_random(PlayList.Items.Count);
            while (nowPlayingIndex == generatedNumber || shuffleList.Contains(generatedNumber))
            {
                Generate_random(PlayList.Items.Count);
            }
            shuffleList.Add(nowPlayingIndex);
            pl[nowPlayingIndex].IsPlaying = false;
            nowPlayingIndex = generatedNumber;
            pl[nowPlayingIndex].IsPlaying = true;

            me.Open(new Uri(pl[nowPlayingIndex].Path));
            Thread.Sleep(500);
            PlayList.SelectedItem = pl[nowPlayingIndex];
            StatusSlider.Maximum = me.NaturalDuration.TimeSpan.TotalSeconds;
            me.Play();
            timer.Start();
            current_track.Content = pl[nowPlayingIndex].Artist + "-" + pl[nowPlayingIndex].Name;
        }
    

        private void Generate_random(int max)
        {
            Random r = new Random();
            generatedNumber = r.Next(0, max);
        }

        private void Next_Button_Click(object sender, RoutedEventArgs e)
        {
            if (pl.Count != 0)
            {
                Next_Track(sender, e);
            }
        }
        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            
        }
        
        private void StatusSlider_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            userIsDraggingSlider = true;
        }

        private void StatusSlider_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            userIsDraggingSlider = false;
            me.Position = TimeSpan.FromSeconds(StatusSlider.Value);
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
           
        }

        private void MenuItem_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            
        }

        private void VolumeSlider_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            
        }

        private void VolumeSlider_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            me.Volume = VolumeSlider.Value;
            if(me.Volume == 0)
            {
                VolumeIcon.Source = new BitmapImage(new Uri(@"pack://application:,,,/Resources/if_audio-volume-muted-blocking-panel_87528.png"));
            }
            else if(me.Volume > 0 && me.Volume <= 0.24)
            {
                VolumeIcon.Source = new BitmapImage(new Uri(@"pack://application:,,,/Resources/if_audio-volume-low-zero-panel_87526.png"));
            }
            else if(me.Volume > 0.24 && me.Volume <= 0.49)
            {
                VolumeIcon.Source = new BitmapImage(new Uri(@"pack://application:,,,/Resources/if_audio-volume-low-panel_87525.png"));
            }
            else if(me.Volume > 0.49 && me.Volume <= 0.74)
            {
                VolumeIcon.Source = new BitmapImage(new Uri(@"pack://application:,,,/Resources/if_audio-volume-medium-panel_87527.png"));
            }
            else
            {
                VolumeIcon.Source = new BitmapImage(new Uri(@"pack://application:,,,/Resources/if_audio-volume-high-panel_87524.png"));
            }
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {

        }

        private void Previous_Button_Click(object sender, RoutedEventArgs e)
        {
            if (pl.Count != 0)
            {
                timer.Stop();
                me.Stop();
                if (nowPlayingIndex > 0 && StatusSlider.Value < 2)
                {
                    pl[nowPlayingIndex].IsPlaying = false;
                    nowPlayingIndex--;
                }
                if (StatusSlider.Value > 2)
                {
                    me.Position = TimeSpan.FromSeconds(0);
                    StatusSlider.Value = 0;
                    me.Play();
                    timer.Start();
                    current_track.Content = pl[nowPlayingIndex].Artist + "-" + pl[nowPlayingIndex].Name;
                }
                else
                {
                    pl[nowPlayingIndex].IsPlaying = true;
                    me.Open(new Uri(pl[nowPlayingIndex].Path));
                    Thread.Sleep(500);
                    StatusSlider.Maximum = me.NaturalDuration.TimeSpan.TotalSeconds;
                    me.Play();
                    timer.Start();
                    current_track.Content = pl[nowPlayingIndex].Artist + "-" + pl[nowPlayingIndex].Name;
                }
            }
        }

        private void MenuItem_Click_2(object sender, RoutedEventArgs e)
        {

        }

        private void Exit_App(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void Show_Color_Picker(object sender, RoutedEventArgs e)
        {
            _colorPicker.IsOpen = true;
        }

        private void New_Background_Color(object sender, RoutedEventArgs e)
        {
            var ColorChanged = new LinearGradientBrush(Color.FromRgb(_colorPicker.SelectedColor.Value.R, _colorPicker.SelectedColor.Value.G, _colorPicker.SelectedColor.Value.B), Color.FromRgb(255, 255, 255), 90);
            grid1.Background = ColorChanged;
            using(StreamWriter sw = new StreamWriter("background_color.txt"))
            {
                sw.WriteLine(_colorPicker.SelectedColor.Value);
            }  
        }

        private void Get_Background_Color()
        {
            string currentfile = "background_color.txt";
            string color = "";

            if (File.Exists(currentfile))
            {
                using (StreamReader sr = new StreamReader(currentfile))
                {
                    if ((color = sr.ReadLine()) != null)
                    {
                        grid1.Background = new LinearGradientBrush((Color)ColorConverter.ConvertFromString(color), Color.FromRgb(255, 255, 255), 90);
                    }
                }
            }
        }

        private void Add_To_Playlist(object sender, RoutedEventArgs e)
        {
            Open_Window(sender, e);
            PlayList.ItemsSource = pl;
            PlayList.Items.Refresh();
        }

        private void Save_Playlist(object sender, RoutedEventArgs e)
        {
            string save_playlist = JsonConvert.SerializeObject(pl);
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Text file (*.txt)|*.txt|C# file (*.cs)|*.cs";
            saveFileDialog.ShowDialog();
            
            if(saveFileDialog.FileName != "")
            {
                File.WriteAllText(saveFileDialog.FileName, save_playlist);
            }
        }

        private void Load_Playlist(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog();
            DialogResult res = dialog.ShowDialog();
            if (res.Equals(System.Windows.Forms.DialogResult.OK))
            {
                using (StreamReader sr = new StreamReader(dialog.FileName))
                {
                    string result = sr.ReadToEnd();
                    pl = (List<Mp3>)JsonConvert.DeserializeObject(result, typeof(List<Mp3>));
                    PlayList.ItemsSource = pl;
                }
            }
        }

        private void Shuffle_Button(object sender, RoutedEventArgs e)
        {
            DoubleAnimation shuffleanimation = new DoubleAnimation();
            DoubleAnimation repeatanimation = new DoubleAnimation();
            if (shuffle == false)
            {
                shuffle = true;
                repeateOn = false;
                shuffleanimation.To = 0;
                repeatanimation.To = 1;
                repeatanimation.Duration = new Duration(new TimeSpan(0));
                RepeatShadowEffect.BeginAnimation(DropShadowEffect.OpacityProperty, repeatanimation);
            }
            else
            {
                shuffle = false;
                shuffleanimation.To = 1;
            }
            shuffleanimation.Duration = new Duration(new TimeSpan(0));
            ShuffleShadowEffect.BeginAnimation(DropShadowEffect.OpacityProperty, shuffleanimation);
        }

        private void Repeat_Button_Click(object sender, RoutedEventArgs e)
        {
            DoubleAnimation repeatanimation = new DoubleAnimation();
            DoubleAnimation shuffleanimation = new DoubleAnimation();

            if (repeateOn == false)
            {
                repeateOn = true;
                shuffle = false;
                repeatanimation.To = 0;
                shuffleanimation.To = 1;
                shuffleanimation.Duration = new Duration(new TimeSpan(0));
                ShuffleShadowEffect.BeginAnimation(DropShadowEffect.OpacityProperty, shuffleanimation);
            }
            else
            {
                repeateOn = false;
                repeatanimation.To = 1;
            }

            repeatanimation.Duration = new Duration(new TimeSpan(0));
            RepeatShadowEffect.BeginAnimation(DropShadowEffect.OpacityProperty, repeatanimation);
        }

        private void Search_Service(object sender, RoutedEventArgs e)
        {
            using (var client = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate }))
            {
                client.BaseAddress = new Uri("http://192.168.150.18:56887/");
                HttpResponseMessage resp = client.GetAsync("Files/GetAvailableFiles").Result;
                resp.EnsureSuccessStatusCode();
                //Stream result = resp.Content.ReadAsStreamAsync().Result;
                string result = resp.Content.ReadAsStringAsync().Result;
                List<Mp3> recieved = JsonConvert.DeserializeObject<List<Mp3>>(result);
                foreach (var mp3 in recieved)
                {
                    System.Windows.MessageBox.Show(mp3.Name);
                }
            }
        }
        private void Grid_Drop(object sender, System.Windows.DragEventArgs e)
        {
            if(null != e.Data && e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                var data = e.Data.GetData(System.Windows.DataFormats.FileDrop) as string[];
                GetMp3Files(data[0]);
            }
        }

        private void Grid_DragOver(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                e.Effects = System.Windows.DragDropEffects.Copy;
            }
            else
            {
                e.Effects = System.Windows.DragDropEffects.None;
            }
        }

        private void Delete_Track_From_PlayList(object sender, RoutedEventArgs e)
        {
            List<int> indexes = new List<int>();
            foreach(object track in PlayList.SelectedItems)
            {
                indexes.Add(PlayList.Items.IndexOf(track));
            }
            indexes.Sort();
            indexes.Reverse();
            foreach(int index in indexes)
            {
                pl.RemoveAt(index);
            }

            PlayList.ItemsSource = pl;
            PlayList.Items.Refresh();
        }

        //private void PlayList_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        //{

        //    (sender as System.Windows.Controls.DataGrid).SelectedItem = null;
        //}

        [DllImport("user32")]

        private static extern uint GetDoubleClickTime();

        private void AudioPlayer_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            
            
        }
       
    }
}
