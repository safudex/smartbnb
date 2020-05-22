# Collateral Provider
> A client implementation for collateral providers

## Set up
1. Install the dependencies with `npm install`
2. Set up a redis instance
3. Set up the following environment variables:
  - NEO\_PRIVATE\_KEY: Private key of the registered collateral provider on NEO. This key should own some GAS in order to send proofs to contest a challenge
  - CONTRACT\_SCRIPTHASH: ScriptHash of the NEO smart contract that holds smartBNB
  - REDIS\_URL: Url of the redis instance, should be unprotected
  - BNC\_PRIVATE\_KEY: Private key of the Binance Chain account that will keep custody of the tokens
  - BNC\_ASSET: Binance Chain ID of the asset that will be ported through smartBNB
  - EMAIL\_ADDRESS: Email address where special alerts will be sent in case the owner needs to take action on something

## Run
```bash
CONTRACT_SCRIPTHASH="5b707..." # Replace with the scripthash of the contract you deployed
NEO_PRIVATE_KEY="..." # Replace
REDIS_URL="..." # Replace
BNC_PRIVATE_KEY="..." # Replace
BNC_ASSET="..." # Replace
EMAIL_ADDRESS="..." # Replace
export NEO_PRIVATE_KEY CONTRACT_SCRIPTHASH REDIS_URL BNC_PRIVATE_KEY BNC_ASSET EMAIL_ADDRESS
npm start
```

## Deploy
[![Deploy](https://www.herokucdn.com/deploy/button.png)](https://heroku.com/deploy)
