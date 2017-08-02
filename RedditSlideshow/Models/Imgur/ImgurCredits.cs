using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RedditSlideshow.Models.Imgur
{

    public class Creditsobject
    {
        public Data data { get; set; }
    }

    public class Data
    {
        public int ClientLimit { get; set; }
        public int ClientRemaining { get; set; }
    }

}
