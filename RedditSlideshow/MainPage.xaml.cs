using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.ServiceModel.Channels;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace RedditSlideshow
{

    public class Link {
        public string Url { get; set; }
        public Link()
        {
            Url = "";
        }
    }



    public sealed partial class MainPage : Page
    {
        
        public ObservableCollection<Link> LinkList;

        public MainPage()
        {
            LinkList = new ObservableCollection<Link>();
            this.InitializeComponent();
        }

        private void addLink_Click(object sender, RoutedEventArgs e)
        {
            LinkList.Add(new Link());

        }

        private void removeLink_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            LinkList.Remove((RedditSlideshow.Link)button.DataContext);

        }

        private void generateSlideShow_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
