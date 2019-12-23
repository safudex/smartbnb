using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using System;
using System.Numerics;

namespace smartBNB
{
    public class Contract : SmartContract
    {
        private static readonly byte[] leafPrefix = { 0x00 };
        private static readonly byte[] innerPrefix = { 0x01 };

        public static bool Main(string operation, params object[] args)
        {
            if (Runtime.Trigger == TriggerType.Application)
            {
                switch (operation)
                {
                    case "spv":
                        return Validate((byte[])args[0], (byte[])args[1], (byte[])args[2]);
                    default:
                        return false;
                }
            }
            return true;
        }

        private static bool Validate(byte[] rawProof, byte[] rawHeader, byte[] rawSignatures)
        {
            // Verify relationship with the block. Compares if hDataHash and txProofRootHash are equal
            // TODO: Remove this comparison and compare hDataHash with computed hash from proof on TxValidation
            if (!AreEqual(rawHeader.Range(158, 32), rawProof.Range(32,32)))
                throw new Exception("Relationship with the signed block cannot be verified");            

            // Verify signatures
            VerifyBlock(rawHeader, rawSignatures);

            // Verify merkle tree
            VerifyTx(rawProof);

            return true;
        }

        private static bool VerifyBlock(byte[] rawHeader, byte[] rawSignatures)
        {
            //Obtaining header slices
            byte[][] headerSlices = new byte[16][];
            int accLen = 0;
            for (int i = 0; i < 16; i++)
            {
                headerSlices[i] = rawHeader.Range(accLen+1, rawHeader[accLen]);
                accLen += rawHeader[accLen]+1;
            }

            //Hashing header
            byte[] headerHash = SimpleHashFromByteSlices(headerSlices);

            //Obtaining signatures
            byte[][] signatures = new byte[11][];
            for (int i = 0; i < 11; i++)
            {
                signatures[i] = rawSignatures.Range(i*32, 32);
            }
            /*
            if (!VerifySignatures(headerHash, signatures))
                throw new Exception("Cruck");
            */

            return true;
        }

        private static bool VerifyTx(byte[] proof)
        {
            byte[] txProofRootHash = proof.Range(0, 32); //TODO: remove this + reindex ranges on unpacking
            byte[] txProofLeafHash = proof.Range(32, 32);
            int txProofIndex = proof.Range(64, 1)[0];
            int txProofTotal = proof.Range(65, 1)[0];

            int len = proof.Range(66, proof.Length - 66).Length / 32;
            byte[][] txProofAunts = new byte[len][];
            for (int i = 0; i < len; i++)
            {
                txProofAunts[i] = proof.Range(66 + (i * 32), 32);
            }

            if (txProofIndex < 0)
                throw new Exception("Proof index cannot be negative");
            if (txProofTotal <= 0)
                throw new Exception("Proof total must be positive");


            byte[] computedHash = ComputeHashFromAunts(txProofIndex, txProofTotal, txProofLeafHash, txProofAunts);
            //TODO: change txProofRootHash for hDataHash get from Header
            if (!AreEqual(computedHash, txProofRootHash))
                throw new Exception("Invalid root hash");

            return true;
        }

        private static byte[] ComputeHashFromAunts(int index, int total, byte[] leafHash, byte[][] innerHashes)
        {
            if (index >= total)
                return null;

            switch (total)
            {
                case 0:
                    throw new Exception("Cannot call computeHashFromAunts() with 0 total");
                case 1:
                    if (innerHashes.Length != 0)
                        return null;
                    return leafHash;
                default:
                    if (innerHashes.Length == 0)
                        return null;

                    int numLeft = GetSplitPoint(total);
                    if (index < numLeft)
                    {
                        byte[] leftHash = ComputeHashFromAunts(index, numLeft, leafHash, TakeArrays(innerHashes, 0, innerHashes.Length - 2));
                        if (leftHash == null)
                        {
                            return null;
                        }
                        return InnerHash(leftHash, innerHashes[innerHashes.Length - 1]);
                    }
                    
                    byte[] rightHash = ComputeHashFromAunts(index - numLeft, total - numLeft, leafHash, TakeArrays(innerHashes, 0, innerHashes.Length - 2));
                    if (rightHash == null)
                    {
                        return null;
                    }
                    return InnerHash(innerHashes[innerHashes.Length - 1], rightHash);
            }
        }

        // returns the largest power of 2 less than length
        private static int GetSplitPoint(int length)
        {
            if (length < 1)
                throw new Exception("Trying to split a tree with size < 1");

            return (length % 2 == 0) ? length / 2 : (length + 1) / 2;
        }

        // returns Sha256(0x01 || left || right)
        private static byte[] InnerHash(byte[] left, byte[] right)
        {
            return Sha256(innerPrefix.Concat(left.Concat(right)));
        }

        // returns Sha256(0x00 || leaf)
        private static byte[] LeafHash(byte[] leaf)
        {
            return Sha256(leafPrefix.Concat(leaf));
        }

        private static bool AreEqual(byte[] a1, byte[] b1)
        {
            if (a1.Length == b1.Length)
            {
                int i = 0;
                while (i < a1.Length && (a1[i] == b1[i]))
                {
                    i++;
                }
                if (i == a1.Length)
                {
                    return true;
                }
            }

            return false;
        }

        // returns the byte arrays located between indexes ini and fin (both included)
        private static byte[][] TakeArrays(byte[][] arr, int ini, int fin)
        {
            int len = (fin - ini)+1;
            byte[][] cutted = new byte[len][];
            for (int i = 0; i < len; i++)
            {
                cutted[i] = arr[ini+i];
            }
            return cutted;
        }

        private static byte[] SimpleHashFromByteSlices(byte[][] slices)
        {
            switch (slices.Length)
            {
                case 0:
                    return null;
                case 1:
                    return LeafHash(slices[0]);
                default:
                    int k = GetSplitPoint(slices.Length);
                    byte[] left = SimpleHashFromByteSlices(TakeArrays(slices, 0, k-1));
                    byte[] right = SimpleHashFromByteSlices(TakeArrays(slices, k, slices.Length-1));
                    return InnerHash(left, right);
            }
        }

        private static BigInteger SHA256(byte[] message)
        {
            // Implemented by NEOVM, see bytecode 0xA8 on https://docs.neo.org/developerguide/en/articles/neo_vm.html
            // https://docs.neo.org/docs/en-us/reference/scapi/fw/dotnet.html
            return Sha256(message).AsBigInteger();
        }

   		private static BigInteger sha512mod(ulong[] num){
			byte[] byteQ = {0xed, 0xd3, 0xf5, 0x5c, 0x1a, 0x63, 0x12, 0x58, 0xd6, 0x9c, 0xf7, 0xa2, 0xde, 0xf9, 0xde, 0x14, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x10};
			BigInteger q = byteQ.AsBigInteger();

			BigInteger res = 0;
			BigInteger powers = 1;
			for(int i=0; i<8; i++){
				for(int j=0; j<64; j++){
					if((num[i] & 1) == 1){
						res = modsum(res, powers, q);
					}
					num[i] = num[i] >> 1;
					powers = modsum(powers, powers, q);
				}
			}
			return res;
		}
        
        private static BigInteger[] EdDSA_PointAdd(BigInteger[] P, BigInteger[] Q, BigInteger p, BigInteger  d)
        {	
            BigInteger A = mulmod(modPositive(P[1]-P[0], p) , modPositive(Q[1]-Q[0], p), p);
            BigInteger B = mulmod(modsum(P[1],P[0], p), modsum(Q[1],Q[0], p), p);
            BigInteger C = mulmod(mulmod(modsum(P[3], P[3], p) , Q[3], p), d, p);
            BigInteger D = mulmod(modsum(P[2],P[2], p) , Q[2], p);

            BigInteger E= modPositive(B-A, p);
            BigInteger F= modPositive(D-C, p);
            BigInteger G= modsum(D,C, p);
            BigInteger H = modsum(B,A, p);

            BigInteger EF = mulmod(E,F,p);
            //BigInteger EF = E*F;
            BigInteger GH = mulmod(G,H,p);
            BigInteger FG = mulmod(F,G,p);
            BigInteger EH = mulmod(E,H,p);


            return new BigInteger[4] { EF, GH, FG, EH };
        }
	
		// To compute (a * b) % mod  
        private static BigInteger mulmod(BigInteger a, BigInteger b, BigInteger mod)  
        {  
            BigInteger res = 0; // Initialize result  

            while (b > 0)  
            {  
                // If b is odd, add 'a' to result  
                if (b % 2 == 1)  
                {  
                    //res = (res + a) % mod;
                    res = modsum(res, a, mod);
                }  

                // Multiply 'a' with 2  
                //a = (a * 2) % mod;  
                a = modsum(a, a, mod);

                // Divide b by 2  
                b /= 2;  
            }  

            // Return result  
            return res;  
        }
	
        private static BigInteger modsum(BigInteger a, BigInteger b, BigInteger p)
        {
            BigInteger k = (a-p)+b;
            if (k<0){
                k+=p;
            }
            return k;
        }

        private static BigInteger modrest(BigInteger a, BigInteger b, BigInteger p)
        {
            BigInteger r = a-b;
            return r < 0 ? r + p : r;
        }

        private static BigInteger modPositive(BigInteger x, BigInteger m)
        {
            return x < 0 ? x + m : x;
        }

    }
}
