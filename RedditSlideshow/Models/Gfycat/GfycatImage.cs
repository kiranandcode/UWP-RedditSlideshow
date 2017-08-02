using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedditSlideshow.Models.Gfycat
{

    public class Gfyobject
    {
        public Gfyitem gfyItem { get; set; }
    }

    public class Gfyitem
    {
        public string gfyId { get; set; }
        public string gfyName { get; set; }
        public string gfyNumber { get; set; }
        public string userName { get; set; }
        public string width { get; set; }
        public string height { get; set; }
        public string frameRate { get; set; }
        public string numFrames { get; set; }
        public string mp4Url { get; set; }
        public string webmUrl { get; set; }
        public string webpUrl { get; set; }
        public string mobileUrl { get; set; }
        public string mobilePosterUrl { get; set; }
        public string posterUrl { get; set; }
        public string thumb360Url { get; set; }
        public string thumb360PosterUrl { get; set; }
        public string thumb100PosterUrl { get; set; }
        public string max5mbGif { get; set; }
        public string max2mbGif { get; set; }
        public string mjpgUrl { get; set; }
        public string gifUrl { get; set; }
    }

}
