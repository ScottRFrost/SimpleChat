# SimpleChat
Proof of concept SignalR + WebRTC Discord Clone.  Tested on Firefox & Chrome on desktop, Chrome on Android, and Safari on iOS.

## Background
We were talking at lunch one day about the Discord ID / face scan requirement and how there weren't any great alternatives.  I said 'Some kind of simple text and voice chat can't be that hard, I wonder how hard it actually is'.  So to find out, I built the initial commit of this (peer to peer voice, text chat) in about 4 hours.  The voice chat really is peer to peer, which makes the latency must lower than any client-server solution, but it also means it probably won't scale past 32 or so users per voice channel, as each user has to send their voice chat to every other user every time they say something.

## Technology Stack
It should run anywhere ASP.NET runs, Windows / Linux / MacOS / etc.
- .NET 10: Powering both the Server-side and client-side.
- Blazor WebAssembly: Client-side interactive UI.
- SignalR: Real-time signaling and text chat.
- WebRTC: Peer-to-peer low-latency audio streaming.
- SQLite: Database for user and text chat persistence.
- Tailwind CSS: Modern, responsive styling.

## Preview image
<img width="1353" height="826" alt="image" src="https://github.com/user-attachments/assets/f145d37f-2eb7-4a04-bcc6-5693f2d6b797" />

## Feature ideas / other throughts for improvement
This is really just a proof of concept to see how difficult it would be and for fun, but we had some ideas for possible improvements (PRs welcome!)
- Better detection of silence / minimum loudness threshold before transmitting voice
- Echo cancellation
- Encrypt the voice chat
- Add features to the text chat like hotlink, attaching image files, bold etc
- User profile page to edit username, password, profile picture, etc for your user after registering
- Server settings like a password required to register a new user account to make your server private
- Mic / input selection and volume control
- Soundboards / other dumb stuff from Discord
