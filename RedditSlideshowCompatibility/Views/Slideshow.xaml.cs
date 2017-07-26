using Newtonsoft.Json;
using RedditSlideshow.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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
            InitializeComponent();
            medialist.addListener((a, e) => Application.Current.Dispatcher.Invoke(new Action(() => {

                ImageSource src = new BitmapImage(medialist.Url.Image_Uri);

                if (src != null)
                {

                    Debug.WriteLine(src.ToString());

                    MainSlideshowImage.Source = src;
                }
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

            })));

            GenerateImages(urls).ContinueWith((result) =>
            {
                foreach (MediaUrl obj in result.Result)
                {
                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        medialist.Add(obj);
                    }));

                };
                if (result.Result.Count == 0)
                {
                    // No Urls retrieved, show error, return to sender.

                    ThreadStart thread = delegate
                    {
                        Debug.WriteLine("No Urls Retrieved");
                        MessageBoxButton button = MessageBoxButton.OK;
                        
                        MessageBoxResult res = MessageBox.Show("Unfortunately we couldn't find any image urls (ending in .png, .jpg, etc...) at the specified subreddit.", "Error - 404 Not Found",button);
                        if(res == MessageBoxResult.OK)
                        {
                            this.Hide();
                            parent.Show();
                        }
                    };

                }
            });


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

        private static async Task<List<MediaUrl>> GenerateImages(string link)
        {
            // Regex for checking image items
            Regex image_expr = new Regex(@".(?:jpg|jpeg|png|bmp|gif|gifv)$");
            Regex gifv_expr = new Regex(@".(?:gifv)$");

            List<MediaUrl> list = new List<MediaUrl>();
            link = "https://www.reddit.com/" + link + ".json?limit=100";
            using (HttpClient client = new HttpClient())
            {
                
                    HttpResponseMessage response = await client.GetAsync(link);
                    if (response.IsSuccessStatusCode)
                    {
                        try
                        {
                            string result = await response.Content.ReadAsStringAsync();
                            var rootResult = JsonConvert.DeserializeObject<Rootobject>(result);
                            foreach (var child in rootResult.data.children)
                            {
                                if (image_expr.IsMatch(child.data.url))
                                {
                                Application.Current.Dispatcher.Invoke(new Action(() =>
                                {
                                    string url;
                                    if (gifv_expr.IsMatch(child.data.url))
                                    {
                                        url = child.data.url.Substring(0, child.data.url.LastIndexOf('v'));
                                        Debug.WriteLine("Changing " + child.data.url + " -> " + url);
                                    }
                                    else
                                        url = child.data.url;
                                    string thumb_url = url;
                                    string self_url = "www.reddit.com" + child.data.permalink;
                                    if (!String.IsNullOrEmpty(child.data.thumbnail))
                                    {
                                        thumb_url = child.data.thumbnail;
                                    }
                                    MediaUrl media_obj = new MediaUrl(child.data.title, url, thumb_url, self_url, Application.Current.Dispatcher);
                                    list.Add(media_obj);
                                }));
                                }
                            }
                        }
                        catch (UriFormatException e)
                        {
                            Debug.WriteLine(e);
                        }

                    }
                
            }
            return list;
        }
    }
}
