# smartBNB
> Smart contracts on Binance Chain via a decentralized sidechain bridge with NEO

## TL;DR
SmartBNB is a project that enables Binance Chain tokens to trustlessly use smart contracts in the NEO blockchain through sidechains.

## What is smartBNB?
SmartBNB is **a trustless bridge that enables tokens on Binance Chain to be transferred to Neo’s blockchain and back**. This enables tokens issued on Binance chain to use smart contracts in the Neo blockchain. As such, token issuers can benefit from Binance Chain’s DEX while taking advantage of Neo’s smart contracting capabilities.

Sending tokens from Binance Chain to Neo is done by locking tokens on Binance Chain, at which point the same number of tokens are minted on Neo. Once tokens are minted on Neo, they can be used for Neo-based smart contracts. User can later redeem tokens by burning them and unlocking the original tokens on Binance Chain. A collateral-based system is used to ensure that, in the event of any irregularity, users are fully compensated.

## How does it work?
Like sidechains, SmartBNB system employs a lock=mint and burn=unlock system. The protocol ensures the secure locking of tokens on Binance Chain by using fully-collateralized custodians.

First, custodians have to deposit GAS tokens as collateral in a smart contract. These custodians can then receive and safekeep tokens from SmartBNB users who want to port tokens from Binance Chain to Neo.

When a user sends tokens to a custodian on Binance Chain, they are locked in the custodian’s vault. The protocol then mints new tokens on Neo to represent the locked tokens and sends them to the user. Later, users can burn their Neo-based tokens to retrieve their original tokens on Binance Chain. The custodian is forced to send these tokens back to the users, as the custodian will lose his/her collateral if this doesn’t happen.

Because of this collateral, which is set to be more than 150% of the locked tokens, the protocol is trustless. Custodians are heavily incentivized to behave honestly, as they will incur significant financial losses if they don’t. Users are fully protected from dishonest behavior by custodians, as their collateral would be given to affected users.

Custodians are rewarded for their custodial services. Anyone can become a custodian by depositing Neo as a bond. For the first time, it is possible to receive a return on capital that is locked on the Neo blockchain.

For more information, check out [our protocol specification](https://github.com/safudex/smartbnb/blob/master/protocol.md).

## Deploy smartBNB for your own token
1. Change the following constants inside [Contract.cs](./Contract/smartBNB/Contract.cs):
	- PriceOracle (you will need to generate the private key to be used in the oracle contract)
	- Denom
	- Name()
	- Symbol()
2. Deploy a full node along with [NeoPubSub](https://github.com/corollari/neo-PubSub)
3. Deploy the updated Contract.cs to MainNet or TestNet
4. Deploy a [price oracle for your token](https://github.com/corollari/neo-oracle)
5. Deploy a [collateral provider](./collatClient)
6. Deploy a [system that sends alerts on collateral liquidations](./liquidationAlertsBot) for arbitrageurs
