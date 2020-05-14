
# Operations

**savestate**
Any
index|arg|info
-|-|-
0|`byte[] portingContractID`|Id
1|`string type`|{"GENERAL", "PM", "SIMPLE", "MULTI", "SIGNABLEBYTES"}

S0, S1, is signature0, signature1
byte0 is first 8 bits of the message
word0 is first 8 bytes of the hash sha512
q = 2**252 + 27742317777372353535851937790883648493

GENERAL
index|arg|info
-|-|-
2|`ulong[][] pres`|Array of 8 Sha512 preprocessed messages (signature[0:32] + validatorPubK + signableBytes) (only padding, see: FIPS 180-4*). Send bytes as ulong. `[[byte0preS0, byte1preS0], [byte0preS1, byte1preS1], ...]`.
3|`ulong[][] preshash`|8 arrays of 8 words of sha512(signature[0:32] + validatorPubK + signableBytes) `[[word0sha512S0, word1sha512S0], [word0sha512S1, word1sha512S1], ...]`
4|`byte[] txproof`|Data related to the SPV `[txProofRootHash + txProofLeafHash + txProofIndex + txProofTotal + ...txProofAunts]`
5|`byte[] blockHeader`|Header data cdc encoded. `len(cdcVersion) + cdcVersion + len(cdcChainID) + cdcChainID + ... + len(cdcProposerAddress) + cdcProposerAddress`
6|`byte[] txBytes`|Position where encoded output starts + output length + amino encoded transaction. `[indexOutputStart, outputLength, ...encodedTx]`

PM
index|arg|info
-|-|-
2|`byte[][] signatures`|Array of 8 signatures.  `[S0, S1, ...]`
3|`bigint[] xs `|Decompressed signatures x. `[S0_x, S1_x...]`
4|`bigint[] ys`|Decompressed signatures y. `[S0_y, S1_y...]`
5|`bigint[] preshashmod`|Hash of signable bytes as integer ("little endian") mod q.`[sha512(signableBytesS0)%q, sha512(signableBytesS1)%q, ...]`

SIGNABLEBYTES
index|arg|info
-|-|-
2|`string id`|Index signature
3|`byte[][] signablebytes`|Vote by validator

POINT MUL (SIMPLE, MULTI)
index|arg|info
-|-|-
3|`string id`| Point mul id {"Ps_sb", "ss_sb", "Qs_sb"} `"pointmulId + sliceNum"`
4|`BigInteger[] data /*if simple*/ | BigInteger[][] data /*if multi*/`|


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

*FIPS 180-4: [https://nvlpubs.nist.gov/nistpubs/FIPS/NIST.FIPS.180-4.pdf](https://nvlpubs.nist.gov/nistpubs/FIPS/NIST.FIPS.180-4.pdf)