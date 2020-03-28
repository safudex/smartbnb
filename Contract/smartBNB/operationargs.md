# Operations

**savestate**
General state
index|arg|info
-|-|-
0|`byte[] portingContractID`|Id
1|`byte[][] signatures`|
2|`bigint[] xs `|Decompressed signature x
3|`bigint[] ys`|Decompressed signature y
4|`byte[][] signablebytes`
5|`ulong[][] pres`
6|`ulong[][] preshash`
7|`bigint[] preshashmod`
8|`byte[] txproof`
9|`byte[] blockHeader`
10|`byte[] txBytes`

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
