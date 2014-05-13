using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PandaVideo
{
    /// <summary>
    /// Interaction logic for ShowOutput.xaml
    /// </summary>
    public partial class ShowOutput : Window
    {
        public string Output { get; set; }

        public ShowOutput()
        {
            InitializeComponent();
        }

        private void buttonClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            textBox1.Text = Output;
        }
    }
}
