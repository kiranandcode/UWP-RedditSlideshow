using RedditSlideshow.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace RedditSlideshowCompatibility.Views
{



    /// <summary>
    /// Interaction logic for Slideshow.xaml
    /// </summary>
    public partial class Slideshow : Window
    {
        private string url;
        private Window parent;
        MediaUrlList medialist;


        public Slideshow(Window parent, string urls)
        {
            this.parent = parent;
            this.url = urls;

            medialist = new MediaUrlList();
            medialist.addListener((a, e) => {
                Debug.WriteLine("MediaLIst Property changed!");
                MainSlideshowImage.Source = medialist.Url.Image as ImageSource;
                SlideshowImageTitle.Text = medialist.Url.Title;
                CurrentImageUrl.Text = medialist.Url.Self;
                if (medialist.Url.Failed)
                {
                    LoadingRing.Visibility = Visibility.Collapsed;
                    FailedNotification.Visibility = Visibility.Visible;
                }
                else if (!medialist.Url.Image_retrieved)
                {

                    FailedNotification.Visibility = Visibility.Collapsed;
                    LoadingRing.Visibility = Visibility.Visible;
                }
                else
                {
                    FailedNotification.Visibility = Visibility.Collapsed;
                    LoadingRing.Visibility = Visibility.Collapsed;
                }

            });

            InitializeComponent();
        }

        private void GoBackButtonClick(object sender, RoutedEventArgs e)
        {
            this.Hide();
            parent.Show();
        }

        private void ViewMoreButtonClick(object sender, RoutedEventArgs e)
        {

        }

        private void IncrementPosition(object sender, RoutedEventArgs e)
        {
            medialist.incrementPosition();
        }

        private void DecrementPosition(object sender, RoutedEventArgs e)
        {
            medialist.decrementPosition();
        }
    }
}
