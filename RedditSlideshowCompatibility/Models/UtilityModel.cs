using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Media.Imaging;
using Windows.Storage.Streams;
using Windows.System;

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


         

                    MediaUrl current = base[Index];
       


                    return current;
                }
                else
                    throw new InvalidOperationException();

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
            item.PropertyChanged += (object sender, PropertyChangedEventArgs args) =>
            {
                base.OnPropertyChanged(args);
            };

            // Can not access the base element while not in the UI thread.

            item.Index = base.Count;
                base.Add(item);

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
        public int Index { get; set; }
        public Uri Image_Uri { get; set; }
        public Uri Image_Thumb_Uri { get; set; }
        public string Self { get; set; }

        public BitmapImage Image { get; set; }

        public String Title { get; set; }
        

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        System.Windows.Threading.Dispatcher Dispatcher;

        public MediaUrl(String title, String imageUri, String imageThumbUri, string self_url, System.Windows.Threading.Dispatcher disp)
        {
            Image_Uri = new Uri(imageUri);
            Image_Thumb_Uri = new Uri(imageThumbUri);
            image_retrieved = true;
            this.Title = title;
            Self = self_url;
            failed = false;
            Dispatcher = disp;
        }


        

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {

            this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

    }



}