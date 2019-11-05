# smartBNB
> Smart contracts on Binance Chain via a decentralized sidechain bridge with NEO

## What?
SmartBNB is a project that enables Binance Chain tokens to use smart contracts in the NEO blockchain.

## How?
1. Binance Chain tokens are ported to the NEO blockchain via a trustless two-way peg that relies on SPV proofs and collateralization
2. Once in NEO, these ported tokens can be used in any smart contract available there
3. Finally these tokens can be redeemed for their real counterparts at any point in time.

## When?
We have encountered some difficulties regarding the signature checking, as Binance Chain uses the secp256k1 elliptic curve for cryptography and NEO uses NIST P-256. This means that Binance Chain's validators sign their blocks using the secp256k1 curve, but NEO smart contracts only provide native functions for the verification of NIST P-256 signatures. To overcome this we are currently studying the inner workings of these sognatures in order to be able to implement a secp256k1 verifier inside a NEO smart contract.
