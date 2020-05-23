# SmartBNB 2.0
# Actors
- **Alice**: A normal user using the system to obtain SBNB
- **Collat**: A user providing collateral for the SBNB bridge
- **Fisher**: Could be anyone, but it will probably be a watchtower that checks all the movements in the smart contract and triggers fraud mechanisms (will get rewarded for that)

# Basic protocol
## BNC -> NEO
1. Alice locks f(Y) NEO in the SC, requesting the porting of Y SBNB and locking Y*alpha collateral NEO for X_1 minutes (Alice gets X_1 minutes of time to send bnb to Collat)
2. Alice sends bnb to Collat
3. Alice a proof of the BNB send to the smart contract, which verifies it
4. Smart contract mints SBNB and sends it to Alice

## NEO -> BNC
1. Alice burns SBNB in the SC
2. Collat listens to NEO, detects burning and sends tokens to the address provided on BNC, then sends a proof of the sending to the smart contract, which verifies the proof  
**Fraud**: If Collat doesn't provide the proof in X_5 minutes [24 hours], then all its collateral is distributed among the holders of SBNB upon which it is collateralized, while all these SBNB get burned

## Problems:
This approach has four main problems:
- **Failure to provide proof burns all the SBNB collateralized with the slashed Collat**: This mechanism requires the SC to keep track of who's the collateralizer associated with every piece of SBNB in circulation, it generates bad UX for the users that suddenly see their SBNB disappear from their wallets and it is horrible for any contract that keeps SBNB locked, as it can suddenly disappear, creating a potential attack vector for the contract.
- **High costs**: The costs of directly verifying on-chain a proof of funds being sent on the Binance Chain can easily costs over 56000 GAS (~56000$ at press time), making the protocol extremely expensive to execute.
- **Long-term use**: Given that the collateral is not rebalanced, given a long enough period of time the tokens will very likely become undercollateralized, at which point the collateral providers will very likely steal the funds. Also, it provides no way for the collateral providers to exit their position, making them wait for their users to withdraw the collateral. 
- **Requirement on initianl NEO ownership**: When users want to port BNB into SBNB they need to own NEO along with the BNB that they will port, as an small amount of NEO is needed initially to be locked in order to prevent spamming attacks. This requirement forces users that may not own any NEO to buy it just for this, which lengthens severely the onboarding process.

We will address each of these problems in the following sections:

# Lowering costs: Fraud-proof based protocol
This new iteration of the protocol addresses the cost problem by making the protocol punishment-based, it will optimistically assume that everything is correct while allowing the counterparty to challenge any step. Only when a challenge is put forward the proofs will need to be sent and verified, with the corresponding punishment being applied to one party or another depending on whether the proof or the challenge is found to be wrong. Due to the incentive structure and the fact that **Fisher** will always be incentivized to challenge wrong proofs, the protocol should always execute along the happy path, without having to perform any costly proof verification.  

## Punishment deposit
Collat must lock n*P amount of GAS in the contract as a security deposit, where €n>=1€ and Collat can choose to increase €n€ in order to be able to handle more proof challenges concurrently. If Collat is punished this deposit will be taken and sent to the creator of the challenge that led to the punishment, along with any liquidation required in order to returns the funds to the user. 
Any additional deposits after the first one are totally optional and can be withdrawn at any time, as they are only meant to allow Collat to respond to an attack where several challenges are opened against him with the objective of locking all his collateral up.  
**Note**: P must be greater than the cost in GAS of executing a proof verification, a constant value for it is provided later.

## BNC -> NEO
In the BNC to NEO port, the following protocol will replace steps 1, 2 and 3 of the protocol described in `Basic protocol`:
1. Alice locks f(Y) NEO in the SC, requesting the porting of Y SBNB and temporarily locking Y*alpha NEO of Collat's collateral, along with P NEO from Collat's punishment deposit for X_1 minutes (Alice gets X_1 minutes of time to send bnb to Collat)
2. Alice has X_1 minutes to send BNB to Collat, the protocol needs to wait for this timeout to expire to continue to the next step
3. Collat has X_6 minutes to signal to the smart contract that Alice has deposited the funds
	- If Collat signals to the smart contract that the deposit has been accepted, the collateralized Y*alpha NEOs get re-locked as collateral funds, SBNB are minted, the initial f(Y) NEO deposit is returned to Alice and the protocol ends.
	- If Collat doesn't activate any function inside the smart contract it is assumed that no deposit has been made and the protocol continues.
4. Alice or Fisher (open to anyone) have X_7 minutes to send a transaction to the smart contract that locks P NEOs and provides proof of a transaction on BNC that sent Y BNB to Collat during the X_1 period, challenging the information reported by Collat (that no deposit was made)
	- If nobody sends a challenge in both timeframes, the temporarily-locked Y*alpha collateral is unlocked, the f(Y) deposit initially created by Alice is burned and the protocol ends.
	- If a challenge is received the protocol continues. The proof provided should include a merkle path connecting the transaction to a BNC blockheader, 8 signatures (2/3 of the total number of  BNC validators) from BNC validators signing that header and intermediate states of the computations that need to be performed to verify the proof. Sending all that data to the smart contract and storing it on its storage costs around 120 GAS.
5. Collat has X_8 minutes to  send a message to the smart contract referencing two consecutive intermediate states of those provided in the previous step for which the transition between them results in a different state than the provided, that is, two states €E_i€, €E_{i+1}€ such that €f_i(E_i)!=E_{i+1}€ where €f_i€ is the function used to compute the next state at step i. That smart contract call will execute the specific transition code between those two states using the first as input and comparing the output with the second state. The on-chain computation of all transition functions has a GAS cost that is always lower than 130 GAS.
	- If the on-chain computed result is different from the second referenced state, it has been proved that the proof is faulty and therefore Collat wins the challenge, unlocking the locked collateral and transferring the deposit consisting of P NEOs provided by the other party to Collat with the goal of compensating his losses.
	- If no message is sent in the X_8 time interval or the result computed on-chain matches the second referenced state, Collat is said to have lost the challenge, which leads to P NEOs from it's punishment deposit being awarded to the creator of the challenge and the Y*alpha locked collateral being liquidated in order to compensate Alice for the stolen deposit.  
	- **Note**: It is trivial to prove that if the data provided in step 3 is not a correct proof then either the final result of the verification is negative (this is easily checkable as the final result will be provided as one of the intermediate states mentioned earlier) or the computation wasn't executed correctly, which implies that one of the transition functions used to create the intermediate states differs from the one used in the smart contract therefore at least one of those states must be wrong. Collat can trivially find which of the states is wrong by running the correct transition functions locally and comparing the results obtained with those provided in step 3 and stored in the smart contract.
6. Go to step 3

**Note**: A possible value for P is 200, as that value would be enough to compensate any of the parties for the costs incurred when verifying the proof (the maxium GAS spent by either party is capped at 130) while providing some extra GAS that would act as an incentive for Fishers to challenge faulty proofs and a deterrent against actors that would submit invalid proofs or challenges.

**Note**: The reason why the protocol enables the creation of other challenges after a challenge has been completed is that doing so prevents an attack where a Fisher affiliated with Collat opens a challenge and purposedly sends a faulty proof, making Collat win the challenge and preventing other challeges on the same deposit. This would result in Collat losing ~240 GAS but managing to steal a deposit without having to face any penalty.

**Warning**: A mechanism should be put in place to make sure that users don't send new bnb while other bnb has been sent but it hasn't been acknoweledged in the contract, leading to undercollaterization. One way of doing this is by having clients calculate the maximum amount that they can send to Collat as `collateral/alpha-(SBNB in circulation + collateral temporarily locked)`. Users must alo make sure that the punishment deposits are not all locked in open challenges, although a user can choose to forfeit that restriction at his expense. 

**Note on picking X values**: The values picked should be large in order to allow the party that should take action to defend itself against DoS attacks against their node and/or against the blockchain (even tho flooding the blockchain with transactions is only a viable strategy if the values of X are really low). An initial proposed value for all X is 24 hours, but the selection of this number has been totally arbitrary and could be improved a lot by analysing data.

**Note on picking f**: A simple function that could accomplish the goal of preventing DoS attacks would be €f(x)=max(x/100, 1)€, which would punish two types of attacks:
- Attacks based on temporarily locking a large amount of collateral by requesting a large deposit, exhausting all the unlocked collateral that a custodian has available and preventing other users from creating new SBNB with them temporarily.
- Attacks that attempt to exhaust the punishment deposits of Collat by requesting the porting of really small amounts of SBNB, which will lock all the punishment deposits of Collat and achieve the same result as the previous attack. This attack can be further prevented by only allowing the porting of a minimum amount of BNB (smaller amounts than the minimum may be allowed but those may require the locking of a punishment deposit).

This function has been chosen without any research backing it and therefore can be improved massively, in the future it will need to be tweaked in order to prevent attacks that may temporarily disable the creation of new SBNB.

## NEO -> BNC
The proofs on the other side of the peg, which would be activated when Collat doesn't send to Alice a quantity of BNB equivalent to the quantity of burned SBNB use the protocol described before, a quick outline of how that protocol is used at the other side of the peg is given:
1. Alice burns sbnb in the SC while providing a BNC address
2. Collat has X_3 minutes to send the required BNB to Alice's address
3. Everyone can submit a challenge to Collat in the following X_9 minutes
	- If nobody submits a challenge it is assumed that Collat has properly sent the BNB and the protocol finishes
	- If a challenge is submitted the protocol continues
4. Collat uploads to the smart contract a proof of a transaction where he sends the required BNB to Alice 
5. The challenge creator points to a supposedly faulty transition and verifies the transition with on-chain computation, deciding the final result of the challenge
6. Go to step 3

**Assumption**: NEO and BNC timestamps in their respective block headers are synchronized up to a certain precision.  
**Danger**: Punishment money shouldn't come from the collateral, as that can lead to all that money being stolen by Collat, undercollateralizing the contract and stealing the BNB. That is the reason behind the existence of the punishment deposits. 

# Long-term support
This is achieved through two mechanisms:
- **Collateralization ratio maintenance**: Collats are allowed to withdraw any collateral that is in excess of the collateralization target €alpha€ and add any amount of collateral that they desire. If the collateralization ratio of their assets ever reaches €alpha_{under}€ (an example constant value for it would be €alpha_{under}=120%€) or lower all their collateral is automatically liquidated.
- **Fees**: SBNB will be continuosly linearly reduced at a per-block resolution. This means that SBNB stored inside smart contracts or wallets may be reduced by 0.00001% per block, for example. The SBNB removed from circulation will be burned, causing a reduction in the amount of SBNB that is collateralized by Collats, effectively giving the fees to them. This fee scheme incentivizes users to take their BNB back to the main chain while rewarding collats based on the length of time that their money has been locked and they have been providing services.
- **Pooled collats**: When creating or withdrawing SBNB, the collat or group of collats that will provide the collateralization for the new coins or will handle the job of sending the BNB on BNC will be chosen by the smart contract, leading to a system where collats are seen as a single entity and are swappable. Also, SBNB will not be tied directly to any specific collat but to the general pool of them.

Furthermore, the system will enable collats to transfer their position as a collat to a different address, hopefully creating a market where collats can move out of their positions whenever they want by selling them to other actors.

## Decentralized oracle
An oracle of the price of the BNB/NEO pair is needed in order to maintain the collateral deposits at an appropiate level in order to avoid undercollateralization. One way to decentralize this oracle is by building a Uniswap-like exchange pair on NEO that enables trading of NEO for BNB and viceversa, the price of the pair could be taken as the median[TODO: is there a better measure?] of the price used in buys/sells of the pair over a long period [how long this period should be needs more research to be decided]. Attacks on the price that try to manipulate it would require selling or buying large quantities of the assets at prices that would be really different from the standard, at which point other actors could appear and arbitrage that pair with other exchanges, making a huge profit at the expense of the attacker, meaning that attacks would cause huge capital losses for the attacker.  

Such oracle would make the system decentralized but it would require:
- Asset pools that are large enough
- Constant trading activity on the pair
- Actors which arbitrage the pair with other exchanges

Leading to a Catch-22 situation in which trading volume is needed in order to get large asset pools and arbitrators but users will only pool assets and arbitrage the pair if it has a large volume. For this reason the oracle might be implemented initially as a centralized oracle that simply reports on the price seen in different exchanges.  
Another possibility is to implement a system that combines both and only allows the price to be used when both mechanisms report the same price (with some tolerance).

## Fee mechanism
The fee mechanism needs to be:
- computationally cheap to compute (only computed when transferring funds)
- should be easy to compute the sum of all balances
- should be as fair and minimal as possible
 
if we were to use a basic compound percentage (reduce tokens by 0.00001% per second, new = oldB*0.9999^n)
 its easy to compute the total sum of tokens at any time exactly if the basic computation is easy
 gets out of the way
 really computationally expensive to compute the updated amount of tokens after some time
  requires computing 0.999999^n which is O(n)
 
if we were to use simple percentage (reduce tokens by 0.00001*n% (constant %), newB = oldB - oldB*0.00001*n)
 easy to compute
 we can keep an updated global sum and keep changing it with the same formula -> easy computation
  this only provides an upper bound on the total sum (updated more frequently), we cant calculate the exact number
 requires people to keep transferring their tokens in order to simulate compound interest -> lose less tokens

# Asset liquidation
The first problem described before (SBNB burning) is fixed through the introduction of an automatic liquidation mechanism.  
Whenever some collateral funds need to be liquidated because either the collateral ratio has sinked too low or a collat has failed to meet some of his responsabilities, the collateral that needs to be liquidated will be offered in exchange for the amount of SBNB that has lost the peg, and after the trade has been completed, the collected SBNB will be burned in order to maintain the peg.
Theoretically this trade should be taken by someone because of the simple fact that it can be arbitraged with other exchanges to gain money.

## Removing the requirement on initial NEO ownership
This can be achieved by replacing the initial locking of f(Y) NEO with a small BNB transaction on BNC that will be used to prove who was first if a dispute where to arise.
More details to be added later.
 
System to start porting process
- the system must satisfy two goals
  - user UX
  - prevent collaterals from being able to censor token portings?
  - prevent spam attacks where collateral money is locked up uselessly and the system is halted
  - prevent attack where collateral steals money
    - (collateral sends a bunch of money at the same time another client is sending money)
 
A basic system would be one where some neo is locked to start a token porting, which causes some collateral to be locked. This neo is returned if the process is succesful and lost if not.
- this solves the last 3 problems but makes the UX shitty as now a user needs to do 2 operations on 2 different blockchains and needs to own two different tokens
 
Another system based on atomic swaps would
- solve the 4th problem
- not solve the 3rd problem unless we allow collaterals to choose the clients that can port tokens, which breaks the 2nd porblem. This is because there is no loss caused by the creation of atomic swaps
- regarding the first problem, the ux is better because you only need to do a single transaction on Binance Chain
 
Yet another system can be based on initially sending a small transaction, which will lock the coins. If the attack is performed on that transactions only the amount of that transaction will be lost.

# Implementation
- [NEO smart contract](./Contract/smartBNB/Contract.cs) ([interface](./Contract/smartBNB/contractInterface.md))
- [Price oracle](https://github.com/corollari/neo-oracle)
- [NEO notifications PubSub system](https://github.com/corollari/neo-PubSub)

# Previous work
- Kyber bridge proposal between BTC and ETH
- XCLAIM
- tBTC
- Portal by Incognito Chain
