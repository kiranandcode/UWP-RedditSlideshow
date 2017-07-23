using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Composition;
using Microsoft.Graphics.Canvas.Effects;
using System.Net.Http;
using RedditSlideshow.Models;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Diagnostics;
using Windows.ApplicationModel.Background;
using System.Threading;
using Windows.System.Threading;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace RedditSlideshow.Views
{
    public class MediaListUrlTitleConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            MediaUrlList list = value as MediaUrlList;
            return list.Url.Title;

        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class MediaListUrlImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            MediaUrlList list = value as MediaUrlList;
            return list.Url.Image;

        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Slideshow : Page
    {
        ThreadPoolTimer autoforwardTimer;
        
        Boolean MenuExtended = false;
        MediaUrlList medialist;

        public Slideshow()
        {
            medialist = new MediaUrlList();
            medialist.addListener((a, e) => {
                Debug.WriteLine("MediaLIst Property changed!");
                MainSlideshowImage.Source = medialist.Url.Image;
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
            this.InitializeComponent();

     
        }



        private void ViewMoreButtonClick(object sender, RoutedEventArgs e)
        {
            if(!MenuExtended)
            {
                menuDisplayStoryboardOpen.Begin();
                ImageBackgroundBlur.Value = 10;
                ImageBackgroundBlur.Duration = 1000;
                ImageBackgroundBlur.StartAnimation();
                //DisplayGridTransform.Y -= MenuContentGrid.ActualHeight;//- 10;
            }
            else
            {
                menuDisplayStoryboardClose.Begin();
                ImageBackgroundBlur.Duration = 1000;
                ImageBackgroundBlur.Value = 0;
                ImageBackgroundBlur.StartAnimation();
                //DisplayGridTransform.Y += MenuContentGrid.ActualHeight;//- 10;

            }
            MenuExtended = !MenuExtended;

           
        }

        private void GoBackButtonClick(object sender, RoutedEventArgs e)
        {
            if (this.Frame.CanGoBack)
            {
                // Deallocate resources.
                if(autoforwardTimer != null) autoforwardTimer.Cancel();
                this.Frame.GoBack();
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            List<string> urls = e.Parameter as List<string>;
           
                GenerateImages(urls).ContinueWith((result) =>
                {
                    foreach (MediaUrl obj in result.Result)
                    {

                        medialist.Add(obj);

                    };
                    if(result.Result.Count == 0)
                    {
                        // No Urls retrieved, show error, return to sender.
                        Debug.WriteLine("No Urls Retrieved");
                    }
                });
             



        }

        private static async Task<List<MediaUrl>> GenerateImages(List<string> urls)
        {
            // Regex for checking image items
            Regex image_expr = new Regex(@".(?:jpg|jpeg|png|bmp|gif|gifv)$");
            Regex gifv_expr = new Regex(@".(?:gifv)$");

            List<MediaUrl> list = new List<MediaUrl>();
            using (HttpClient client = new HttpClient())
            {
                
                IEnumerable<string> full_uris = urls.Select(str => "https://www.reddit.com/" + str + ".json?limit=100");
                foreach (string link in full_uris)
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
                                    MediaUrl media_obj = new MediaUrl(child.data.title, url, thumb_url, self_url);
                                    list.Add(media_obj);
                                }
                            }
                        } catch (UriFormatException e)
                        {
                            Debug.WriteLine(e);
                        }

                    }
                }
            }
            return list;
        }

        static string UriToString(Uri uri)
        {
            return uri.ToString();
        }

        private void TextboxNumeralsOnly(TextBox sender, TextBoxTextChangingEventArgs args)
        {
            sender.Text = new String(sender.Text.Where((char x) =>
            {
                if (Char.IsDigit(x))
                    return true;
                else
                    return false;
            }).ToArray());
        }



        private void configureAutoTask(Boolean enable, int period)
        {
            if (enable)
            {
                if (autoforwardTimer != null) autoforwardTimer.Cancel();
                
                autoforwardTimer = ThreadPoolTimer.CreatePeriodicTimer((source) => {
                    
                    Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        medialist.incrementPosition();
                    });

                }, TimeSpan.FromSeconds(period));
                Debug.WriteLine(autoforwardTimer.ToString());

            }
            else
            {
                if (autoforwardTimer != null) autoforwardTimer.Cancel();

            }
        }

        private void enableAutoForward(object sender, RoutedEventArgs e)
        {
            CheckBox checkbox = sender as CheckBox;
            int period;

            if (!Int32.TryParse(AutoForwardDelayTimeTextBox.Text, out period)) period = 5;
            configureAutoTask(checkbox.IsChecked ?? false, period);
        }

        private void timingChanged(object sender, RoutedEventArgs e)
        {
            int period;

            if (!Int32.TryParse(AutoForwardDelayTimeTextBox.Text, out period)) period = 5;
            configureAutoTask(AutoForwardEnabledCheckbox.IsChecked ?? false, period);
        }
    }
}
