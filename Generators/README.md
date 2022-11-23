# Process #

So these generators are trimmed down versions of the ones I use

I removed API Versioning and all auth related stuff, just kept the basics

They basically allow for a single interface to represent the data transfer between the server and the client, and a single implementation of it on the server to get said data, everything else (controllers on the server and http requests on the client) is handled by them

I could have gone the classic route but I want to show some range on what I can do