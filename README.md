# PickleTrick

This is a rewrite of TrickEmuS2, making it another Trickster Online Season 2 emulator. The goal of this project is to emulate the server of Trickster Online, but with a better codebase than TrickEmuS2's.

PickleTrick is in a very early stage, yet the core is somewhat battle-tested. There are some bugs to iron out, but it is mostly stable. Some obstacles to figure out:

* Whether the config format is sufficient for the future
* The entire channel and game servers

### Progress

At the current moment, the only thing the server can do is send a server list packet with dummy data. Please see the section about PickleTrick.FirstLoginServer below. Contributions are welcome!

### Getting Started

Compile the projects and copy over the `*.toml` files to the folder the server binaries are present in. Edit the files to match your server's configuration.

You'll want to patch your client with the (very undetailed) instructions provided below in the "Client" section.

The server uses MySQL with development being done mostly in MariaDB. As there's no actual stuff going on with the database yet, there's no database schema or SQL file provided.

### Client

`dummy` should be used as your password if you aren't able to patch the login code to use your actual password. This is due to Trickster's SSO system being used in the past.

### PickleTrick.FirstLoginServer

When you load the solution, you might get an error about a project named PickleTrick.FirstLoginServer. That project is a reimplementation of the official FirstLoginServer that isn't included with PickleTrick. It was made to test out the PickleTrick core.

That's the reason you can see `Preconfigure` in the `ServerApp` class. FLS uses SQL Server due to it being a replacement for the official server, so you can optionally choose to connect to SQL Server with a call to `Database.Setup` in `Preconfigure`. If you want to use the PickleTrick core to write your own FLS implementation, this is what you should do.
