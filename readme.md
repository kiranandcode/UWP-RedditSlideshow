# Gopiandcode's Reddit Slideshow (unofficial)
## A small UWP slideshow application.
![The Application Logo][logo]

Cool, so this is a further development on my initial Reddit Slideshow Web App. It was quite fun moving from the dynamic weakly typed nature of JS back to C#.
I'm slightly ashamed to admit it, but briefly I almost missed the JS style. However I can now see why JS is the lingua franca of
the web - especially when dealing with JSON responses from the reddit API I realized that the dynamic nature of the responses
from a JSON endpoint weren't a good match with the type security of a static language and even with a strongly typed dynamic language
like Python, when your end goal is to provide a usable interface to the user, the silent failures of JS could be preferable.

If only there were a language which provided type safety while also providing functionality to set a type as an entry point to
a weak type. I haven't thoought deep enough into that idea to ascertain whether it is actually a worthwhile train of thought.

Developing on C# was suprisingly fun - I found that it incorperated a nice mix of both C++ and Java concepts making it an easy
pickup for a developer mainly experienced in Java, not C++ - although I should point out that I wasn't really using many of the
C++ inspired features in this project.

## Design
Unlike many of my prior projects, I didn't need much planning for this project. In terms of system design, as I was working with a framework, most of the UI
planning was already done for me - otherwise as I hadn't any prior experience of using the framework, I couldn't really plan for
how objects should work in this kind of application.

In terms of the UI design, as this was essentially a port of my web app, I simply reused much of the design from that. Looking back
I'm actually suprised at how close the UI structure of both projects are despite being on completely different sytems.

## Development
Development of this application was a veritable rollercoaster ride of emotions, from frustration to exhilaration and all between.
Some features, while absolutely insignificant ended up taking ages. Particularly of note is the blurring on the slideshow images as
the selection menu pops up.

Another fun experience was the development of the Image class.

### Development of the Image App
As I was developing the application on a local platform, I wanted to also encorperate buffering features (yes I know I could have done this on the web one as well, but developing locally I had no excuses).
Initially I tried to implement it by encapsulating the storage and retrieval of the image into the class. When retrieve content was called the class would request an image from it's url and store it locally. However this ran into memory issues as I never removed the images.

Then as a quick fix, I made the application delete the 2nd previous image if the memory use was too high(>500mb) - this was a
foolish decision as the user was also provided the ability to jump to random positions throughout the list of images. This meant that
if the user jumped around they could end up crashing the application from a memory excess. I also converted my manual stream/byte based
image retrieval functions to use the Image class supplied by the framework, as this would encapsulate that functionality, and also
according to the MSDN would allow for image reuse - as in if the same image is fetched from multiple locations, only one copy would be 
required.

After this, I spent some time thinking through the image retrieval process (making sure to 'commit' this to memory as Image prefetching
had been a subject I have thought about in a project previously) and came up with a better method. The Image list keeps track of the 
direction the user has just taken and uses it to prefetch the next image in that direction - the list also keeps a reference to the image
it has prefetched. If the user jumps, their direction is set to none, and the previoiusly prefetched image is deleted. Then once the
user starts moving again and a direction has been established, I restart the prefetching.

Even after this laborious task, I still kept on running into crashes. After spending some while in the debugger, I isolated the
issue down to a memory spike at generation. What was interesting about this spike was that it didn't occur in the request json from
reddit loop, but was also proporitional to the number of subreddits being requested. After about 2 hours of headscratching I finally noticed
the issue - oh and what a frustrating issue it was - it turns out I had placed a listener on the Image list too early, which meant
that as I populated the list, the callback would get fired like crazy, each time opening a stack frame again and again. This would 
cause the memory to exceed the limit and cause the crash. After a simple text movement, the application would work perfectly again.



## Screenshots
![Landing Page][landingpage]

The Landing page for the application. The Logo positioning has been ~~frustratingly~~ carefully adjusted to look good on all sizes.

![Image View Page][imageviewpage]

How the actual slideshow looks. Potentially in the future I'll make the icons fade on a lack of mouse movement.

![Options Page][optionspage]

Selecting options - also notice the blur effect on the image. Took me friggen ages to get right.

![On the Start Menu][startmenu]
Ahh man. Looks so good to have it on a start page like a real application!


[logo]: https://raw.githubusercontent.com/Gopiandcode/UWP-RedditSlideshow/master/docs/images/AppLogo.png "Reddit Slideshow Unofficial"
[landingpage]: https://raw.githubusercontent.com/Gopiandcode/UWP-RedditSlideshow/master/docs/images/LandingPage.png "Landing Page"
[imageviewpage]: https://raw.githubusercontent.com/Gopiandcode/UWP-RedditSlideshow/master/docs/images/ImageViewScreen.png "Image View Page"
[optionspage]: https://raw.githubusercontent.com/Gopiandcode/UWP-RedditSlideshow/master/docs/images/SelectionScreen.png "Options Page"
[startmenu]: https://raw.githubusercontent.com/Gopiandcode/UWP-RedditSlideshow/master/docs/images/OnStartScreen.png "Application on the Start menu"

