# BlackOfWorld's Webkit Library
**WARNING** This library is in pre-alpha development stage! I wouldn't advise anyone to use this until it's fully developed. *testers are appreciated though*


A simple server listener with static files and dynamic files system. Written in .NET Core 2.1 ([What's .net core?](https://en.wikipedia.org/wiki/.NET_Core))
# Features
- [x] HTTP Server listener
- [x] Config
- [x] Events
- [x] Static file system
- [x] Firewall and rate limiting
- [ ] Dynamic file system
# When will SSL support be implemented?
As much as I wish I could, I can't (blame Microsoft). This project is targeted to be cross platform as much as possible. And implementing it right now would be annoying after it's been implemented. Sorry if this bothers you, but believe me. It bothers me more that I can't make this library unique and shiny. ([Implement https connection support for the managed HttpListener](https://github.com/dotnet/corefx/issues/14691))
# Dependencies
I'm trying my best to not use any dependency at all, but for now, the only dependency this library uses is [Microsoft's memory caching extension nuget](https://www.nuget.org/packages/Microsoft.Extensions.Caching.Memory/)
# LICENSE
 * Apache License, Version 2.0
   ([LICENSE](LICENSE) or http://www.apache.org/licenses/LICENSE-2.0)
