using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using System.Runtime.CompilerServices;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

namespace RedditSlideshow.Models {

    // A custom observable list to also contain the 
    public class MediaUrlList : ObservableCollection<MediaUrl>
    {
        public MediaUrlList() : base()
        {
        }

        public new void Add(MediaUrl item)
        {
            // Override the add method to also trigger the collection's property changed event
            item.PropertyChanged += (object sender, PropertyChangedEventArgs args) =>
            {
                base.OnPropertyChanged(args);
            };

            Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => { base.Add(item); });

        }
    }

    // Class Representing a MediaItem - Also encapsulating the async call to retrieve the data
    public class MediaUrl : INotifyPropertyChanged
    {
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


        public Uri Image_Uri { get; set; }
        public Uri Image_Thumb_Uri { get; set; }

        public BitmapImage Image { get; set; }

        public String Title { get; set; }

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        public MediaUrl(String title,String imageUri, String imageThumbUri)
        {
            Image_Uri = new Uri(imageUri);
            Image_Thumb_Uri = new Uri(imageThumbUri);
            image_retrieved = false;
            this.Title = title;
        }

        public async void RetrieveContent()
        {
            HttpClient client = new HttpClient();


            HttpResponseMessage response = await client.GetAsync(Image_Uri);
            byte[] img= await response.Content.ReadAsByteArrayAsync();
            InMemoryRandomAccessStream randomAccessStream = new InMemoryRandomAccessStream();
            DataWriter writer = new DataWriter(randomAccessStream.GetOutputStreamAt(0));
            writer.WriteBytes(img);
            await writer.StoreAsync();
            writer.Dispose();

            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
             {
                 Image = new BitmapImage();
                 Image.SetSource(randomAccessStream);
                 Image_retrieved = true;
             });
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

    }



}