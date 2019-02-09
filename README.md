# MCI500H
Interface library and GUI for managing the Philips MCI500H hifi system.

## Intent
A while back, I had picked up a MCI500H hi-fi system (which I often tend to refer to as stereo), that allows storing content on the included harddisk.
Usually you would transfer new tracks via an acient piece of Windows software that originated from Philips India division in 2007.
Said software did never have many features to begin with - which is also obvious, it uses a smaller set of the full WADM API) and stopped working on later versions of Windows such as on Windows 8 (yes, the original software is Windows only...), as the networking functionality ceased to work properly and reliably.
This rendered the nice hi-fi system basically unusable in this day and age, so I decided to go do something about it.
Work on this started several months back, when I got the original application working inside an old virtual machine.
I decided to go through every single function inside the GUI and capture the individual transactions with Wireshark.
Then, I started manually analyzing the traffic between the hifi and the application, which turned out to be sad-looking XML REST.
As usually, there was no documentation available and Philips had retired the product previously.
During the entire process, I used different means to get my head around the whole architecture and system.
This library is a work in progress and is constantly updated. The API might change at any moment until the first release.
The final goal consists of writing a GUI application to be able to manage the hifi from Windows again.
There will also be an OSX and Linux command line management utility and potentionally a GUI port further down the road.

## Roadmap
* Finish implementing all API calls
* Implement the media upload protocol
* Finish the cover art scaling
* Figure out a few more details about some calls
* Write the abstraction layer for library browsing
* Write the abstraction layer for library management
* Start working on the GUI
* Start working on the query language
* Start working on the command line utility
