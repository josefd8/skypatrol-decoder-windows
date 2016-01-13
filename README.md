# skypatrol-decoder-windows
Message decoder for the Skypatrol TT8750 written in Visual Basic .NET 

##  Main characteristics

- Completely written in .NET: Everything from the classes for managing the sockets connections to logging.
- Full support for UDP & TCP protocols.
- The interface provides a clear view of the clients connected, last message received and total number of messages transmitted
- All package details are available: latitude, longitude, altitude, heading, speed and I/O ports state
- Now is possible to send messages (commands) to the remote device (Just click the device in the list, write the message and click send)
- In the installation folder, a log file is created automatically to store all data in raw format
- along with the program, 3 different script are provided (all tested on a standard Skypatrol TT8750). The script are pretty simple, and demonstrate the basic settings for TCP and UDP transmissions modes


Take a look of the program in action in the following youtube video (video in spanish)

[https://www.youtube.com/watch?v=xIkegfwP6xo]()


**Final note:** This program will work only with the Skypatrol TT8750 model. However, it can be easily modified to parse any other model or device brand
