using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Windows.ApplicationModel.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Controls;

namespace RedditSlideshowCompatibility
{

    public class Link
    {
        private string url;
        public string Url
        {
            get
            {
                return url;
            }
            set
            {
                url = value;
            }
        }
        public Link()
        {
            url = "";
        }
    }


    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public MainWindow()
        {


      

            InitializeComponent();

            

        }





        private void generateSlideShow_Click(object sender, RoutedEventArgs e)
        {

            if (!String.IsNullOrEmpty(text.Text))
            {

                
                Window Slideshow = new RedditSlideshowCompatibility.Views.Slideshow(this, text.Text);
                Slideshow.Owner = this;
                Slideshow.Show();
                this.Hide();

            }
            else
            {
                ShowMessageDialog("Could not generate Slideshow", "At least one valid reddit url is required to generate a slideshow.");
            }
        }

        private void textBoxLostFocusEventHandler(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.TextBox textbox = sender as System.Windows.Controls.TextBox;

            string text = textbox.Text;

            // Auto format entry to compatible format..
            Regex url_rx = new Regex(@"(?:^(?:https?:\/\/)?(?:www.)?(?:(?:np|np-dk).)?(?:reddit\.com)\/r\/([a-zA-Z0-9_]+)\/?|^(?:\/?r\/)?([a-zA-Z0-9_]+))", RegexOptions.Compiled);
            MatchCollection extracted = url_rx.Matches(text);

            if (extracted.Count != 0)
            {
                StringBuilder sb = new StringBuilder();
                Match match = extracted[0];
                sb.Append("r/");
                if (!String.IsNullOrEmpty(match.Groups[1].ToString()))
                    sb.Append(match.Groups[1]);
                else if (!String.IsNullOrEmpty(match.Groups[2].ToString()))
                    sb.Append(match.Groups[2]);
                textbox.Text = sb.ToString();
            }
            else
            {
                ShowMessageDialog("Url Format Error", "The url submitted was not a valid reddit url.\n" +
                "Urls can be of the form reddit.com/r/[subreddit name] or even r/[subreddit name].");
                textbox.Text = "";
            }


            textbox.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty).UpdateSource();
        }

        private static void ShowMessageDialog(string title, string content)
        {
            string messageboxText = content;
            string caption = content;
            MessageBoxButton button = MessageBoxButton.OK;
            MessageBox.Show(messageboxText, caption, button);
        }


    }
}
