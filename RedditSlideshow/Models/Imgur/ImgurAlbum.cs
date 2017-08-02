using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedditSlideshow.Models.Imgur
{

    public class Albumobject
    {
        public Data2 data { get; set; }
        public bool success { get; set; }
        public int status { get; set; }
    }

    public class Data2
    {
        public string title { get; set; }
        public bool favorite { get; set; }
        public bool nsfw { get; set; }
        public object section { get; set; }
        public int images_count { get; set; }
        public bool in_gallery { get; set; }
        public bool is_ad { get; set; }
        public Image[] images { get; set; }
    }

    public class Image
    {
        public string id { get; set; }
        public object title { get; set; }
        public string link { get; set; }
    }

}
