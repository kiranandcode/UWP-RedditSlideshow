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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace RedditSlideshow.Views
{
    
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Slideshow : Page
    {
        
        Boolean MenuExtended = false;

        public Slideshow()
        {
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
        }
    }
}
