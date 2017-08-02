using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedditSlideshow.Models.Imgur
{

    public class Imageobject
    {
        public Data3 data { get; set; }
        public bool success { get; set; }
        public int status { get; set; }
    }

    public class Data3
    {
        public string id { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public int datetime { get; set; }
        public string type { get; set; }
        public bool animated { get; set; }
        public int width { get; set; }
        public int height { get; set; }
        public int size { get; set; }
        public int views { get; set; }
        public long bandwidth { get; set; }
        public object vote { get; set; }
        public bool favorite { get; set; }
        public bool nsfw { get; set; }
        public string section { get; set; }
        public object account_url { get; set; }
        public object account_id { get; set; }
        public bool is_ad { get; set; }
        public bool in_most_viral { get; set; }
        public object[] tags { get; set; }
        public int ad_type { get; set; }
        public string ad_url { get; set; }
        public bool in_gallery { get; set; }
        public string link { get; set; }
    }

}
