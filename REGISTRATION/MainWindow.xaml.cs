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
using System.Net;
using System.Configuration;
using System.ComponentModel;

namespace REGISTRATION{
    public partial class MainWindow : Window{
        string IP;
        public MainWindow(){
            InitializeComponent();
            IP = Dns.GetHostByName(Dns.GetHostName()).AddressList[0].ToString();
            Uri iconUri = new Uri(AppDomain.CurrentDomain.BaseDirectory + @"\Image\ico.ico", UriKind.Relative);
            this.Icon = BitmapFrame.Create(iconUri);
            client_b.TextDecorations = TextDecorations.Underline;
            string name = "";
            for (int i = 0, j=0; i < IP.Length; ++i) {
                if (IP[i] == '.') ++j;
                else if (j >= 3)
                    name += IP[i];
            }
            name_textbox.Text = "Client" + name;
            if(Dns.GetHostByName(Dns.GetHostName()).AddressList.Count()==1)
                if(Dns.GetHostByName(Dns.GetHostName()).AddressList[0].ToString() != "127.0.0.1")
                combobox1.Items.Add("127.0.0.1");
            foreach (var a in Dns.GetHostByName(Dns.GetHostName()).AddressList) {
                combobox1.Items.Add(a.ToString());
            }
            combobox1.SelectedIndex = 0;
        }
        private void Label_MouseDoubleClick(object sender, MouseButtonEventArgs e){
            this.Close();
        }
        private void Label_MouseDown(object sender, MouseButtonEventArgs e){
            this.WindowState = WindowState.Minimized;
        }
        private void Label_MouseEnter(object sender, MouseEventArgs e){
            (sender as Label).FontWeight = FontWeights.Bold;
        }
        private void Label_MouseLeave(object sender, MouseEventArgs e){
            (sender as Label).FontWeight = FontWeights.Normal;
        }
        private void client_b_MouseDown(object sender, MouseButtonEventArgs e){
            if ((sender as TextBlock).Text == "Client"){
                client_b.TextDecorations = TextDecorations.Underline;
                boss_b.TextDecorations = null;
                name_textbox.Text =  "";
                string name = "";
                for (int i = 0, j = 0; i < IP.Length; ++i){
                    if (IP[i] == '.')
                        ++j;
                    else if (j >= 3)
                        name += IP[i];
                }
                this.Dispatcher.Invoke(() => {
                    name_textbox.Text = "Client" + name;
                });
            }
            else {
                boss_b.TextDecorations = TextDecorations.Underline;
                client_b.TextDecorations = null;
                combobox1.SelectedItem = IP;
                name_textbox.Text = "Server";
            }
        }
        private void Label_MouseDown_1(object sender, MouseButtonEventArgs e){
            LabelStart.IsEnabled = false;
            if (combobox1.SelectedItem == null) {
                combobox1.Items.Add(combobox1.Text);
                combobox1.SelectedIndex = combobox1.Items.Count - 1;
            }

            int port_v = 0, port_c = 0, port_f = 0, port_m = 0;
            port_v = Int32.Parse(ConfigurationSettings.AppSettings["port_v"]);
            port_c = Int32.Parse(ConfigurationSettings.AppSettings["port_c"]);
            port_f = Int32.Parse(ConfigurationSettings.AppSettings["port_f"]);
            port_m = Int32.Parse(ConfigurationSettings.AppSettings["port_m"]);
            try{
                bool checed = !(bool)HideMenuChecked.IsChecked;
                if (combobox1.SelectedItem.ToString() != "" && ip_perevirka() && name_textbox.Text != ""){
                    if (client_b.TextDecorations == TextDecorations.Underline){
                        ClientPresentation.MainWindow client = new ClientPresentation.MainWindow();
                        client.Icon = this.Icon;
                        client.StartProgram(combobox1.SelectedItem.ToString(), name_textbox.Text, mail_textbox.Text, port_v, port_c, port_f, port_m,checed);
                        client.Show();
                        this.Close();
                    }
                    else{
                        Cursova.MainWindow boss = new Cursova.MainWindow();
                        boss.Icon = this.Icon;
                        boss.Show();
                        boss.StartProgram(name_textbox.Text, combobox1.SelectedItem.ToString(), port_v, port_c, port_f, port_m, mail_textbox.Text,checed);
                        this.Close();
                    }
                }
                else {
                    if (!ip_perevirka()){
                        this.Dispatcher.Invoke(() => {
                            combobox1.Background = Brushes.Red;                            
                        });
                    }
                    else{
                        this.Dispatcher.Invoke(() => { combobox1.Background = Brushes.Green; });
                    }
                    if (name_textbox.Text == ""){
                        this.Dispatcher.Invoke(() => { name_textbox.Background = Brushes.Red; });
                    }
                    else{
                        this.Dispatcher.Invoke(() => { name_textbox.Background = Brushes.Green; });
                    }
                }               
            }
            catch (Exception s) { MessageBox.Show(s.Message);  }
            LabelStart.IsEnabled = true;
        }
        private bool ip_perevirka() {
            int j = 0;
            string per = "";
            this.Dispatcher.Invoke(()=> {
                for (int i = 0; i < combobox1.SelectedItem.ToString().Length; ++i) {
                    if (combobox1.SelectedItem.ToString()[i] == '.') {
                       
                        ++j;
                    }
                    if (combobox1.SelectedItem.ToString()[i] == '.' || i== combobox1.SelectedItem.ToString().Length-1) {
                        if (per.Length<=3 && per.Length>=0 )
                            per = "";
                        else { j = 0; break; }
                    }
                    else if (combobox1.SelectedItem.ToString()[i] < '0' || combobox1.SelectedItem.ToString()[0] > '9'){
                        j = 0;
                        break;
                    }
                    else per += combobox1.SelectedItem.ToString()[i];
                }        
            });          
            return j == 3 ? true : false ;
        }
    }
}
