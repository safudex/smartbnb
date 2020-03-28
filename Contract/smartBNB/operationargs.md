# Operations

**savestate**
General state
index|arg|info
-|-|-
0|`byte[] portingContractID`|Id.
1|`byte[][] signatures`|Array of signatures.
2|`bigint[] xs `|Decompressed signature x. `[sig0_x, sig1_x...]`
3|`bigint[] ys`|Decompressed signature y. `[sig0_y, sig_y...]`
4|`byte[][] signablebytes`
5|`ulong[][] pres`
6|`ulong[][] preshash`
7|`bigint[] preshashmod`
8|`byte[] txproof`
9|`byte[] blockHeader`|Header data cdc encoded. `len(cdcVersion) + cdcVersion + len(cdcChainID) + cdcChainID + ... + len(cdcProposerAddress) + cdcProposerAddress`
10|`byte[] txBytes`|Amino encoded transaction.

Pointmul state
index|arg|info
-|-|-
0|`byte[] collatid`
1|`byte[] txid`
2|`string type`| {"simple" \| "multi"}|BigInteger[]/BigInteger[][])
3|`string id`| Pointmulid + slice num
4|`BigInteger[] data | BigInteger[][] data`|Depends if "simple" or "multi"


**executeChallenge**
Any
index|arg|info
-|-|-
0|`byte[] portingContractID`|Id
1|`int challengeNum`|

Challenge 1|2|3|4|5|7
index|arg|info
-|-|-
2|`int sigNum`|Signature num corresponding to the saved array in general state

Challenge 6
index|arg|info
-|-|-
2|`int sigNum`|Signature num corresponding to the saved array in general state
3|`int i`|
4|`int mulid`|

**registerAsCollateral**
index|arg|info
-|-|-
0|`byte[] address`|
1|`byte[] BNCAddress`|
2|`BigInteger newAmount`|
3|`byte  op`|

**newPorting**
index|arg|info
-|-|-
0|`byte[] collatID`|
1|`byte[] userAddr`|
2|`BigInteger amount`|
3|`string denom`|

**ackDepositByUser | challengedeposit | challengewithdraw | unlockcollateral**
index|arg|info
-|-|-
0|`byte[] portingContractID `|

**requestwithdraw**
index|arg|info
-|-|-
0|`byte[] collatID`|
1|`byte[] userAddr`|
2|`BigInteger AmountBNB`|
3|`byte[] userBCNAddr`|
