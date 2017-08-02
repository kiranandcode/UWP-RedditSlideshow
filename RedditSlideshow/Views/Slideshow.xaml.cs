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
using RedditSlideshow.Controls;
using Windows.Storage;
using Windows.Graphics.Imaging;
using Microsoft.Gestures;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Automation.Provider;
using Windows.UI.Core;
using RedditSlideshow.Models.Gfycat;
using RedditSlideshow.Authentication;
using System.Net.Http.Headers;
using RedditSlideshow.Models.Imgur;
// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace RedditSlideshow.Views
{


    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Slideshow : Page
    {
        ThreadPoolTimer autoforwardTimer;

        CancellationTokenSource downloadTaskCancelToken = new CancellationTokenSource();

        SemaphoreSlim totalRequestSemaphore = new SemaphoreSlim(1, 1);
        Boolean MenuExtended = false;
        MediaUrlList medialist;

        public Slideshow()
        {
            medialist = new MediaUrlList();




            this.InitializeComponent();

            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
        }


        private void ViewMoreButtonClick(object sender, RoutedEventArgs e)
        {
            if (!MenuExtended)
            {
                menuDisplayStoryboardOpen.Begin();
                ImageBackgroundBlur.Value = 10;
                ImageBackgroundBlur.Duration = 1000;
                ImageBackgroundBlur.StartAnimation();
            }
            else
            {
                menuDisplayStoryboardClose.Begin();
                ImageBackgroundBlur.Duration = 1000;
                ImageBackgroundBlur.Value = 0;
                ImageBackgroundBlur.StartAnimation();

            }
            MenuExtended = !MenuExtended;


        }

        private void GoBackButtonClick(object sender, RoutedEventArgs e)
        {
            if (this.Frame.CanGoBack)
            {
                // Deallocate resources.
                if (autoforwardTimer != null) autoforwardTimer.Cancel();
                if (downloadTaskCancelToken != null) downloadTaskCancelToken.Cancel();

                this.Frame.GoBack();
            }
        }
        
        

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {

            base.OnNavigatedTo(e);



            List<string> urls = e.Parameter as List<string>;

            GenerateImages(urls, downloadTaskCancelToken.Token).ContinueWith((result) =>
            {
                Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    ContentLoadingPanel.Visibility = Visibility.Collapsed;
                });

                for (int i = 0; i< result.Result.Count-1; i++)
                {
                    MediaUrl obj = result.Result[i];
                    medialist.Add(obj);
                };


                medialist.addListener((a, f) =>
                {
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

                if(result.Result.Count != 0)
                medialist.Add(result.Result.Last());





                if (result.Result.Count == 0 && !downloadTaskCancelToken.Token.IsCancellationRequested)
                {
                    // No Urls retrieved, show error, return to sender.

                    Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    Debug.WriteLine("No Urls Retrieved");
                    var dialog = new ContentDialog()
                    {
                        Title = "Error - 404 Not Found",
                        Background = Application.Current.Resources["MainTheme_color_highlight"] as SolidColorBrush,
                    };
                    var panel = new StackPanel()
                    {
                    };
                    panel.Children.Add(new TextBlock
                    {
                        Text = "Unfortunately we couldn't find any image urls (ending in .png, .jpg, etc...) at the specified subreddit.",
                        TextWrapping = TextWrapping.Wrap,

                    });

                    dialog.Content = panel;

                    dialog.PrimaryButtonText = "Ok";
                    dialog.PrimaryButtonClick += (sender, args) =>
                   {
                       GoBackButtonClick(sender, new RoutedEventArgs());
                   };


                    dialog.ShowAsync();
                });

                }
            });

            

        }

        private async Task<List<MediaUrl>> GenerateImages(List<string> urls, CancellationToken token)
        {
            // Regex for checking image items
            Regex image_expr = new Regex(@".(?:jpg|jpeg|png|bmp|gif|gifv)$");
            Regex gifv_expr = new Regex(@".(?:gifv)$");

            // Regex for identifying non-type-labelled imgur links
            Regex imgur_album = new Regex(@"(?:[a-zA-Z0-0]\.)?imgur.com\/(?:a|gallery|t\/[a-zA-Z0-9]*)\/([a-zA-Z0-9]*)");
            Regex imgur_unmarked = new Regex(@"(?:[a-zA-Z0-0]\.)?imgur.com\/([a-zA-Z0-9]{3,})");

            // Regex for identifying gfycat(beautifully simple) links
            Regex gfycat_links = new Regex(@"gfycat.com\/([a-zA-Z0-9]*)");
            int imgur_remaining = 0;

            List<MediaUrl> list = new List<MediaUrl>();
            using (HttpClient client = new HttpClient())
            {

                string imgur_available_url = "https://api.imgur.com/3/credits";
                if (token.IsCancellationRequested) return new List<MediaUrl>();
                try
                {
                    var reqMsg = new HttpRequestMessage(HttpMethod.Get, imgur_available_url);
                    reqMsg.Headers.Authorization = new AuthenticationHeaderValue("Client-ID", ImgurAuth.client_id);
                    HttpResponseMessage imgurresponse = await client.SendAsync(reqMsg);
                    if (imgurresponse.IsSuccessStatusCode)
                    {
                        string imgurcontent = await imgurresponse.Content.ReadAsStringAsync();

                        var creditsdata = JsonConvert.DeserializeObject<Creditsobject>(imgurcontent);
                        imgur_remaining = creditsdata.data.ClientRemaining;
                    } else
                    {
                        imgur_remaining = 0;
                    }
                } catch(HttpRequestException e)
                {
                    imgur_remaining = 0;
                    Debug.WriteLine(e);
                }
                catch (JsonReaderException e)
                {
                    imgur_remaining = 0;
                    Debug.WriteLine(e);
                }
                catch (NullReferenceException e)
                {

                } catch(JsonSerializationException e)
                {

                    Debug.WriteLine(e);
                }

                int total_link_count = urls.Count;
                int current_link_count = 0;
                int current_image_count = 0;

                Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    OverallProgressText.Text = "Processed " + current_link_count + " / " + total_link_count + " Links";
                });

                IEnumerable<string> full_uris = urls.Select(str => "https://www.reddit.com/" + str + ".json?limit=100");
                int downloaded = 0;
                Stopwatch watch = new Stopwatch();
                watch.Start();

                foreach (string link in full_uris)
                {
                    if (token.IsCancellationRequested) return new List<MediaUrl>();
                    HttpResponseMessage response = await client.GetAsync(link);
                    if (response.IsSuccessStatusCode)
                    {
                        try
                        {
                            string result = await response.Content.ReadAsStringAsync();
                            var rootResult = JsonConvert.DeserializeObject<Rootobject>(result);
                            foreach (var child in rootResult.data.children)
                            {
                                if (token.IsCancellationRequested) return new List<MediaUrl>();
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

                                    if (!String.IsNullOrEmpty(thumb_url) && !String.IsNullOrEmpty(url) && !String.IsNullOrEmpty(self_url)) {
                                        MediaUrl media_obj = new MediaUrl(child.data.title, url, thumb_url, self_url);
                                        list.Add(media_obj);
                                        current_image_count++;
                                        Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                                        {
                                            ImagesCountText.Text = "Loaded " + current_image_count + " Images";
                                        });
                                    }
                                }
                                else if(imgur_album.IsMatch(child.data.url) && imgur_remaining > 500)
                                {
                                    imgur_remaining--;
                                    Match match = imgur_album.Match(child.data.url);
                                    string imgur_album_url = "https://api.imgur.com/3/album/" + match.Groups[1].Value;
                                    Debug.WriteLine(imgur_album_url);
                                    try
                                    {
                                        var requestMessage = new HttpRequestMessage(HttpMethod.Get, imgur_album_url);
                                        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Client-ID",ImgurAuth.client_id);
                                        HttpResponseMessage imgurresponse = await client.SendAsync(requestMessage);
                                        if (imgurresponse.IsSuccessStatusCode)
                                        {
                                            string imgurcontent = await imgurresponse.Content.ReadAsStringAsync();

                                            var imgurdata = JsonConvert.DeserializeObject<Albumobject>(imgurcontent);


                                            if (imgurdata.success)
                                            {
                                                foreach (Models.Imgur.Image img in imgurdata.data.images)
                                                {
                                                    string url = img.link;

                                                    string thumb_url = url;
                                                    string self_url = "www.reddit.com" + child.data.permalink;
                                                    if (!String.IsNullOrEmpty(child.data.thumbnail))
                                                    {
                                                        thumb_url = child.data.thumbnail;
                                                    }
                                                    string title = child.data.title;


                                                    if (!String.IsNullOrEmpty(thumb_url) && !String.IsNullOrEmpty(url) && !String.IsNullOrEmpty(self_url)) {
                                                        MediaUrl media_obj = new MediaUrl(title, url, thumb_url, self_url);
                                                        list.Add(media_obj);
                                                        current_image_count++;
                                                        Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                                                        {
                                                            ImagesCountText.Text = "Loaded " + current_image_count + " Images";
                                                        });
                                                    }
                                                }

                                            }
                                        }


                                    }
                                    catch (HttpRequestException e)
                                    {

                                        Debug.WriteLine(e);
                                    }
                                    catch (JsonReaderException e)
                                    {

                                        Debug.WriteLine(e);
                                    }
                                    catch (NullReferenceException e)
                                    {

                                        Debug.WriteLine(e);
                                    }
                                    catch (JsonSerializationException e)
                                    {

                                        Debug.WriteLine(e);
                                    }

                                } else if(imgur_unmarked.IsMatch(child.data.url) && imgur_remaining > 500)
                                {
                                    imgur_remaining--;
                                    Match match = imgur_unmarked.Match(child.data.url);
                                    string imgur_album_url = "https://api.imgur.com/3/album/" + match.Groups[1].Value;
                                    Debug.WriteLine(imgur_album_url);
                                    try
                                    {
                                        var requestMessage = new HttpRequestMessage(HttpMethod.Get, imgur_album_url);
                                        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Client-ID", ImgurAuth.client_id);
                                        HttpResponseMessage imgurresponse = await client.SendAsync(requestMessage);
                                        if (imgurresponse.IsSuccessStatusCode)
                                        {
                                            string imgurcontent = await imgurresponse.Content.ReadAsStringAsync();

                                            var imgurdata = JsonConvert.DeserializeObject<Albumobject>(imgurcontent);


                                            if (imgurdata.success)
                                            {
                                                foreach (Models.Imgur.Image img in imgurdata.data.images)
                                                {
                                                    string url = img.link;

                                                    string thumb_url = url;
                                                    string self_url = "www.reddit.com" + child.data.permalink;
                                                    if (!String.IsNullOrEmpty(child.data.thumbnail))
                                                    {
                                                        thumb_url = child.data.thumbnail;
                                                    }
                                                    string title = child.data.title;

                                                    if (!String.IsNullOrEmpty(thumb_url) && !String.IsNullOrEmpty(url) && !String.IsNullOrEmpty(self_url)) {
                                                        MediaUrl media_obj = new MediaUrl(title, url, thumb_url, self_url);
                                                        list.Add(media_obj);
                                                        current_image_count++;
                                                        Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                                                        {
                                                            ImagesCountText.Text = "Loaded " + current_image_count + " Images";
                                                        });
                                                    }
                                                    
                                                }

                                            } else if(imgur_remaining > 500)
                                            {
                                                imgur_remaining--;
                                                imgur_album_url = "https://api.imgur.com/3/image/" + match.Groups[1].Value;
                                                try
                                                {
                                                    requestMessage = new HttpRequestMessage(HttpMethod.Get, imgur_album_url);
                                                    requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Client-ID", ImgurAuth.client_id);
                                                    imgurresponse = await client.SendAsync(requestMessage);
                                                    if (imgurresponse.IsSuccessStatusCode)
                                                    {
                                                        imgurcontent = await imgurresponse.Content.ReadAsStringAsync();

                                                        var imgur_img = JsonConvert.DeserializeObject<Imageobject>(imgurcontent);
                                                        if(imgur_img.success)
                                                        {
                                                            string url = imgur_img.data.link;

                                                            string thumb_url = url;
                                                            string self_url = "www.reddit.com" + child.data.permalink;
                                                            if (!String.IsNullOrEmpty(child.data.thumbnail))
                                                            {
                                                                thumb_url = child.data.thumbnail;
                                                            }
                                                            string title = child.data.title;

                                                            if (!String.IsNullOrEmpty(thumb_url) && !String.IsNullOrEmpty(url) && !String.IsNullOrEmpty(self_url)) {
                                                                MediaUrl media_obj = new MediaUrl(title, url, thumb_url, self_url);
                                                                list.Add(media_obj);
                                                                current_image_count++;
                                                                Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                                                                {
                                                                    ImagesCountText.Text = "Loaded " + current_image_count + " Images";
                                                                });
                                                            }
                                                        }
                                                    }


                                                    } catch(HttpRequestException e)
                                                {

                                                    Debug.WriteLine(e);
                                                }
                                                catch (JsonReaderException e)
                                                {

                                                    Debug.WriteLine(e);
                                                }
                                                catch (NullReferenceException e)
                                                {

                                                    Debug.WriteLine(e);
                                                }
                                                catch (JsonSerializationException e)
                                                {

                                                    Debug.WriteLine(e);
                                                }
                                            }
                                        }


                                    }
                                    catch (HttpRequestException e)
                                    {

                                        Debug.WriteLine(e);
                                    }
                                    catch (JsonReaderException e)
                                    {

                                        Debug.WriteLine(e);
                                    }
                                    catch (NullReferenceException e)
                                    {

                                        Debug.WriteLine(e);
                                    }
                                    catch (JsonSerializationException e)
                                    {

                                        Debug.WriteLine(e);
                                    }


                                } else if (gfycat_links.IsMatch(child.data.url))
                                {
                                    Match match = gfycat_links.Match(child.data.url);
                                    Debug.WriteLine(match.Groups[1] +" <- " + child.data.url);
                                    string gfy_url = "https://gfycat.com/cajax/get/" + match.Groups[1].Value;
                                    Debug.WriteLine(gfy_url);

                                    try
                                    {
                                        HttpResponseMessage gfyresponse = await client.GetAsync(gfy_url);
                                        if (gfyresponse.IsSuccessStatusCode)
                                        {
                                            string gfycontent = await gfyresponse.Content.ReadAsStringAsync();

                                            var gfydata = JsonConvert.DeserializeObject<Gfyobject>(gfycontent);
                                            var gfyurl = gfydata.gfyItem.gifUrl;

                                            string thumb_url = gfydata.gfyItem.max2mbGif;
                                            string self_url = "www.reddit.com" + child.data.permalink;
                                            if (!String.IsNullOrEmpty(child.data.thumbnail))
                                            {
                                                thumb_url = child.data.thumbnail;
                                            }

                                            if (!String.IsNullOrEmpty(thumb_url) && !String.IsNullOrEmpty(gfyurl) && !String.IsNullOrEmpty(self_url)) {
                                                MediaUrl media_obj = new MediaUrl(child.data.title, gfyurl, thumb_url, self_url);
                                                list.Add(media_obj);
                                                current_image_count++;
                                                Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                                                {
                                                    ImagesCountText.Text = "Loaded " + current_image_count + " Images";
                                                });
                                            }

                                        }
                                    } catch(HttpRequestException e)
                                    {

                                        Debug.WriteLine(e);
                                    } catch(NullReferenceException e)
                                    {
                                        Debug.WriteLine(e);

                                    }catch(JsonReaderException e)
                                    {

                                        Debug.WriteLine(e);
                                    }
                                    catch (JsonSerializationException e)
                                    {
                                        Debug.WriteLine(e);

                                    }
                                }
                            }
                        }
                        catch (UriFormatException e)
                        {
                            Debug.WriteLine(e);
                        } catch(JsonReaderException e)
                        {

                            Debug.WriteLine(e);
                        }
                        catch (NullReferenceException e)
                        {

                            Debug.WriteLine(e);
                        }
                        catch (JsonSerializationException e)
                        {

                            Debug.WriteLine(e);
                        }

                    }
                    downloaded++;

                    if(downloaded >= 50)
                   {
                        int seconds_since = watch.Elapsed.Seconds;
                        watch.Reset();
                        if (seconds_since < 60)
                        {
                            await Task.Delay((60 - seconds_since) * 1000);
                        }
                        
                        downloaded = downloaded < 60 ? 0 : downloaded - 60;
                    }
                    current_link_count++;
                    Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        OverallProgressText.Text = "Processed " + current_link_count + " / " + total_link_count + " Links";
                        OverallProgressLoader.Value = (float)current_link_count / total_link_count * 100;
                    });
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

                autoforwardTimer = ThreadPoolTimer.CreatePeriodicTimer((source) =>
                {

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

        private void ImageView_Click(object sender, EventArgs e)
        {
            
            ImageView view = sender as ImageView;
            MediaUrl url = view.DataContext as MediaUrl;
            medialist.setPosition(url.Index);
        }

        private async void downloadImageAsync(object sender, RoutedEventArgs e)
        {
            
            if (medialist.Count != 0)
            {

                MediaUrl current_image = medialist.Url;
                if(current_image.Image_retrieved && !current_image.Failed)
                try
                {
                    await totalRequestSemaphore.WaitAsync();


                    HttpClientHandler handler = new HttpClientHandler();
                    handler.AllowAutoRedirect = false;
                    HttpClient client = new HttpClient(handler);

                    HttpResponseMessage response = await client.GetAsync(current_image.Image_Uri);

                    while (response.StatusCode == System.Net.HttpStatusCode.Redirect || response.StatusCode == System.Net.HttpStatusCode.MovedPermanently)
                    {
                        // Redirect required.
                        Uri redirect_uri = response.Headers.Location;
                        response = await client.GetAsync(redirect_uri);
                    }

                    byte[] img = await response.Content.ReadAsByteArrayAsync();

                    String filename = current_image.Image_Uri.Segments.Last();
                    StorageFile x = await KnownFolders.SavedPictures.CreateFileAsync(filename, CreationCollisionOption.GenerateUniqueName);
                    using (IRandomAccessStream stream = await x.OpenAsync(FileAccessMode.ReadWrite))
                    {

                        using (var output = stream.GetOutputStreamAt(0))
                        {
                            using (DataWriter writer = new DataWriter(output))
                            {
                                writer.WriteBytes(img);
                                await writer.StoreAsync();
                            }

                        }


                    }
                }
                catch (System.Threading.Tasks.TaskCanceledException except2)
                {
                    Debug.WriteLine(except2);
                }
                catch (HttpRequestException excep3)
                {
                    Debug.WriteLine(excep3);
                }
                finally
                {
                    totalRequestSemaphore.Release();

                }

            }
            
        }

        private void SlideshowKeyHandler(object sender, KeyRoutedEventArgs e)
        {
            if(e.Key == Windows.System.VirtualKey.Left) {
                ButtonAutomationPeer peer = new ButtonAutomationPeer(LeftButton);

                IInvokeProvider invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                invokeProv.Invoke();
            }
            else if(e.Key == Windows.System.VirtualKey.Right)
            {
                ButtonAutomationPeer peer = new ButtonAutomationPeer(RightButton);

                IInvokeProvider invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                invokeProv.Invoke();
            }
            else if(e.Key == Windows.System.VirtualKey.Up) {
                ButtonAutomationPeer peer = new ButtonAutomationPeer(ViewMoreButton);

                IInvokeProvider invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                invokeProv.Invoke();
            }
            else if(e.Key == Windows.System.VirtualKey.Down)
            {
                if (MenuExtended)
                {
                    ButtonAutomationPeer peer = new ButtonAutomationPeer(ViewMoreButton);

                    IInvokeProvider invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                    invokeProv.Invoke();
                }
                else downloadImageAsync(this, new RoutedEventArgs());
            }
        }


        private void CoreWindow_KeyDown(CoreWindow sender, KeyEventArgs args)
        {

            if (args.VirtualKey == Windows.System.VirtualKey.Left)
            {
                ButtonAutomationPeer peer = new ButtonAutomationPeer(LeftButton);

                IInvokeProvider invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                invokeProv.Invoke();
            }
            else if (args.VirtualKey == Windows.System.VirtualKey.Right)
            {
                ButtonAutomationPeer peer = new ButtonAutomationPeer(RightButton);

                IInvokeProvider invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                invokeProv.Invoke();
            }
            else if (args.VirtualKey == Windows.System.VirtualKey.Up)
            {
                ButtonAutomationPeer peer = new ButtonAutomationPeer(ViewMoreButton);

                IInvokeProvider invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                invokeProv.Invoke();
            }
            else if (args.VirtualKey == Windows.System.VirtualKey.Down)
            {
                if (MenuExtended)
                {
                    ButtonAutomationPeer peer = new ButtonAutomationPeer(ViewMoreButton);

                    IInvokeProvider invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                    invokeProv.Invoke();
                }
                else downloadImageAsync(this, new RoutedEventArgs());
            }


        }

    }
}
