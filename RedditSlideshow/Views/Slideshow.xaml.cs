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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace RedditSlideshow.Views
{
    
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Slideshow : Page
    {
        
        Boolean MenuExtended = false;
        MediaUrlList medialist;

        public Slideshow()
        {
            medialist = new MediaUrlList();
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
                this.Frame.GoBack();
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            List<string> urls = e.Parameter as List<string>;
            GenerateImages(urls).ContinueWith((result) => { foreach (MediaUrl obj in result.Result) { medialist.Add(obj); obj.RetrieveContent(); } });

        }

        private static async Task<List<MediaUrl>> GenerateImages(List<string> urls)
        {
            // Regex for checking image items
            Regex image_expr = new Regex(@".(?:jpg|jpeg|png|bmp|gif|gifv)$");

            List<MediaUrl> list = new List<MediaUrl>();
            using (HttpClient client = new HttpClient())
            {
                
                IEnumerable<string> full_uris = urls.Select(str => "https://www.reddit.com/" + str + ".json?limit=100");
                foreach (string link in full_uris)
                {
                    HttpResponseMessage response = await client.GetAsync(link);
                    if (response.IsSuccessStatusCode)
                    {
                        string result = await response.Content.ReadAsStringAsync();
                        var rootResult = JsonConvert.DeserializeObject<Rootobject>(result);
                        foreach(var child in rootResult.data.children)
                        {
                            if(image_expr.IsMatch(child.data.url))
                            {
                                string url = child.data.url;
                                string thumb_url = url;
                                if(!String.IsNullOrEmpty(child.data.thumbnail))
                                {
                                    thumb_url = child.data.thumbnail;
                                }
                                MediaUrl media_obj = new MediaUrl(child.data.title, url, thumb_url);
                                list.Add(media_obj);
                            }
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
    }
}
