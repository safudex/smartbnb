**NEO plugin forked & modified from [CityOfZion/neo-plugins-coz](https://github.com/CityOfZion/neo-plugins-coz/)**

# NeoPubSub

A plugin for neo-cli to publish smart contract events to a Redis PubSub queue

### Author
hal0x2328

### Installation
```bash
cd NeoPubSub
dotnet publish -c Release
cp -r ./NeoPubSub {neo-cli folder}/Plugins
cp ./bin/Release/netstandard2.0/publish/NeoPubSub.dll {neo-cli folder}/Plugins
cp ./bin/Release/netstandard2.0/publish/StackExchange.Redis.dll {neo-cli folder}/Plugins
cp ./bin/Release/netstandard2.0/publish/System.Threading.Channels.dll {neo-cli folder}/Plugins
cp ./bin/Release/netstandard2.0/publish/System.Diagnostics.PerformanceCounter.dll {neo-cli folder}/Plugins
cp ./bin/Release/netstandard2.0/publish/Pipelines.Sockets.Unofficial.dll {neo-cli folder}/Plugins
# cp ./bin/Release/netstandard2.0/publish/System.IO.Pipelines {neo-cli folder}/Plugins # Already a dependency of neo-cli
```

### Usage with websockets
```bash
cd redis2ws
npm install
npm start
```

You can then connect to the websockets server available at `ws://localhost:8000/` and will receive all the events triggered inside smart contracts.

**Note**: This plugin guarantees that all the events broadcasted have been triggered inside a non-FAULTy transaction, therefore consumers of these events don't need to implement any extra code that checks that.
