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
using System.Reflection;
using System.Net.Mail;
using System.Configuration;

using AForge.Video;
using AForge.Video.FFMPEG;
using NAudio.Wave;
using NAudio.CoreAudioApi;

namespace Cursova
{
    public partial class MainWindow : Window
    {
        class DataFile {
            public byte[] buffer;
            public string name;
            public DataFile(byte[]b,string n) {
                buffer = b;
                name = n;
            }
        }
        ImageBrush stop  = new ImageBrush(new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + @"\Image\stop.png")))  { Stretch= Stretch.Uniform};
        ImageBrush start = new ImageBrush(new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + @"\Image\start.png"))) { Stretch = Stretch.Uniform };
        ImageBrush m_not = new ImageBrush(new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + @"\Image\m_not.png"))) { Stretch = Stretch.Uniform};
        ImageBrush m_yes = new ImageBrush(new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + @"\Image\m_yes.png"))) { Stretch = Stretch.Uniform };
        string MyIp = Dns.GetHostByName(Dns.GetHostName()).AddressList[0].ToString();
        string mail_boss = "";
        string name="";
        bool stan = false;
        bool connect = false;
        bool savephoto = false;
        bool change_w = false,m_up=true;
        bool microfone = true;
        bool video_save = false;
        bool connecting = false;
        bool HideRectangle = false;
        VideoFileWriter vFWriter = new VideoFileWriter();
        Thread T_Scrindesctop;
        Thread move_form;
        Task Timer;
        Task GetClient;
        Task GetClient_chat;
        Task ChangeL=null;
        Task listen_microfone;
        Task SendMicrofone;
        TcpListener server;
        TcpListener chat_server;
        TcpListener file_server;
        TcpListener client;
        List<NetworkStream> ClientsStream = new List<NetworkStream>();
        List<NetworkStream> ClientChatStream = new List<NetworkStream>();
        List<NetworkStream> FileStream = new List<NetworkStream>();
        List<DataFile> ListDataFile = new List<DataFile>();
        List<string> MailClient = new List<string>();
        List<System.Drawing.Bitmap> ImageFromVideo = new List<System.Drawing.Bitmap>();
        List<NetworkStream> listeningSocket = new List<NetworkStream>();
        int port = 8333, port_chat = 0, port_file = 0,port_microfone=0;
        int coun_photo = 0;
        int rate = 0;
        int X_M, Y_M;
        const int maxFps = 25;
        const int skipTickfps = 1000 / maxFps;
        object locer = new object();
        WaveIn input;
        UdpClient sender;
        IPEndPoint endPoint;

        public MainWindow() {
            InitializeComponent();
            T_Scrindesctop = new Thread(new ThreadStart(ScrinDesctop));
            Rectangle1.Fill = start;
            r_audio.Fill = m_yes;
            m_l.Content = "<>";
            photodesctop.Background = new ImageBrush(new BitmapImage(new Uri(AppDomain.CurrentDomain.BaseDirectory + @"\Image\bacground.jpg"))) { Stretch = Stretch.Fill };
        }
        public void StartProgram(string name,string IP,int port,int port_c,int port_f,int port_m,string mail_boss,bool HideRectangle) {
            try{
                if (!connecting){
                    this.HideRectangle = HideRectangle;
                    if (HideRectangle)
                    {
                        menu_grid.Opacity = 1;
                        buttondesctop.Opacity = 0.8;
                    }
                    connecting = true;
                    progress_bar.Minimum = progress_bar.Value =0;
                    progress_bar.Maximum = 4;
                    ConnectClient(IP, port);
                    if (connect)
                    {
                        Timer = new Task(new Action(Tick));
                        this.name = name;
                        this.MyIp = IP;
                        Timer.Start();
                        info_1.ToolTip = ConfigurationSettings.AppSettings["Name"] + Environment.NewLine + ConfigurationSettings.AppSettings["Mail"];
                        port_chat = port_c;
                        port_file = port_f;
                        port_microfone = port_m;
                        StartMicrofone();
                        this.mail_boss = mail_boss;
                        if (mail_boss != "")
                            MailClient.Add(mail_boss);
                        StartChat();
                        StartFile();
                        this.Dispatcher.Invoke(() => {
                            TextBoxChatClient.Text += "  #Microfone -> true\n";
                            progress_bar.Value = progress_bar.Minimum = 0;
                            progress_bar.Maximum = 1;
                        });
                        
                    }
                }
            }
            catch (Exception s) { MessageBox.Show("Server : "+ s.Message); }
        }
        private void r_MouseLeave(object sender, MouseEventArgs e){
            if (!HideRectangle)
            {
                foreach (var a in buttondesctop.Children)
                    try { (a as Rectangle).Opacity = 0; } catch { try { (a as ListBox).Opacity = 0; } catch { (a as TextBlock).Opacity = 0; } }
                buttondesctop.Opacity = 0;
            }
        }
        private void r_MouseEnter(object sender, MouseEventArgs e){
            if (! HideRectangle) {
                foreach (var a in buttondesctop.Children)
                    try { (a as Rectangle).Opacity = 0.8; } catch { try { (a as ListBox).Opacity = 0.8; } catch { (a as TextBlock).Opacity = 0.8; } }
                buttondesctop.Opacity = 0.8;
            }
        }
        private void Rectangle_MouseDown(object sender, MouseButtonEventArgs e){
            if (!stan){
                Rectangle1.Fill = stop;
            }
            else {
                Rectangle1.Fill = start;
            }
            stan = !stan;
        }
        private BitmapImage ConvertToBitmapImage(System.Drawing.Bitmap bitmap){               
            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            ms.Seek(0, System.IO.SeekOrigin.Begin);
            image.StreamSource = ms;
            image.EndInit();           
            return image;
        }
        private void ScrinDesctop(){
            System.Drawing.Pen p = new System.Drawing.Pen(System.Drawing.Color.White, 3);
            vFWriter.Open(AppDomain.CurrentDomain.BaseDirectory + @"File\lesson.avi", (int)System.Windows.SystemParameters.PrimaryScreenWidth, (int)System.Windows.SystemParameters.PrimaryScreenHeight,8, VideoCodec.MSMPEG4v3);
            int nextTickfps = Environment.TickCount;
            while (true){
                if (stan){
                    System.Drawing.Bitmap scrin;
                    ImageBrush desctopphoto;
                    scrin = new System.Drawing.Bitmap((int)System.Windows.SystemParameters.PrimaryScreenWidth, (int)System.Windows.SystemParameters.PrimaryScreenHeight);
                    System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(scrin);                    
                    this.Dispatcher.Invoke(() =>{
                        graphics.CopyFromScreen(0, 0, 0, 0, scrin.Size);
                        graphics.DrawEllipse(p, System.Windows.Forms.Cursor.Position.X, System.Windows.Forms.Cursor.Position.Y, 5, 5);
                        desctopphoto = new ImageBrush(ConvertToBitmapImage(scrin));
                        photodesctop.Background = desctopphoto;                       
                    });
                    if (Environment.TickCount > nextTickfps){
                        if (!video_save)
                            vFWriter.WriteVideoFrame(scrin);
                        SendScrinClients(scrin);
                        nextTickfps += skipTickfps;
                    }    
                    if (savephoto){
                        Task.Run(new Action(delegate () {
                            if(!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + @"File\" + DateTime.Now.ToLongDateString()))
                                Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory+@"File\"+DateTime.Now.ToLongDateString());
                            scrin.Save(AppDomain.CurrentDomain.BaseDirectory + @"File\"+ DateTime.Now.ToLongDateString()+@"\" +DateTime.Now.Minute.ToString()+"min - "+(DateTime.Now.Millisecond/100).ToString()+" sec = "+ coun_photo.ToString() + ".jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                            ++coun_photo;
                        }));
                        Thread.Sleep(1);
                        savephoto = false;
                    }
                }
                Thread.Sleep(10);
            }
        }
        private void SendScrinClients(System.Drawing.Bitmap bitmap){
            if (connect) {
                MemoryStream stream = new MemoryStream();
                bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Jpeg);
                byte[] buffer = stream.ToArray();
                byte[] bytes = new byte[4 + buffer.Length];
                Array.Copy(BitConverter.GetBytes(buffer.Length), 0, bytes, 0, 4);
                Array.Copy(buffer, 0, bytes, 4, buffer.Length);
                this.Dispatcher.Invoke(() => { this.Title = buffer.Length.ToString(); });
                for (int i = 0; i < ClientsStream.Count; ++i) {
                    try {
                        ClientsStream[i].Write(bytes, 0, bytes.Length);
                    }
                    catch (Exception s){
                        Console.WriteLine(s.Message);
                    }
                }
            }        
        }
        private void Tick(){
            DateTime start = DateTime.Now;
            while (true){
                try
                {
                    this.Dispatcher.Invoke(
                        () => { info_1.Text = "-> " + (DateTime.Now - start).ToString().Remove(8, 8) + " : client " + info_2.Items.Count.ToString() + Environment.NewLine + "-> " + MyIp + " : " + port.ToString(); });
                    Thread.Sleep(1000);
                    TextBoxChatClient.SelectionStart = TextBoxChatClient.Text.Length;
                    TextBoxChatClient.ScrollToEnd();
                }
				catch (Exception s) {
					Console.WriteLine(s.Message);
				}
			}
        }
        private void ConnectClient(string IP,int port){
            try{                
                server = new TcpListener(IPAddress.Parse(IP), port);              
                server.Start();
                this.Dispatcher.Invoke(()=> {
                    TextBoxChatClient.Text += "Status connecting:\n  #Video[" + port.ToString() + "] -> true\n";
                    ++progress_bar.Value;
                });
                GetClient = new Task(new Action(ListenGetCLient));
                GetClient.Start();
                T_Scrindesctop.Start();               
                connect = true;
                this.port = port;
            }
            catch(Exception s) { connect= false; this.Dispatcher.Invoke(() => {
                TextBoxChatClient.Text += "Status connecting:\n Video[" + port.ToString() + "] -> false";
                progress_bar.Maximum = 1;
                progress_bar.Value =progress_bar.Minimum =0 ;
                MessageBox.Show(s.Message);
            });
            }            
        }
        private void ListenGetCLient(){
            while (true){
                TcpClient client = server.AcceptTcpClient();
                client.SendBufferSize = client.ReceiveBufferSize = 500000;                
                ClientsStream.Add(client.GetStream());
                byte[] data = new byte[256];
                StringBuilder response = new StringBuilder();
                NetworkStream stream = ClientsStream[ClientsStream.Count - 1];
                do{
                    int bytes = ClientsStream[ClientsStream.Count - 1].Read(data, 0, data.Length);
                    response.Append(Encoding.Unicode.GetString(data, 0, bytes));
                }
                while (stream.DataAvailable);
                string name = "", mail = "";
                int j =response.ToString().IndexOf("<&&>");
                for (int i = 0; i < response.ToString().Length; ++i) {
                    if (j > i){
                        name += response.ToString()[i];
                    }
                    else if (j+3<  i) {
                        mail += response.ToString()[i];
                    }
                }
                if(mail!="")
                    MailClient.Add(mail);
                var pi = ClientsStream[ClientsStream.Count - 1].GetType().GetProperty("Socket", BindingFlags.NonPublic | BindingFlags.Instance);
                var socketIp = ((Socket)pi.GetValue(ClientsStream[ClientsStream.Count - 1], null)).RemoteEndPoint.ToString();
                this.Dispatcher.Invoke(() =>
                {
                    info_2.Items.Add(name + " -> " + socketIp);
                    TextBoxChatClient.Text += "Connect -> " + name + "\n";
                });
                byte[] type = new byte[1] { 3 };
                byte[] messange = Encoding.Unicode.GetBytes($"{name} -> {socketIp.ToString()}");
                SendDataChat(null, messange, type,true);
                Thread.Sleep(10);
            } 
        } 
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e){
            byte[] type = new byte[1] { 2};
            byte[] messange = Encoding.Unicode.GetBytes("BOSS disconected!");
            SendDataChat(null,messange,type);
            T_Scrindesctop?.Abort();
        }
        private void StartChat(){
            try
            {
                chat_server = new TcpListener(IPAddress.Parse(MyIp), port_chat);
                chat_server.Start();
                this.Dispatcher.Invoke(() => {
                    TextBoxChatClient.Text += "  #Chat -> true\n";
                    ++progress_bar.Value;
                });         
            GetClient_chat = new Task(new Action(ListenGetClientChat));
            GetClient_chat.Start();
            }
            catch(Exception s) {
                this.Dispatcher.Invoke(() => {
                    TextBoxChatClient.Text += "  #Chat -> false\n";
                    ++progress_bar.Value;
                });
                MessageBox.Show(s.Message);
            }
            
        }
        public void ListenGetClientChat() {            
            while (true) {
                try{
                    TcpClient client = chat_server.AcceptTcpClient();
                    client.SendBufferSize = client.ReceiveBufferSize = int.MaxValue;
                    ClientChatStream.Add(client.GetStream());
                    byte[] clients_type = new byte[1] { 5 };
                    this.Dispatcher.Invoke(() => {
                        for (int i = 0; i < info_2.Items.Count; ++i){
                            byte[] name = Encoding.Unicode.GetBytes(info_2.Items[i].ToString());
                            byte[] size = BitConverter.GetBytes(name.Length);
                            byte[] buffer = new byte[1+name.Length+size.Length];
                            Array.Copy(clients_type,0,buffer,0,1);
                            Array.Copy(size, 0, buffer, 1, size.Length);
                            Array.Copy(name, 0, buffer, 1+size.Length, name.Length);
                            client.GetStream().Write(buffer, 0,buffer.Length);
                        }
                    });
                    Task go = new Task(new Action(delegate() { ReceiveDataChat(); }));
                    go.Start();
                    Thread.Sleep(10);
				}
				catch (Exception s) {
					Console.WriteLine(s.Message);
				}
			}            
        }
        public void ReceiveDataChat() {
            NetworkStream stream = ClientChatStream[ClientChatStream.Count-1];            
            try{
                while (true){
                    byte[] type = new byte[1];
                    byte[] size = new byte[4];
                    stream.Read(type,0,1);
                    if (type[0] == 2){
                        for (int i = 0; i < ClientChatStream.Count; ++i)
                            if (ClientChatStream[i] == stream){
                                type = new byte[1] { 4 };
                                ClientChatStream[i].Close();
                                ClientChatStream.RemoveAt(i);
                                FileStream[i].Close();
                                FileStream.RemoveAt(i);
                                ClientsStream[i].Close();
                                ClientsStream.RemoveAt(i);
                                SendDataChat(null, Encoding.Unicode.GetBytes(info_2.Items[i].ToString()), type);
                                this.Dispatcher.Invoke(() => {
                                    TextBoxChatClient.Text += "Disconect -> " + info_2.Items[i]+Environment.NewLine;
                                    info_2.Items.RemoveAt(i--);
                                });                               
                            }
                    }
                    else{
                        stream.Read(size, 0, 4);
                        Thread.Sleep(1);
                        byte[] buffer = new byte[BitConverter.ToInt32(size, 0)];
                        int l = stream.Read(buffer, 0, buffer.Length);
                        while (l < buffer.Length) {
                            l += stream.Read(buffer, l, buffer.Length - l);
                            Thread.Sleep(1);
                        }
                        this.Dispatcher.Invoke(() => { TextBoxChatClient.Text += Encoding.Unicode.GetString(buffer) + Environment.NewLine;
                            TextBoxChatClient.SelectionStart = TextBoxChatClient.Text.Length;
                            TextBoxChatClient.ScrollToEnd();
                        });
                        SendDataChat(stream, buffer, type);
                    }
                    Thread.Sleep(10);                    
                }
            }
			catch (Exception s) {
				Console.WriteLine(s.Message);
			}
		}
        public void SendDataChat(NetworkStream r,byte[] arr,byte[]type,bool t=false){
            byte[] buffer = new byte[5 + arr.Length];
            Array.Copy(type,0,buffer,0,1);
            Array.Copy(BitConverter.GetBytes(arr.Length),0,buffer,1,4);
            Array.Copy(arr,0,buffer,5,arr.Length);
            for (int i = 0; i < (t==true?ClientChatStream.Count-1: ClientChatStream.Count); ++i) {
                try{
                    if (ClientChatStream[i] != r)
                        ClientChatStream[i].Write(buffer, 0, buffer.Length);
                }
                catch { }
            }
        }
        private void TextBoxYou_KeyUp(object sender, KeyEventArgs e){         
            byte[] type = new byte[1] { 1 };
            if (e.Key == Key.Enter && e.KeyboardDevice.Modifiers == ModifierKeys.Shift)
                this.Dispatcher.Invoke(()=> { TextBoxYou.Text += Environment.NewLine; });              
            else if (e.Key == Key.Enter){
                string text = "";
                this.Dispatcher.Invoke(()=> {text=name+" -> "+ TextBoxYou.Text; });
                SendDataChat(null, Encoding.Unicode.GetBytes(text), type);
                this.Dispatcher.Invoke(()=> {
                    TextBoxChatClient.Text += text = name + " -> " + TextBoxYou.Text + Environment.NewLine;
                    TextBoxYou.Text = "";
                });                                          
            }
            this.Dispatcher.Invoke(() => { TextBoxYou.SelectionStart = TextBoxYou.Text.Length; });
            this.Dispatcher.Invoke(() => { TextBoxChatClient.SelectionStart = TextBoxChatClient.Text.Length;
                TextBoxChatClient.ScrollToEnd();
            });
        }
        private void Window_KeyDown(object sender, KeyEventArgs e){
            if (this.WindowState != WindowState.Maximized && e.Key == Key.F11){
                this.WindowState = WindowState.Maximized;
                m_l.FontSize = exit_l.FontSize = hide_l.FontSize = 15;
            }
            else if (this.WindowState == WindowState.Maximized && e.Key == Key.F11){
                this.WindowState = WindowState.Normal;
                m_l.FontSize = exit_l.FontSize = hide_l.FontSize = 7;
            }
            else if (e.Key == Key.PrintScreen)
                savephoto = true;
        }
        private void Window_KeyUp(object sender, KeyEventArgs e){            
        }
        private void StartFile() {
            try
            {
                file_server = new TcpListener(IPAddress.Parse(MyIp), port_file);
                file_server.Start();
                this.Dispatcher.Invoke(() => {
                    TextBoxChatClient.Text += "  #File -> true\n";
                    ++progress_bar.Value;
                });
                Task listen = new Task(new Action(delegate() { ListenFile(); }));
                listen.Start();
            }
            catch (Exception s) {
                this.Dispatcher.Invoke(() => {
                    TextBoxChatClient.Text += "  #File -> false\n";
                    ++progress_bar.Value;
                });
                MessageBox.Show(s.Message);
            }
            
        }
        private void Button_Click(object sender, RoutedEventArgs e){
            try{
                System.Windows.Forms.OpenFileDialog open = new System.Windows.Forms.OpenFileDialog();
                if (System.Windows.Forms.DialogResult.OK == open.ShowDialog()){
                    Task readFile = new Task(new Action(delegate() { SendFile(File.ReadAllBytes(open.FileName), System.IO.Path.GetFileName(open.FileName)); }));
                    readFile.Start();
                }
            }
			catch (Exception s) {
				Console.WriteLine(s.Message);
			}
		}
        private void ListFile_MouseDoubleClick(object sender, MouseButtonEventArgs e){
            if (ListFile.Items.Count > 0 && ListFile.SelectedIndex != null){
                Task savefile = new Task(new Action(delegate () {
                    int index = 0;
                    this.Dispatcher.Invoke(() => { index = ListFile.SelectedIndex; });
                    if (index != null){
                        if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + @"File\" + DateTime.Now.ToLongDateString()))
                            Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + @"File\" + DateTime.Now.ToLongDateString());
                        File.WriteAllBytes(AppDomain.CurrentDomain.BaseDirectory + @"File\" + DateTime.Now.ToLongDateString() + @"\" + ListDataFile[index].name, ListDataFile[index].buffer);
                        Dispatcher.Invoke(()=> { TextBoxChatClient.Text += "Saved : " + ListDataFile[index].name+Environment.NewLine; });
                    }
                }));
                savefile.Start();
            }
        }
        public void ListenFile() {
            while (true) {
                TcpClient client = file_server.AcceptTcpClient();               
                client.ReceiveBufferSize = client.SendBufferSize = 50001000;                
                FileStream.Add(client.GetStream());
                FileStream[FileStream.Count - 1].ReadTimeout = FileStream[FileStream.Count - 1].ReadTimeout = 60000 ;
                Task rec_client = new Task(new Action(delegate() {
                    ReceiveFile();
                }));
                rec_client.Start();
                Thread.Sleep(10);
            }
        }
        public void ReceiveFile(){
            NetworkStream stream = FileStream[FileStream.Count-1];           
            while (true) {
                try{
                    byte[] size = new byte[4];
                    stream.Read(size, 0, 4);
                    byte[] name = new byte[BitConverter.ToInt32(size, 0)];
                    int l = stream.Read(name, 0, name.Length);
                    while (l < name.Length){
                        l += stream.Read(name, l, name.Length - l);
                        Thread.Sleep(1);
                    }
                    size = new byte[4];
                    stream.Read(size, 0, 4);
                    byte[] arr = new byte[BitConverter.ToInt32(size, 0)];
                    l = stream.Read(arr, 0, arr.Length);
                    while (l < arr.Length){
                        l += stream.Read(arr, l, arr.Length - l);
                        Thread.Sleep(1);
                    }
                    SendFile(arr, Encoding.Unicode.GetString(name), stream);                    
                }
				catch (Exception s) {
					Console.WriteLine(s.Message);
				}
				Thread.Sleep(10);
            }
        }
        public bool AllowReadFile(int size){
            int suma = 0;
            for (int i = 0; i < ListDataFile.Count; ++i)
                suma += ListDataFile[i].buffer.Length;
            return suma + size < 1104647000 ? true : false;
        }
        private void SendFile(byte[] arr, string name, NetworkStream r = null){
            if (AllowReadFile(arr.Length) && arr.Length <= 50000000){
                byte[] buffer = Packet(arr, name);
                ListDataFile.Add(new DataFile(arr, name));
                for (int i = 0; i < FileStream.Count; ++i){
                    if (FileStream[i] != r)
                        try{
                            FileStream[i].Write(buffer, 0, buffer.Length);
                        }
						catch (Exception s) {
							Console.WriteLine(s.Message);
						}
				}
               this.Dispatcher.Invoke(() => { ListFile.Items.Add(name + " : " + arr.Length.ToString()); });
            }
            else MessageBox.Show("Buffer FILL / File long(max 50 mb)");
        }
        private byte[] Packet(byte[] arr, string name){
            byte[] byte_name = Encoding.Unicode.GetBytes(name);
            byte[] size_name = BitConverter.GetBytes(byte_name.Length);
            byte[] size_arr = BitConverter.GetBytes(arr.Length);
            byte[] buffer = new byte[8 + arr.Length + byte_name.Length];
            Array.Copy(size_name, 0, buffer, 0, size_name.Length);
            Array.Copy(byte_name, 0, buffer, size_name.Length, byte_name.Length);
            Array.Copy(size_arr, 0, buffer, size_name.Length + byte_name.Length, size_arr.Length);
            Array.Copy(arr, 0, buffer, size_name.Length+size_arr.Length + byte_name.Length, arr.Length);
            return buffer;
        }
        private void Button_Click_1(object sender, RoutedEventArgs e){
            if (ListDataFile.Count > 0){
                MessageBox.Show("List : " + ListDataFile.Count.ToString() + "\nMail:" + MailClient.Count.ToString());
                MailAddress from = new MailAddress(ConfigurationSettings.AppSettings["Mail"].ToString(), ConfigurationSettings.AppSettings["Name"].ToString());
                SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587);
                smtp.Timeout = 60000;
                if (ConfigurationSettings.AppSettings["Password"].ToString() == "") {
                    smtp.Credentials = new NetworkCredential(ConfigurationSettings.AppSettings["Mail"].ToString(), "333horoshenko333");
                }
                else
                    smtp.Credentials = new NetworkCredential(ConfigurationSettings.AppSettings["Mail"].ToString(), ConfigurationSettings.AppSettings["Password"].ToString());
                smtp.EnableSsl = true;
                int count = 0, error = 0;
                string errors = "";
                this.Dispatcher.Invoke(() => {
                    progress_bar.Maximum = MailClient.Count;
                    progress_bar.Minimum = 0;
                    progress_bar.Value = 0;
                });
                Task SendingMail = new Task(new Action(() =>{
                    for (int i = 0; i < MailClient.Count; ++i){
                        MailAddress to = new MailAddress(MailClient[i]);
                        MailMessage m = new MailMessage(from, to);
                        m.Subject = "Lesson -> " + DateTime.Now.ToLongDateString();
                        m.Body = "<h2>" + ConfigurationSettings.AppSettings["Name"].ToString() + "</h2>";
                        m.IsBodyHtml = true;
                        for (int j = 0; j < ListDataFile.Count; ++j){
                            Stream stream = new MemoryStream(ListDataFile[j].buffer);
                            m.Attachments.Add(new Attachment(stream, ListDataFile[j].name));
                        }
						try {
							smtp.Send(m);
							++count;
						}
						catch (Exception s) {
							Console.WriteLine(s.Message);
							++error;
							errors += MailClient[i] + Environment.NewLine;
						}
                        this.Dispatcher.Invoke(()=> { ++progress_bar.Value; });
                    }
                    this.Dispatcher.Invoke(() =>{
                        TextBoxChatClient.Text += "Mail sended : " + count.ToString() + " - error : " + error.ToString() + Environment.NewLine + errors;
                        progress_bar.Value = 0;
                        progress_bar.Maximum = 1;
                        progress_bar.Minimum = 0;
                    });
                }));
                this.Dispatcher.Invoke(() =>{
                    TextBoxChatClient.Text += "Sending mail files..." + Environment.NewLine;
                });
                SendingMail.Start();
            }
            else Dispatcher.Invoke(()=> { TextBoxChatClient.Text += "Not send mail but files :  0" + Environment.NewLine; });
        }
        private void Button_Click_2(object sender, RoutedEventArgs e){
            System.Diagnostics.Process.Start("explorer", AppDomain.CurrentDomain.BaseDirectory+@"File\");
        }
        private void Button_Click_3(object sender, RoutedEventArgs e){
            System.Diagnostics.Process.Start(ConfigurationSettings.AppSettings["Href"].ToString());
        }
        private void Button_Click_4(object sender, RoutedEventArgs e){
            Task savefile = new Task(new Action(delegate (){
                this.Dispatcher.Invoke(()=> {
                    progress_bar.Maximum = ListDataFile.Count;
                    progress_bar.Value = progress_bar.Minimum = 0;
                });
                for (int i = 0; i < ListDataFile.Count; ++i){
                    if (!Directory.Exists(AppDomain.CurrentDomain.BaseDirectory + @"File\" + DateTime.Now.ToLongDateString()))
                        Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory + @"File\" + DateTime.Now.ToLongDateString());
                    File.WriteAllBytes(AppDomain.CurrentDomain.BaseDirectory + @"File\" + DateTime.Now.ToLongDateString() + @"\" + ListDataFile[i].name, ListDataFile[i].buffer);
                    this.Dispatcher.Invoke(() => { ++progress_bar.Value; });
                }
                this.Dispatcher.Invoke(() => {
                    progress_bar.Maximum = 1;
                    progress_bar.Value = progress_bar.Minimum = 0;
                });
            }));
            this.Dispatcher.Invoke(() => {
                TextBoxChatClient.Text += "Files saving..." + Environment.NewLine;
            });
            savefile.Start();
            this.Dispatcher.Invoke(() => {
                TextBoxChatClient.Text += "File saved!" + Environment.NewLine;
            });
        }
        private void Label_MouseDown(object sender, MouseButtonEventArgs e){
            if ((string)(sender as Label).Content == "x") {
                byte[] type = new byte[1] { 2 };
                byte[] messange = Encoding.Unicode.GetBytes("Server -> disconected!");
                SendDataChat(null, messange, type);
                T_Scrindesctop?.Abort();
                input?.StopRecording();
                this.sender?.Close();
                this.Close();
            }
            else if ((string)(sender as Label).Content == "<>"){
                this.WindowState = WindowState.Maximized;
                (sender as Label).FontSize = exit_l.FontSize = hide_l.FontSize = 15;
                (sender as Label).Content = "><><";
            }
            else if ((string)(sender as Label).Content == "><><"){
                this.WindowState = WindowState.Normal;
                (sender as Label).FontSize = exit_l.FontSize = hide_l.FontSize = 7;
                (sender as Label).Content = "<>";
            }
            else if ((string)(sender as Label).Content == "^") this.WindowState = WindowState.Minimized;
        }
        private void menu_grid_MouseEnter(object sender, MouseEventArgs e){
            if(!HideRectangle)
            menu_grid.Opacity = 1;
        }
        private void menu_grid_MouseLeave(object sender, MouseEventArgs e){
            if(!HideRectangle)
            menu_grid.Opacity = 0;
        }
        private void ListFile_SelectionChanged(object sender, SelectionChangedEventArgs e){
        }
        private void Rectangle_MouseDown_1(object sender, MouseButtonEventArgs e){
            X_M = System.Windows.Forms.Cursor.Position.X - (int)this.Left;
            Y_M = System.Windows.Forms.Cursor.Position.Y- (int)this.Top;
            change_w = true;
            m_up = false;           
            move_form = new Thread(new ThreadStart(delegate() { MoveForm(ref e); }));
            move_form.Start();
        }
        private void r_audio_MouseDown(object sender, MouseButtonEventArgs e){
            if (microfone){
                microfone = false;
                r_audio.Fill = m_not;
                //input?.StopRecording();
            }
            else {
                microfone = true;
                r_audio.Fill = m_yes;
                //input?.StartRecording();
            }
        }
        private void StartMicrofone() {
            try
            {
                input = new WaveIn();
                input.DeviceNumber = 0;
                input.WaveFormat = new WaveFormat(8000, 32, 2);
                input.DataAvailable += Input_DataAvailable; ;
                this.Dispatcher.Invoke(() =>
                {
                    TextBoxChatClient.Text += "  #Microfone -> true\n";
                    ++progress_bar.Value;
                });
                sender = new UdpClient();
                endPoint = new IPEndPoint(IPAddress.Parse("233.233.233.233"), port_microfone);
                input.StartRecording();
            }
            catch (Exception s)
            {
                this.Dispatcher.Invoke(() =>
                {
                    TextBoxChatClient.Text += "  #Microfone -> false\n";
                    ++progress_bar.Value;
                });       
            }
            
        }
        private void Send_M() {
        }
        private void Input_DataAvailable(object sender, WaveInEventArgs e){
            Task.Run(()=> {
                try{
                    if (microfone)
                    {
                        this.sender.Send(e.Buffer, e.Buffer.Length, endPoint);
                    }

                }
				catch (Exception s) {
					Console.WriteLine(s.Message);
				}
			});
        }
        private void Button_MouseEnter(object sender, MouseEventArgs e){
            (sender as Button).Opacity = 1;
            (sender as Button).Background = Brushes.Green;
            (sender as Button).Foreground = Brushes.Black;
        }
        private void Button_MouseLeave(object sender, MouseEventArgs e){
            (sender as Button).Opacity = 0.8;
            (sender as Button).Background = Brushes.White;
            (sender as Button).Foreground = Brushes.Green;
        }
        private void Rectangle_MouseMove(object sender, MouseEventArgs e){
        }
        private void Window_Closing_1(object sender, System.ComponentModel.CancelEventArgs e){
            byte[] type = new byte[1] { 2 };
            byte[] messange = Encoding.Unicode.GetBytes("Server -> disconected!");
            SendDataChat(null, messange, type);
            T_Scrindesctop?.Abort();
        }
        private void Rectangle_MouseUp(object sender, MouseButtonEventArgs e){
            m_up = true;
            change_w = false;
        }
        private void MoveForm(ref MouseButtonEventArgs e) {
            while (e.ButtonState== MouseButtonState.Pressed){
                this.Dispatcher.Invoke(() =>{
                    this.Left = System.Windows.Forms.Cursor.Position.X - X_M;
                    this.Top = System.Windows.Forms.Cursor.Position.Y - Y_M;                    
                });            
            }
            move_form.Abort();
        }
        private void Button_Click_5(object sender, RoutedEventArgs e){
            if ((string)(sender as Button).Content == "Save video"){
                video_save = true;
                Task.Run(() =>{
                    vFWriter.Close();
                    Dispatcher.Invoke(() => { TextBoxChatClient.Text += "Video compiled!" + Environment.NewLine; });
                });
                (sender as Button).Content = "Send video mail";
            }
            else if ((string)(sender as Button).Content == "Send video mail"){
                if (true){
                    Task SendingMail = new Task(new Action(() =>{
                        this.Dispatcher.Invoke(() => {
                            progress_bar.Maximum = MailClient.Count;
                            progress_bar.Value = progress_bar.Minimum = 0;
                        });
                        MailAddress from = new MailAddress(ConfigurationSettings.AppSettings["Mail"].ToString(), ConfigurationSettings.AppSettings["Name"].ToString());
                        SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587);
                        smtp.Timeout = 60000;
                        if (ConfigurationSettings.AppSettings["Password"].ToString() == ""){
                            smtp.Credentials = new NetworkCredential(ConfigurationSettings.AppSettings["Mail"].ToString(), "333horoshenko333");
                        }
                        else
                            smtp.Credentials = new NetworkCredential(ConfigurationSettings.AppSettings["Mail"].ToString(), ConfigurationSettings.AppSettings["Password"].ToString());
                        smtp.EnableSsl = true;
                        int count = 0, error = 0;
                        string errors = "";
                        for (int i = 0; i < MailClient.Count; ++i){
                            MailAddress to = new MailAddress(MailClient[i]);
                            MailMessage m = new MailMessage(from, to);
                            m.Subject = "Lesson -> Video" + DateTime.Now.ToLongDateString();
                            m.Body = "<h2>" + ConfigurationSettings.AppSettings["Name"].ToString() + "</h2>";
                            m.IsBodyHtml = true;
                            Stream stream = new MemoryStream(File.ReadAllBytes(AppDomain.CurrentDomain.BaseDirectory + @"\File\lesson.avi"));
                            m.Attachments.Add(new Attachment(stream, "lesson.avi"));
                            try{
                                smtp.Send(m);
                                ++count;
                            }
							catch (Exception s) {
								Console.WriteLine(s.Message);
							++error;
								errors += MailClient[i] + Environment.NewLine;
							}
                            Dispatcher.Invoke(()=> { ++progress_bar.Value; });
                        }
                        this.Dispatcher.Invoke(() =>{
                            progress_bar.Maximum = 1;
                            progress_bar.Value = progress_bar.Minimum = 0;
                            TextBoxChatClient.Text += "Mail sended : " + count.ToString() + " - error : " + error.ToString() + Environment.NewLine + errors;
                        });
                    }));
                    this.Dispatcher.Invoke(() =>{
                        TextBoxChatClient.Text += "Sending mail files..." + Environment.NewLine;
                    });
                    SendingMail.Start();
                }
                (sender as Button).Content = "Receive new video";
            }
            else if ((string)(sender as Button).Content == "Receive new video") {
                vFWriter.Open(AppDomain.CurrentDomain.BaseDirectory + @"File\test.avi", (int)System.Windows.SystemParameters.PrimaryScreenWidth, (int)System.Windows.SystemParameters.PrimaryScreenHeight,8, VideoCodec.Default);
                video_save = false;
                (sender as Button).Content = "Save video";
            }
        }
    }
}