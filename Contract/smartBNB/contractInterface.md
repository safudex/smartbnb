
  
# Operations

For a better understanding of the interface see proof.go.

**savestate**

Any
index|arg|info
-|-|-
0|`byte[] portingContractID`|Id
1|`string type`|{"GENERAL", "PM", "SIMPLE", "MULTI", "SIGNABLEBYTES"}

GENERAL
index|arg|info
-|-|-
2|`ulong[][] pres`|Array of 8 Sha512 preprocessed messages (signature[0:32] + validatorPubK + signableBytes) (only padding, see: FIPS 180-4*). Send bytes as ulong. `[[byte0preS0, byte1preS0], [byte0preS1, byte1preS1], ...]`.
3|`ulong[][] preshash`|8 arrays of 8 words of sha512(signature[0:32] + validatorPubK + signableBytes) `[[word0sha512S0, word1sha512S0], [word0sha512S1, word1sha512S1], ...]`
4|`byte[] txproof`|Data related to the SPV `[txProofRootHash + txProofLeafHash + txProofIndex + txProofTotal + ...txProofAunts]`
5|`byte[] blockHeader`|Header data cdc encoded. `len(cdcVersion) + cdcVersion + len(cdcChainID) + cdcChainID + ... + len(cdcProposerAddress) + cdcProposerAddress`
6|`byte[] txBytes`|Position where encoded output starts + output length + amino encoded transaction. `[indexOutputStart + outputLength + ...encodedTx]`

PM
index|arg|info
-|-|-
2|`byte[][] signatures`|Array of 8 signatures.  `[S0, S1, ...]`
3|`bigint[] xs `|Decompressed signatures x. `[S0_x, S1_x...]`
4|`bigint[] ys`|Decompressed signatures y. `[S0_y, S1_y...]`
5|`bigint[] preshashmod`|Hash of signable bytes as integer ("little endian") mod q. (for q see RFC8032**) `[sha512(signableBytesS0)%q, sha512(signableBytesS1)%q, ...]`

SIGNABLEBYTES
index|arg|info
-|-|-
2|`string id`|Index signature
3|`byte[][] signablebytes`|`[ValidatorIndex0 + (timestampStart0 + 5) + timestampStart0 + Round + VoteByValidator0, ValidatorIndex1 + ... + VoteByValidator1, ...]`

POINT MUL (SIMPLE, MULTI)
index|arg|info
-|-|-
2|`string id`| Point mul id {"Ps_sb", "ss_sb", "Qs_sb"}. `"pointmulId + sliceNum"`
3|`BigInteger[] data /*if simple*/` <br> `BigInteger[][] data /*if multi*/`|Array of intermediate states of point mul. Each 8 iterations of the while loop in function point_mul (see: RFC 8032**) is an intermediate step

<sup>S0, S1, is signature of the validator in position 0, signature of the validator in position 1
  
byte0 is first 8 bits of a message

word0 is first 8 bytes of the hash sha512</sup>

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
3|`int i`|Slice num {0...15}
4|`int mulid`|Point mul id {"Ps_sb", "ss_sb", "Qs_sb"}.

**registerAsCollateral**
index|arg|info
-|-|-
0|`byte[] address`|NEO Address
1|`byte[] BNCAddress`|Binance Chain Address of the collateral in format convert8BitsTo5Bits(RIPEMD160(SHA256(compressed public key))
2|`BigInteger newAmount`|Amount to add or substract to use as collateral
3|`byte  op`|For add 0x01, for substract 0x02

**newPorting**
index|arg|info
-|-|-
0|`byte[] collatID`|Collateral id returned from registerAsCollateral
1|`byte[] userAddr`|Neo address of the user
2|`BigInteger amount`|Amount of sBNB requested
3|`string denom`|Denom of the token {BNB}

**ackDepositByUser | challengedeposit | challengewithdraw | unlockcollateral**
index|arg|info
-|-|-
0|`byte[] portingContractID `|Porting contract id returned from newPorting

**requestwithdraw**
index|arg|info
-|-|-
0|`byte[] collatID`|Collateral id
1|`byte[] userAddr`|Neo address of the user
2|`BigInteger AmountBNB`|Amount of sBNB to withdraw
3|`byte[] userBCNAddr`|Binance Chain Address of the user in format convert8BitsTo5Bits(RIPEMD160(SHA256(compressed public key))

  
# Verify transaction
To verify the signatures we splitted the verification in diferent challenges.
TODO


*FIPS 180-4: [https://nvlpubs.nist.gov/nistpubs/FIPS/NIST.FIPS.180-4.pdf](https://nvlpubs.nist.gov/nistpubs/FIPS/NIST.FIPS.180-4.pdf)

**RFC 8032: [https://tools.ietf.org/html/rfc8032](https://tools.ietf.org/html/rfc8032)