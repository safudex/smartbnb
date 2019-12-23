# SmartBNB 2.0
# Actors
- **Alice**: A normal user using the system to obtain SBNB
- **Collat**: A user providing collateral for the SBNB bridge
- **Fisher**: Could be anyone, but it will probably be a watchtower that checks all the movements in the smart contract and triggers fraud mechanism (also gets rewarded for that)

# Basic protocol
## BNC -> NEO
1. Alice activates smart contract (gets X_1 minutes of time to send bnb to Collat)
2. Alice sends bnb to Collat and sends a proof of it to the smart contract, which verifies it

## NEO -> BNC
1. Alice burns sbnb in the SC
2. Collat listens to NEO, detects burning and sends tokens to the address provided on BNC, then sends a proof of the sending to the smart contract, which verifies the proof  
**Fraud**: If Collat doesn't provide the proof in X_5 minutes [24 hours], then all its collateral is distributed among the holders of SBNB upon which it is collateralized, while all these SBNB get burned

## Problems:
This approach has three main problems:
- Failure to provide proof burns all the SBNB collateralized with the slashed Collat: This mechanism requires the SC to keep track of who's the collateralizer associated with every piece of SBNB in circulation, it generates bad UX for the users that suddenly see their SBNB disappear from their wallets and it is horrible for any contract that keeps SBNB locked, as it can suddenly disappear, creating a potential attack vector for the contract.
- Costs: The costs of verifying on-chain a proof of funds being sent on the Binance Chain can easily costs over 30 GAS (~30$ at press time), making the protocol extremely expensive to execute.
- Long-term use: Given that the collateral is not rebalanced, given a long enough period of time the tokens will very likely become undercollateralized, at which point the collateral providers will very likely steal the funds. Also, it provides no way for the collateral providers to exit their position, making them wait for their users to withdraw the collateral. 

# Fraud-proof based protocol
This new iteration of the protocol addresses the cost problem by making the protocol punishment-based, it will optimistically assume that everything is correct while allowing the counterparty to challenge any step. Only when a challenge is put forward the proofs will need to be sent and verified, with the corresponding punishment being applied to one party or another depending on whether the proof or the challenge is found to be wrong. Due to the incentive structure and the fact that **Fisher** will always be incentivized to challenge wrong proofs, the protocol should always execute along the happy path, without having to perform any costly proof verification.  
It also fixes the first problem (SBNB burning) through the introduction of an atomatic liquidation mechanism.  
This new protocol also adds a punishment deposit to allow for failures on Collat's side (which could be non-malicious such as those caused by downtime) without having these trigger a liquidation of all of Collat's assets.  

## Punishment deposit
Collat can choose to lock P amount of GAS in the contract as a security deposit. If Collat is punished this deposit will be taken and sent to the creator of the challenge that led to the punishment, without further prejudice for Collat. In the event of this deposit not being present when Collat is punished his position will be slashed and all his collateralized assets will be liquidated.  
This deposit is totally optional and can be withdrawn at any time, as is only meant to lessen the risk on Collat's side in the event of non-malicious failure.  
**Note**: P must be greater than the cost in GAS of executing a proof verification.

## BNC -> NEO
1. Alice activates smart contract (gets X_1 minutes of time to send BNB to Collat)
2. Alice sends bnb to Collat
3. Collat listens to BNC, sends message to SC saying that it has received bnb -> SC mints SBNB  
**Fraud**: If Collat doesn't send the message after X_2 minutes, Fisher will send the proof -> bnb is minted, Collat is punished and Fisher rewarded  
**Warning**: A mechanism should be put in place to make sure that users don't send new bnb while other bnb has been sent but it hasn't been acknoweledged in the contract, leading to undercollaterization. One way of doing this is by having clients calculate the maximum amount that they can send to Collat as `collateralized_funds/1.5-(SBNB in circulation+sum of SBNB sent to Collat in the last (X_2+K) minutes)` where `K` is a contant that creates a safety buffer.  

## NEO -> BNC
1. Alice burns sbnb in the SC while providing a BNC address
2. Collat listens to NEO, detects burning and sends tokens to the BNC address provided by Alice  
**Fraud**: If Collat doesn't send tokens in X_3 minutes, Fisher can provide a punishment deposit to the SC and request that Collat send proof of the sending in X_4 minutes. If Collat doesn't provide proof or the proof provided has a timestamp later than X_3 (it sent the tokens after the interval when it should have sent them), Collat gets punished.  
**Assumption**: NEO and BNC timestamps in block headers are synchronized up to a certain precision.  

**Danger**: Punishment money shouldn't come from the collateral, as that can lead to all that money being stolen by Collat, undercollateralizing the contract and stealing the BNB.  

## Node
- Listen for events on BNC

# Long-term support protocol

## Decentralized oracle
An oracle of the price of the BNB/NEO pair is needed in order to maintain the collateral deposits at an appropiate level in order to avoid undercollateralization. One way to decentralize this oracle is by building a Uniswap-like exchange pair on NEO that enables trading of NEO for BNB and viceversa, the price of the pair could be taken as the median[TODO: is there a better measure?] of the price used in buys/sells of the pair over a long period [how long this period should be needs more research to be decided]. Attacks on the price that try to manipulate it would require selling or buying large quantities of the assets at prices that would be really different from the standard, at which point other actors could appear and arbitrage that pair with other exchanges, making a huge profit at the expense of the attacker, meaning that attacks would cause huge capital losses for the attacker.  

Such oracle would make the system decentralized but it would require:
- Asset pools that are large enough
- Constant activity trading activity on the pair
- Actors which arbitrage the pair with other exchanges

Leading to a Catch-22 situation in which trading volume is needed in order to get large asset pools and arbitrators but users will only pool assets and arbitrage the pair if it has a large volume. For this reason the oracle might be implemented initially as a centralized oracle that simply reports on the price seen in different exchanges.  

# Asset liquidation

