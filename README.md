# Process #

I went for a ASP.NET Hosted Blazor WASM solution with pre rendering and PWA for offline use

## Server ##

So, since this is all API based there's actually no need for a server, this could easily be a fully clientside app, but in order to show some caching dynamics I decided to go with a Server implementation

Since the data doesn't change, I decided to cache as much as I can, the api returns tons of out of scope data, so first I trim all that, and cache the results, much smaller than the initial request

Since it is much smaller and cached in memory, it should produce sub ms response times, ignoring network related latency of course, much faster than pulling it straight from the API

I'm also using pre rendering here to allow for the best user experience possible, with state persistance too, to avoid any flashing when the data is loading on the client again

## Client ##

The Client uses a good ol manifest to allow for PWA features, the app works offline as long as the data was requested at least once while it was online

For the grid I went with QuickGrid which is a reference grid provided by microsoft in the Microsoft.AspNetCore.Components.QuickGrid namespace

For the data fetching I used a 3 layer approach here, first I check for the pre render persistance layer, then local storage and then I fetch from remote

I also made the app usable on mobile, it's not the pretties app ever but it's functional, I'm good with CSS I just suck at coming up with a pretty design myself, but I can make basically any design you give me happen

And I'm not using the latest version of chartjs but the one I'm using has a pretty nice wrapper I've used before with

## Generators ##

I used my generators to speed things up and show some range, I host them publicly [here](https://pkgs.dev.azure.com/willinton006/WP/_packaging/WP/nuget/v3/index.json) but I trimmed them down and included them in the project just in case you wanted to take a peek at them

## Tests ##

For testing I went with xUnit, just a few tests to give you an ide of how I usually roll

## Other considerations ##

The app requires .NET 7 and VS2022 to run, if you encounter any issues trying to run it let me know

And I took longer than expected because I wanted to make sure I showed some range with the map, the charts, the pre rendering and the generators

Also there's a few 404s on the apps initialization, just some unnecesary css (bootstrap requests it) stuff and the favicon, but I left it as is cause it seems out of scope