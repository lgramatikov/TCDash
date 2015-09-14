# TCDash
TeamCity dashboard for Windows 10 IoT

Proof of concept code for making a bit more complete app running on Windows 10 IoT on Raspberry Pi. Most of the code is based on
(i.e. copy-pasted from) Microsoft examples. The app loads project and build data from TeamCity using its REST API.
Then renders everything on the screen. What is in:

1. Pretty horrible code for rendering tiles on screen. I just can't figure out a proper way to use XAML with binding and grid
that takes the whole screen.

2. Support for PIR sensor, which "lights-up" the board if someone walks by. PIR connection is pretty simple and based on 
examples from Adafruit (https://learn.adafruit.com/pir-passive-infrared-proximity-motion-sensor/overview).

3. Simple way to do WebHooks as it takes a lot of calls to TeamCity API to collect all required information. WebHook "should" work
with https://github.com/tcplugins/tcWebHooks. Still have to test this with real TC setup.

4. AllJoyn server, just for fun.

5. Azure Application Insights for monitoring and exceptions log. Surprisingly, AI works fine on Raspberry.

And I know, that the code needs pretty big refactoring before it can be called "usable" or "stable".
