using System;
using System.Collections.Generic;
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
using System.Threading;
using System.Net;
using System.Windows.Interop;
using System.Net.Sockets;
using System.IO;
using System.Configuration;
using System.Runtime.InteropServices;

using NAudio.Wave;
using NAudio.CoreAudioApi;

namespace ClientPresentation
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Class
        class DataFile
        {
            public byte[] buffer;
            public string name;
            public DataFile(byte[] b, string n)
            {
                buffer = b;
                name = n;
            }
        }
        // ImageBrush
        ImageBrush stop = new ImageBrush(new BitmapImage(new Uri(@"Image\stop.png", UriKind.Relative))) { Stretch = Stretch.Uniform };
        ImageBrush start = new ImageBrush(new BitmapImage(new Uri(@"Image\start.png", UriKind.Relative))) { Stretch = Stretch.Uniform };
        ImageBrush m_not = new ImageBrush(new BitmapImage(new Uri(@"Image\m_not.png", UriKind.Relative))) { Stretch = Stretch.Uniform };
        ImageBrush m_yes = new ImageBrush(new BitmapImage(new Uri(@"Image\m_yes.png", UriKind.Relative))) { Stretch = Stretch.Uniform };

        //
        WaveOut output;
        //буфферный поток для передачи через сеть
        BufferedWaveProvider bufferStream;
        //поток для прослушивания входящих сообщений
        Thread in_thread;
        //сокет для приема (протокол UDP)
        TcpClient listeningSocket;

        bool stan = false;
        bool connect = false;
        bool savephoto = false;
        bool m_up = true;
        bool change_w = false;
        bool microfone = true;
        bool HideRectangle = false;
        bool connecting = false;
        // TcpClient
        TcpClient client = new TcpClient();
        TcpClient client_chat = new TcpClient();
        TcpClient File_chat = new TcpClient();
        //NetworkStream
        NetworkStream stream;
        NetworkStream chat_stream;
        NetworkStream file_stream;
        // String
        string MyIp = "";
        string name = "";
        string mail = "";
        // Int
        int port = 0, port_chat = 0, port_file = 0, port_microfone = 0;
        int coun_photo;
        int X_M, Y_M;
        // Thread
        Thread T_Receive_Video = null;
        Thread move_form;
        // Task
        Task Timer;
        Task Chat;
        // List
        List<DataFile> ListDataFile = new List<DataFile>();
        TcpClient receiver;
        NetworkStream remoteIp;

        object locker = new object();
        public MainWindow()
        {
            //AllocConsole();
            InitializeComponent();
            Rectangle1.Fill = start;
            photodesctop.Background = new ImageBrush(new BitmapImage(new Uri(@"Image\bacground.jpg", UriKind.Relative))) { Stretch = Stretch.Fill };
            client.ReceiveBufferSize = client.SendBufferSize = Int32.MaxValue;
            r_audio.Fill = m_yes;
            m_l.Content = "<>";
        }
        private void r_MouseLeave(object sender, MouseEventArgs e){
            if (!HideRectangle){
                foreach (var a in buttondesctop.Children)
                    try { (a as Rectangle).Opacity = 0; } catch { try { (a as ListBox).Opacity = 0; } catch { (a as TextBlock).Opacity = 0; } }
                buttondesctop.Opacity = 0;
            }
        }
        private void r_MouseEnter(object sender, MouseEventArgs e)
        {
            if (!HideRectangle)
            {
                foreach (var a in buttondesctop.Children)
                    try { (a as Rectangle).Opacity = 0.5; } catch { try { (a as ListBox).Opacity = 0.5; } catch { (a as TextBlock).Opacity = 0.5; } }
                buttondesctop.Opacity = 0.5;
            }
        }
        private void Rectangle_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (connect)
            {
                if (!stan)
                {
                    Rectangle1.Fill = stop;
                    photodesctop.Background = Brushes.Silver;
                    if (T_Receive_Video == null)
                    {
                        T_Receive_Video = new Thread(new ThreadStart(Receive_Video));
                        T_Receive_Video.Start();
                    }
                }
                else
                {
                    Rectangle1.Fill = start;
                    photodesctop.Background = photodesctop.Background = new ImageBrush(new BitmapImage(new Uri(@"Image\bacground.jpg", UriKind.Relative))) { Stretch = Stretch.Fill };
                }
                stan = !stan;
            }
            else
            {
                if (!connect)
                    StartProgram(MyIp, name, mail, port, port_chat, port_file, port_microfone,HideRectangle);
            }
        }
        private BitmapImage ConvertToBitmapImage(System.Drawing.Bitmap bitmap)
        {
            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            ms.Seek(0, System.IO.SeekOrigin.Begin);
            image.StreamSource = ms;
            image.EndInit();
            return image;
        }
        public void Receive_Video()
        {
            ImageBrush desctopphoto;           
            while (true){
                try{
                    if (connect){
                        byte[] size = new byte[4];
                        stream.Read(size, 0, 4);
                        while (!stream.DataAvailable)
                            Thread.Sleep(1);
                        byte[] data = new byte[BitConverter.ToInt32(size, 0)];
                        var l = stream.Read(data, 0, data.Length);
                        while (l < data.Length){
                            l += stream.Read(data, l, data.Length - l);
                            Thread.Sleep(1);
                        }                        
                            MemoryStream photo = new MemoryStream(data);
                            if (stan){
                                this.Dispatcher.Invoke(() =>{
                                    BitmapImage image = new BitmapImage();
                                    image.BeginInit();
                                    photo.Seek(0, System.IO.SeekOrigin.Begin);
                                    image.StreamSource = photo;
                                    image.EndInit();
                                    desctopphoto = new ImageBrush(image);
                                    if (!HideRectangle)
                                        mail_image_desctop.Source = image;
                                    //photodesctop.Background = desctopphoto;
                                    else image_desctop.Source = image;
                                    if (savephoto){
                                            System.Drawing.Bitmap scrin = new System.Drawing.Bitmap(image.StreamSource);
                                            Task.Run(new Action(delegate (){
                                                if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + @"File\" + DateTime.Now.ToLongDateString()))
                                                    Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + @"File\" + DateTime.Now.ToLongDateString());
                                                scrin.Save(AppDomain.CurrentDomain.BaseDirectory + @"File\" + DateTime.Now.ToLongDateString() + @"\" + DateTime.Now.Minute.ToString() + "min - " + (DateTime.Now.Millisecond / 100).ToString() + " sec = " + coun_photo.ToString() + ".jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                                                ++coun_photo;
                                            }));
                                            savephoto = false;
                                     }
                                });                           
                        }
                    }
                }
                catch (Exception s) {
                }
            }
        }
        private void Tick(){
            DateTime start = DateTime.Now;
            while (true){
                if (connect){
                    this.Dispatcher.Invoke(
                        () => {
                            info_1.Text = "-> " + (DateTime.Now - start).ToString().Remove(8, 8) + " : " + name+ Environment.NewLine + "-> " + MyIp + " : " + port.ToString();
                            TextBoxChatClient.SelectionStart = TextBoxChatClient.Text.Length;
                            TextBoxChatClient.ScrollToEnd();
                        });
                    Thread.Sleep(1000);
                }
            }
        }
        public void StartProgram(string IP, string name, string mail, int port, int port_c, int port_f, int port_m,bool HideRectangle){
            if (!connecting)
            {
                try
                {
                    connecting = true;
                    this.MyIp = IP;
                    this.port = port;
                    this.port_file = port_f;
                    this.port_chat = port_c;
                    this.port_microfone = port_m;
                    this.name = name;
                    this.mail = mail;
                    this.HideRectangle = HideRectangle;
                    if (HideRectangle)
                    {
                        menu_grid.Opacity = 1;
                        buttondesctop.Opacity = 0.8;
                    }
                    this.Dispatcher.Invoke(() =>
                    {
                        progress_bar.Minimum = progress_bar.Value = 0;
                        progress_bar.Maximum = 1;
                    });
                    client.Connect(IP, port);
                    if (T_Receive_Video == null)
                    {
                        T_Receive_Video = new Thread(new ThreadStart(Receive_Video));
                        T_Receive_Video.Start();
                        Rectangle1.Fill = stop;
                        stan = true;
                    }
                    StartChat();
                    StartFile();
                    StartMicrofone();
                    stream = client.GetStream();
                    byte[] data = System.Text.Encoding.Unicode.GetBytes(name + "<&&>" + mail);
                    stream.Write(data, 0, data.Length);
                    Timer = new Task(new Action(Tick));
                    Timer.Start();
                    connect = true;
                    this.Dispatcher.Invoke(() =>
                    {
                        TextBoxChatClient.Text += "You connect to server..." + Environment.NewLine;
                        info_1.ToolTip = mail;
                    });
                }
                catch(Exception s)
                {
                    
                    this.Dispatcher.Invoke(() => { TextBoxChatClient.Text += "Error connecting!\n"; });
                    connect =  false;
                    MessageBox.Show(s.Message);
                }
                connecting = false;
            }
            else
                this.Dispatcher.Invoke(() => { TextBoxChatClient.Text += "Noooo!\n"; });
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e){
            if (connect){
                byte[] type = new byte[1] { 2 };
                byte[] arr = new byte[1];
                SendDataChat(arr, type);
            }
            T_Receive_Video?.Abort();
            in_thread?.Abort();

        }
        public void StartChat() {
            Task c = new Task(new Action(delegate () {
                try
                {
                    client_chat.Connect(MyIp, port_chat);
                    chat_stream = client_chat.GetStream();
                    Chat = new Task(new Action(delegate () { ReceiveData(); }));
                    Chat.Start();
                }
                catch (Exception s) {
                    MessageBox.Show(s.Message);
                }
            }));
            c.Start();
        }
        public void ReceiveData(){
            try {
                while (true){
                    byte[] type = new byte[1];
                    byte[] size = new byte[4];
                    chat_stream.Read(type, 0, 1);
                    chat_stream.Read(size, 0, 4);
                    byte[] buffer = new byte[BitConverter.ToInt32(size, 0)];
                    int l = chat_stream.Read(buffer, 0, buffer.Length);
                    while (l < buffer.Length){
                        l += stream.Read(buffer, l, buffer.Length - l);
                        Thread.Sleep(1);
                    }
                    if (type[0] == 2){
                        this.Dispatcher.Invoke(() => { TextBoxChatClient.Text += Encoding.Unicode.GetString(buffer) + Environment.NewLine; });
                        Chat = null;
                        client = new TcpClient();
                        client_chat = new TcpClient();
                        File_chat = new TcpClient();
                        stream = null;
                        chat_stream = null;
                        file_stream = null;
                        T_Receive_Video?.Abort();
                        T_Receive_Video = null;
                        connect = false;
                        stan = true;
                       
                        in_thread.Abort();
                        output.Dispose();
                        this.Dispatcher.Invoke(() => {
                            info_2.Items.Clear();
                            Rectangle1.Fill = start;
                            info_1.Text = "-> " + "00.00" + " : " + name + info_2.Items.Count.ToString() + Environment.NewLine + "-> " + MyIp + " : " + port.ToString();
                            photodesctop.Background = new ImageBrush(new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + @"\Image\bacground.jpg"))) {
                                Stretch = Stretch.Fill
                            };
                            image_desctop.Source = mail_image_desctop.Source = null;
                        });

                    }
                    else if (type[0] == 3){
                        this.Dispatcher.Invoke(() => {
                            if ("Connect : " + name + " -> " + MyIp != Encoding.Unicode.GetString(buffer)) {
                                string name_c = "", buffer_string = Encoding.Unicode.GetString(buffer);
                                for (int i = 0; i< buffer_string.Length; ++i)
                                {
                                    if (buffer_string[i] == '-' && buffer_string[i + 1] == '>')
                                        break;
                                    name_c += buffer_string[i];
                                }
                                TextBoxChatClient.Text += "Connect : " + name_c + Environment.NewLine;
                                info_2.Items.Add(Encoding.Unicode.GetString(buffer));
                            }
                        });
                    }
                    else if (type[0] == 4){
                        this.Dispatcher.Invoke(() =>{
                                for (int i = 0; i < info_2.Items.Count; ++i)
                                    if (info_2.Items[i].ToString() == Encoding.Unicode.GetString(buffer)){
                                        string name_c = "";
                                        for (int j = 0; j < info_2.Items[i].ToString().Length; ++j) {
                                            if (info_2.Items[i].ToString()[j] == '-' && info_2.Items[i].ToString()[j + 1] == '>') break;
                                            name_c += info_2.Items[i].ToString()[j];
                                        }
                                        TextBoxChatClient.Text += "Disconnect : " +name_c + Environment.NewLine;
                                        info_2.Items.RemoveAt(i);
                                    }
                        });
                    }
                    else if (type[0] == 5){
                        this.Dispatcher.Invoke(() =>{
                            info_2.Items.Add(Encoding.Unicode.GetString(buffer));
                        });
                    }
                    else
                        this.Dispatcher.Invoke(() => { TextBoxChatClient.Text += Encoding.Unicode.GetString(buffer) + Environment.NewLine; });
                    this.Dispatcher.Invoke(()=> {
                        TextBoxChatClient.SelectionStart = TextBoxChatClient.Text.Length;
                        TextBoxChatClient.ScrollToEnd();
                    });
                }
            }
            catch (Exception s) {  }
        }
        private void TextBoxYou_KeyDown(object sender, KeyEventArgs e){
            string text = "";
            this.Dispatcher.Invoke(() => {
                text = TextBoxYou.Text;
            });
            byte[] type = new byte[1] { 1 };
            if (e.Key == Key.Enter && e.KeyboardDevice.Modifiers == ModifierKeys.Shift)
                this.Dispatcher.Invoke(() => {
                    TextBoxYou.Text += Environment.NewLine;
                });
            else if (e.Key == Key.Enter){
                Task send = new Task(new Action(delegate () {
                    SendDataChat(Encoding.Unicode.GetBytes(name + " -> " + text), type);
                }));
                send.Start();
                this.Dispatcher.Invoke(() =>{
                    TextBoxChatClient.Text += $"{name} -> " + TextBoxYou.Text + Environment.NewLine;
                    TextBoxYou.Text = "";
                });
            }
            this.Dispatcher.Invoke(() => {
                TextBoxYou.SelectionStart = TextBoxYou.Text.Length;
                TextBoxChatClient.SelectionStart = TextBoxChatClient.Text.Length;
                TextBoxChatClient.ScrollToEnd();
            });
        }
        private void SendDataChat(byte[] arr, byte[] type)
        {
            byte[] buffer = new byte[5 + arr.Length];
            Array.Copy(type, 0, buffer, 0, 1);
            Array.Copy(BitConverter.GetBytes(arr.Length), 0, buffer, 1, 4);
            Array.Copy(arr, 0, buffer, 5, arr.Length);
            try{
                chat_stream.Write(buffer, 0, buffer.Length);
            }
            catch {  }
        }
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (this.WindowState != WindowState.Maximized && e.Key == Key.F11)
            {
                this.WindowState = WindowState.Maximized;
                m_l.FontSize = exit_l.FontSize = hide_l.FontSize = 15;
            }
            else if (this.WindowState == WindowState.Maximized && e.Key == Key.F11)
            {
                this.WindowState = WindowState.Normal;
                m_l.FontSize = exit_l.FontSize = hide_l.FontSize = 7;
            }
            else if (e.Key == Key.PrintScreen)
                savephoto = true;
        }
        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.PrintScreen)
                savephoto = true;
        }
        private void ListFile_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ListFile.Items.Count > 0 && ListFile.SelectedIndex != null)
            {
                Task savefile = new Task(new Action(delegate ()
                {
                    int index = 0;
                    this.Dispatcher.Invoke(() => { index = ListFile.SelectedIndex; });
                    if (index != null)
                    {
                        if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + @"File\" + DateTime.Now.ToLongDateString()))
                            Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + @"File\" + DateTime.Now.ToLongDateString());
                        File.WriteAllBytes(AppDomain.CurrentDomain.BaseDirectory + @"File\" + DateTime.Now.ToLongDateString() + @"\" + ListDataFile[index].name, ListDataFile[index].buffer);
                    }
                    this.Dispatcher.Invoke(() => { TextBoxChatClient.Text += "Saved : " + ListDataFile[index].name + Environment.NewLine; });
                }));
                savefile.Start();
            }
        }
        public void StartFile()
        {
            try
            {
                File_chat.Connect(MyIp, port_file);
                File_chat.ReceiveBufferSize = File_chat.SendBufferSize = 50001000;
                file_stream = File_chat.GetStream();

                Task rec_file = new Task(new Action(delegate () { ReceiveFile(); }));
                rec_file.Start();
            }
            catch (Exception s) {
                MessageBox.Show(s.Message);
            }
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                System.Windows.Forms.OpenFileDialog open = new System.Windows.Forms.OpenFileDialog();
                if (System.Windows.Forms.DialogResult.OK == open.ShowDialog())
                {
                    Task readFile = new Task(new Action(delegate () { SendFile(File.ReadAllBytes(open.FileName), System.IO.Path.GetFileName(open.FileName)); }));
                    readFile.Start();
                }
            }
            catch { }
        }
        private void ReceiveFile()
        {
            file_stream.ReadTimeout = file_stream.WriteTimeout = 60000;
            try
            {
                while (true)
                {

                    byte[] size = new byte[4];
                    file_stream.Read(size, 0, 4);


                    byte[] name = new byte[BitConverter.ToInt32(size, 0)];
                    int l = file_stream.Read(name, 0, name.Length);

                    while (l < name.Length){
                        l += file_stream.Read(name, l, name.Length - l);
                        Thread.Sleep(1);
                    }

                    size = new byte[4];

                    file_stream.Read(size, 0, 4);

                    byte[] arr = new byte[BitConverter.ToInt32(size, 0)];
                    l = file_stream.Read(arr, 0, arr.Length);

                    while (l < arr.Length)
                    {
                        l += file_stream.Read(arr, l, arr.Length - l);
                        Thread.Sleep(1);
                    }

                    if (AllowReadFile(arr.Length)){
                        ListDataFile.Add(new DataFile(arr, Encoding.Unicode.GetString(name)));
                        this.Dispatcher.Invoke(() => { ListFile.Items.Add(Encoding.Unicode.GetString(name) + " : " + arr.Length.ToString()); });
                    }
                    else MessageBox.Show("Buffer fill!");

                }
            }
            catch { }
        }
        public bool AllowReadFile(int size)
        {
            int suma = 0;
            for (int i = 0; i < ListDataFile.Count; ++i)
                suma += ListDataFile[i].buffer.Length;
            return suma + size < 1104647000 ? true : false;
        }
        private void SendFile(byte[] arr, string name, NetworkStream r = null)
        {
            try
            {
                if (AllowReadFile(arr.Length) && arr.Length <= 50000000)
                {
                    byte[] buffer = Packet(arr, name);
                    ListDataFile.Add(new DataFile(arr, name));
                    file_stream.Write(buffer, 0, buffer.Length);
                    this.Dispatcher.Invoke(() => { ListFile.Items.Add(name + " : " + arr.Length.ToString()); });

                }
                else MessageBox.Show("Buffer FILL / File long(max 50 mb)");
            }
            catch { }
        }
        private byte[] Packet(byte[] arr, string name)
        {
            byte[] byte_name = Encoding.Unicode.GetBytes(name);
            byte[] size_name = BitConverter.GetBytes(byte_name.Length);
            byte[] size_arr = BitConverter.GetBytes(arr.Length);
            byte[] buffer = new byte[8 + arr.Length + byte_name.Length];
            Array.Copy(size_name, 0, buffer, 0, size_name.Length);
            Array.Copy(byte_name, 0, buffer, size_name.Length, byte_name.Length);
            Array.Copy(size_arr, 0, buffer, size_name.Length + byte_name.Length, size_arr.Length);
            Array.Copy(arr, 0, buffer, size_name.Length + size_arr.Length + byte_name.Length, arr.Length);
            return buffer;
        }
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(ConfigurationSettings.AppSettings["Href"].ToString());
        }
        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("explorer", AppDomain.CurrentDomain.BaseDirectory + @"File\");
        }
        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            Task savefile = new Task(new Action(delegate ()
            {
                this.Dispatcher.Invoke(() => {
                    progress_bar.Maximum = ListDataFile.Count;
                    progress_bar.Value = progress_bar.Minimum = 0;
                });
                for (int i = 0; i < ListDataFile.Count; ++i)
                {
                    if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + @"File\" + DateTime.Now.ToLongDateString()))
                        Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + @"File\" + DateTime.Now.ToLongDateString());
                    File.WriteAllBytes(AppDomain.CurrentDomain.BaseDirectory + @"File\" + DateTime.Now.ToLongDateString() + @"\" + ListDataFile[i].name, ListDataFile[i].buffer);
                    this.Dispatcher.Invoke(() => { ++progress_bar.Value; });
                }
                this.Dispatcher.Invoke(() => {
                    progress_bar.Maximum =1;
                    progress_bar.Value = progress_bar.Minimum = 0;
                });
            }));

            this.Dispatcher.Invoke(() =>
            {
                TextBoxChatClient.Text += "Files saving..." + Environment.NewLine;
            });
            savefile.Start();
            this.Dispatcher.Invoke(() =>
            {
                TextBoxChatClient.Text += "Files saved!" + Environment.NewLine;
            });
        }
        private void Rectangle_MouseUp(object sender, MouseButtonEventArgs e)
        {
            m_up = true;
            change_w = false;
        }
        private void Rectangle_MouseMove(object sender, MouseEventArgs e)
        {
            if (!m_up)
                Dispatcher.Invoke(() =>
                {
                    this.Left = System.Windows.Forms.Cursor.Position.X - X_M;
                    this.Top = System.Windows.Forms.Cursor.Position.Y - Y_M;
                });
            else m_up = true;
        }
        private void Window_Closing_1(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                if (connect)
                {
                    byte[] type = new byte[1] { 2 };
                    byte[] arr = new byte[1];
                    SendDataChat(arr, type);
                }
            }
            catch { }
            T_Receive_Video?.Abort();
            in_thread?.Abort();
        }
        private void menu_grid_MouseEnter(object sender, MouseEventArgs e)
        {
            if (!HideRectangle)
            menu_grid.Opacity = 1;
        }
        private void menu_grid_MouseLeave(object sender, MouseEventArgs e)
        {
            if(!HideRectangle)
                menu_grid.Opacity = 0;
        }
        private void Button_MouseEnter(object sender, MouseEventArgs e)
        {
            (sender as Button).Opacity = 1;
            (sender as Button).Background = Brushes.Green;
            (sender as Button).Foreground = Brushes.Black;
           
        }
        private void Button_MouseLeave(object sender, MouseEventArgs e)
        {
            (sender as Button).Opacity = 0.8;
            (sender as Button).Background = Brushes.White;
            (sender as Button).Foreground = Brushes.Green;
            
        }
        private void Rectangle_MouseDown_1(object sender, MouseButtonEventArgs e)
        {

            X_M = System.Windows.Forms.Cursor.Position.X - (int)this.Left;
            Y_M = System.Windows.Forms.Cursor.Position.Y - (int)this.Top;
            change_w = true;
            m_up = false;
            move_form = new Thread(new ThreadStart(delegate () { MoveForm(ref e); }));
            move_form.Start();
        }
        private void MoveForm(ref MouseButtonEventArgs e)
        {
            while (e.ButtonState == MouseButtonState.Pressed)
            {
                this.Dispatcher.Invoke(() => {
                    this.Left = System.Windows.Forms.Cursor.Position.X - X_M;
                    this.Top = System.Windows.Forms.Cursor.Position.Y - Y_M;
                });
            }
            move_form.Abort();
        }
        private void Label_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if ((string)(sender as Label).Content == "x")
            {
                if (connect)
                {
                    byte[] type = new byte[1] { 2 };
                    byte[] arr = new byte[1];
                    SendDataChat(arr, type);
                }
                T_Receive_Video?.Abort();
                this.receiver?.Close();
                output?.Stop();
                in_thread?.Abort();
                this.Close();
            }
            else if ((string)(sender as Label).Content == "<>")
            {
                this.WindowState = WindowState.Maximized;
                (sender as Label).FontSize = exit_l.FontSize = hide_l.FontSize = 15;

                (sender as Label).Content = "><><";

            }
            else if ((string)(sender as Label).Content == "><><")
            {
                this.WindowState = WindowState.Normal;
                (sender as Label).FontSize = exit_l.FontSize = hide_l.FontSize = 7;
                (sender as Label).Content = "<>";
            }
            else if ((string)(sender as Label).Content == "^") this.WindowState = WindowState.Minimized;
        }
        private void r_audio_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (microfone)
            {
                microfone = false;
                r_audio.Fill = m_not;
                // input.StopRecording();
            }
            else
            {
                microfone = true;
                r_audio.Fill = m_yes;
                // input.StartRecording();
            }
        }
        public void StartMicrofone() {
            try{
                output = new WaveOut();
                bufferStream = new BufferedWaveProvider(new WaveFormat(8000,32,2));
                output.Init(bufferStream);               
                in_thread = new Thread(new ThreadStart(ListeningMicrofone));
                in_thread.Start();
            }
            catch (Exception s) { }
        }
        private void ListeningMicrofone(){
            try{
                receiver = new TcpClient();
                receiver.Connect(MyIp, port_microfone);
                remoteIp = receiver.GetStream();
                output.Play();
                while (true){
                    try{
                        byte[] data = new byte[65000];
                        int size=remoteIp.Read(data,0,data.Length);
                        if (microfone)
                            bufferStream.AddSamples(data, 0, size);
                    }
                    catch (SocketException ex)
                    {  }
                }
            }
            catch(Exception s) { receiver?.Close(); }
        }
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool AllocConsole();
        private void Window_Drop(object sender, DragEventArgs e)
        {
            string[] name_files = (string[])e.Data.GetData(DataFormats.FileDrop);
            this.Dispatcher.Invoke(() => {
                foreach (string file in name_files)
                {
                    Task readFile = new Task(new Action(delegate () {
                        SendFile(File.ReadAllBytes(file), System.IO.Path.GetFileName(file));
                    }));
                    readFile.Start();
                }
            });
        }

        private void Window_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, false) == true)
                e.Effects = DragDropEffects.All;
        }

    }
}
