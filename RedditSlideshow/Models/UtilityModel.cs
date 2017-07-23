using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

namespace RedditSlideshow.Models {

    // A custom observable list to also contain the 
    public class MediaUrlList : ObservableCollection<MediaUrl>
    {
        private int index = 0;

        public void addListener(PropertyChangedEventHandler del)
        {
            base.PropertyChanged += del;
        }

        public int Index {
            get
            {
                return index;
            }
            set
            {
                index = value;
                Changed();
            }
        }

        public void incrementPosition()
        {
            if (base.Count != 0)
                Index = (Index + 1) % base.Count;
        }

        public void decrementPosition()
        {
            if (Index > 0)
            {
                Index = (Index - 1);
            } else
            {
                Index = base.Count - 1;
            }
        }

        public void setPosition(int position)
        {
            position = position % base.Count;
            while (position < 0) position += base.Count;
            while (position > base.Count) position -= base.Count;
            Index = position;

        }

        public MediaUrl Url
        {
            get
            {
                // To provide a smooth experience, we auto buffer the next and prior images
                if (base.Count != 0)
                {

                        MediaUrl prev = base[(Index > 0 ? Index-1 : base.Count - 1)];
                        MediaUrl next = base[(Index < base.Count - 1 ? Index + 1 : 0)];
                    
                    if(!next.Image_retrieved && !next.Failed && next.retrievingContent.CurrentCount == 1 && MediaUrl.totalRequestSemaphore.CurrentCount != 0)
                        Windows.System.Threading.ThreadPool.RunAsync((workitem) => { next.RetrieveContent(); }, Windows.System.Threading.WorkItemPriority.Low);
                    if(!prev.Image_retrieved && !prev.Failed && prev.retrievingContent.CurrentCount == 1 && MediaUrl.totalRequestSemaphore.CurrentCount != 0)
                        Windows.System.Threading.ThreadPool.RunAsync((workitem) => { prev.RetrieveContent(); }, Windows.System.Threading.WorkItemPriority.Low);

                        MediaUrl current = base[Index];
                        if(!current.Image_retrieved && !current.Failed && current.retrievingContent.CurrentCount == 1 && MediaUrl.totalRequestSemaphore.CurrentCount != 0)
                        {
                        Windows.System.Threading.ThreadPool.RunAsync((workitem) => { current.RetrieveContent(); }, Windows.System.Threading.WorkItemPriority.Low);
                        }

                        

                        return current;
                }
                else
                    return new MediaUrl("Placeholder", "http://lorempixel.com/400/200/", "http://lorempixel.com/400/200/", "placeholder" );

            }
            set
            {

                    base[Index] = value;

                Changed();
            }
        }

        public MediaUrlList() : base()
        {
        }



        public new void Add(MediaUrl item)
        {

            
            // Override the add method to also trigger the collection's property changed event
            item.PropertyChanged += async (object sender, PropertyChangedEventArgs args) =>
            {


                    base.OnPropertyChanged(args);

            };

            Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => {
                base.Add(item);
            });

        }

        public void Changed([CallerMemberName] string propertyName = null)
        {
           
                base.OnPropertyChanged(new PropertyChangedEventArgs(propertyName));

        }
    }

    // Class Representing a MediaItem - Also encapsulating the async call to retrieve the data
    public class MediaUrl : INotifyPropertyChanged
    {
        public static SemaphoreSlim totalRequestSemaphore = new SemaphoreSlim(5, 5);
        private Boolean image_retrieved;

        public Boolean Image_retrieved {
            get
            {
                return image_retrieved;
            }
            set
            {
                image_retrieved = value;
                OnPropertyChanged();
            }
        }
        public Boolean failed;
        public Boolean Failed { get { return failed;  } set { failed = value; OnPropertyChanged(); } }
        public Uri Image_Uri { get; set; }
        public Uri Image_Thumb_Uri { get; set; }
        public string Self { get; set; }

        public BitmapImage Image { get; set; }

        public String Title { get; set; }

        public SemaphoreSlim retrievingContent = new SemaphoreSlim(1,1);

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        public MediaUrl(String title, String imageUri, String imageThumbUri, string self_url)
        {
            Image_Uri = new Uri(imageUri);
            Image_Thumb_Uri = new Uri(imageThumbUri);
            image_retrieved = false;
            this.Title = title;
            Self = self_url;
            failed = false;
        }

        public async void RetrieveContent()
        {

            // If we're already retrieving the image, wait.
            await retrievingContent.WaitAsync();
           
            try {
                if (Image_retrieved || failed) return;

                try { 
                await totalRequestSemaphore.WaitAsync();
                // For http requests, as often reddit img urls redirect https -> http, which is not supported by default
                // we have to manually perform redirects.
                HttpClientHandler handler = new HttpClientHandler();
                handler.AllowAutoRedirect = false;
                HttpClient client = new HttpClient(handler);
                
                HttpResponseMessage response = await client.GetAsync(Image_Uri);

                while(response.StatusCode == System.Net.HttpStatusCode.Redirect || response.StatusCode == System.Net.HttpStatusCode.MovedPermanently)
                {
                    // Redirect required.
                    Uri redirect_uri = response.Headers.Location;
                    Debug.WriteLine("Redirecting to " + redirect_uri);
                    response = await client.GetAsync(redirect_uri);
                }
                
                    byte[] img = await response.Content.ReadAsByteArrayAsync();
                    InMemoryRandomAccessStream randomAccessStream = new InMemoryRandomAccessStream();
                    using (DataWriter writer = new DataWriter(randomAccessStream.GetOutputStreamAt(0))) {
                        writer.WriteBytes(img);
                        await writer.StoreAsync();
                    }

                    await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        Image = new BitmapImage();
                        Image.SetSource(randomAccessStream);
                        randomAccessStream.Dispose();
                        Image_retrieved = true;
                    });
                
            } catch(System.Threading.Tasks.TaskCanceledException e)
            {
                Debug.WriteLine(e);
                failed = true;
            }
            catch (HttpRequestException e)
            {
                Debug.WriteLine(e);
                failed = true;
            }
                finally
                {
                    totalRequestSemaphore.Release();

                }
            }
            finally
            {

                retrievingContent.Release();
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {

            this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

    }



}