using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using System.Runtime.CompilerServices;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

namespace RedditSlideshow.Models {

    // A custom observable list to also contain the 
    class MediaUrlList : ObservableCollection<MediaUrl>
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

            base.Add(item);
        }
    }

    // Class Representing a MediaItem - Also encapsulating the async call to retrieve the data
    class MediaUrl : INotifyPropertyChanged
    {
        private Boolean image_retrieved;
        private Boolean thumb_retrieved;

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

        public Boolean Thumb_retrieved {
            get
            {
                return thumb_retrieved;
            }
            set
            {
                thumb_retrieved = value;
                OnPropertyChanged();
            }
        }

        public Uri Image_Uri { get; set; }
        public Uri Image_Thumb_Uri { get; set; }

        public BitmapImage Image { get; set; }
        public BitmapImage Image_Thumb { get; set; }

        public String Title { get; set; }

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        public MediaUrl(String title,Uri imageUri, Uri imageThumbUri)
        {
            Image_Uri = imageUri;
            Image_Thumb_Uri = imageThumbUri;
            image_retrieved = false;
            thumb_retrieved = false;
            this.Title = title;
        }

        public async void RetrieveContent()
        {
            HttpClient client = new HttpClient();

            // First retrieve the thumbnail.
            HttpResponseMessage response = await client.GetAsync(Image_Thumb_Uri);

            byte[] img_thumb = await response.Content.ReadAsByteArrayAsync();


            InMemoryRandomAccessStream randomAccessStream = new InMemoryRandomAccessStream();

            DataWriter writer = new DataWriter(randomAccessStream.GetOutputStreamAt(0));
            writer.WriteBytes(img_thumb);

            await writer.StoreAsync();

            BitmapImage thumb = new BitmapImage();
            thumb.SetSource(randomAccessStream);

            Image_Thumb = thumb;
            Thumb_retrieved = true;

            if(Image_Uri != Image_Thumb_Uri)
            {
                // Get the full image
                response = await client.GetAsync(Image_Uri);

                byte[] img = await response.Content.ReadAsByteArrayAsync();

                randomAccessStream = new InMemoryRandomAccessStream();

                writer = new DataWriter(randomAccessStream.GetOutputStreamAt(0));
                writer.WriteBytes(img);

                await writer.StoreAsync();

                thumb = new BitmapImage();
                thumb.SetSource(randomAccessStream);

                Image = thumb;

                Image_retrieved = true;

            } else
            {
                Image = thumb;
                Image_retrieved = true;
            }


        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

    }



}