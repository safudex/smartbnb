# Collateral Provider Client
> An implementation of a client for collateral providers

## Set up
1. Create a private key with any NEO wallet (eg: [ansy](https://snowypowers.github.io/ansy/))
2. Compute the address associated with the private key you just created, you can use neon-js
3. Install the dependencies with `npm install`

## Run
```bash
PRIVATE_KEY="9ab7e154840daca3a2efadaf0df93cd3a5b51768c632f5433f86909d9b994a69" # Replace with the private key you generated during setup
CONTRACT_SCRIPTHASH="5b7074e873973a6ed3708862f219a6fbf4d1c411" # Replace with the scripthash of the contract you deployed
BNC_TICKER="BNB_BTCB-1DE" # Replace with the Binance Chain ticker of the BEP2 token that the oracle needs to serve
export PRIVATE_KEY CONTRACT_SCRIPTHASH BNC_TICKER
node index.js
```

## Deploy
[![Deploy](https://www.herokucdn.com/deploy/button.png)](https://heroku.com/deploy)
