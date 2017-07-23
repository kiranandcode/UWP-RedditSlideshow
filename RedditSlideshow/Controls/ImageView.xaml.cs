using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace RedditSlideshow.Controls
{

    public sealed partial class ImageView : UserControl
    {

        public event EventHandler Click;


        public ImageView()
        {
            this.InitializeComponent();
            MainGrid.Tapped += Clicked;
        }

        private void Clicked(object sender, RoutedEventArgs e)
        {
            var eventHandler = this.Click;
            if(eventHandler != null)
            {
                eventHandler(this, new EventArgs());
            }
        }
        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Title.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(UserControl), new PropertyMetadata(0));



        public Uri Thumb
        {
            get { return (Uri)GetValue(ThumbProperty); }
            set { SetValue(ThumbProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Thumb.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ThumbProperty =
            DependencyProperty.Register("Thumb", typeof(Uri), typeof(UserControl), new PropertyMetadata(0));

    }
}
