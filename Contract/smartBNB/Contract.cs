using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using System;
using System.Numerics;

namespace smartBNB
{
    public class Contract : SmartContract
    {
        private static readonly byte[] innerPrefix = { 0x01 };

        public static bool Main(string operation, params object[] args)
        {
            if (Runtime.Trigger == TriggerType.Application)
            {
                switch (operation)
                {
                    case "spv":
                        return Validate((byte[])args[0]);
                    default:
                        return false;
                }
            }
            return true;
        }

        // secp256k1 is defined in the ring Z/pZ
        private static readonly BigInteger p = BigInteger.Parse("115792089237316195423570985008687907853269984665640564039457584007908834671663");
        // y^2 = x^3 + 7
        private static readonly BigInteger a = 0;

        private static readonly BigInteger b = 7;
        // https://en.bitcoin.it/wiki/Secp256k1
        // https://bitcointalk.org/index.php?topic=237260.0
        private static readonly BigInteger n = BigInteger.Parse("115792089237316195423570985008687907852837564279074904382605163141518161494337");
        private static readonly BigInteger G_x = BigInteger.Parse("55066263022277343669578718895168534326250603453777594175500187360389116729240");
        private static readonly BigInteger G_y = BigInteger.Parse("32670510020758816978083085130507043184471273380659243275938904335757337482424");
        private static readonly BigInteger L_n = 256; // bit length of n
                                                      // Calculated using the fact that n*G=O
        private static readonly BigInteger x_0 = 0;
        private static readonly BigInteger y_0 = 0;

		private static BigInteger SHA256(byte[] message)
        {
            // Implemented by NEOVM, see bytecode 0xA8 on https://docs.neo.org/developerguide/en/articles/neo_vm.html
            // https://docs.neo.org/docs/en-us/reference/scapi/fw/dotnet.html
            return Sha256(message).AsBigInteger();
        }

        // https://en.wikipedia.org/wiki/Elliptic_curve_point_multiplication#Point_multiplication
        private static BigInteger[] scalarMultECC(BigInteger x, BigInteger y, BigInteger k)
        {
            BigInteger x1, y2;
            return new BigInteger[2] { x, y };
        }

        // https://en.wikipedia.org/wiki/Elliptic_Curve_Digital_Signature_Algorithm
        // inverse_s needs to be calculated by the contract caller
        private static bool verifyECSDA(BigInteger r, BigInteger s, byte[] message, BigInteger pubkey_x, BigInteger pubkey_y, BigInteger inverse_s)
        {
            // Verify public key is valid

            // Verify signature coordinates
            if (r <= 0 || r >= n || s <= 0 || s >= n){ // r,s \in [1, n-1]
                return false;
            }
            BigInteger e = SHA256(message);
            BigInteger z = e; // Nothing to be done as e's bit length is 256 == L_n
            if (((s * inverse_s) % n) != 1)
            { // Verify inverse_s is correct
                return false;
            }
            BigInteger u1 = (z * inverse_s) % n;

            BigInteger u2 = (r * inverse_s) % n;
            // The following part (sum of scalar point multiplications) can be optimized further using some math but I chose code correctness over cost
            BigInteger[] mul1 = scalarMultECC(G_x, G_y, u1);

            BigInteger[] mul2 = scalarMultECC(pubkey_x, pubkey_y, u2);

            BigInteger x1 = mul1[0] + mul2[0];
            BigInteger y1 = mul1[1] + mul2[1];
            if (x1 == x_0 && y1 == y_0){ //Check if (x1, y1) == O
                return false;
            }
            if (r == (x1 % n))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private static bool Validate(byte[] proof)
        {
            /*
            // Verify signatures
            bool isValidBlock = VerifyBlock();
            if (validBlock != true)
                throw new Exception("cuck");
            */

            // SPV Proof
            bool isValidTx = VerifyTx(proof);
            if (isValidTx != true)
                throw new Exception("Proof is not internally consistent");
            return true;
        }

        private static bool VerifyTx(byte[] proof)
        {

            byte[] blockDataHash = proof.Range(0, 64);
            byte[] txProofRootHash = proof.Range(64, 64);
            byte[] txProofLeafHash = proof.Range(128, 64);
            int txProofIndex = proof.Range(192, 2)[0];
            int txProofTotal = proof.Range(194, 2)[0];

            int len = proof.Range(196, proof.Length - 196).Length / 64;
            byte[][] txProofAunts = new byte[len][];
            for (int i = 0; i < len; i++)
            {
                txProofAunts[i] = proof.Range(196 + (i * 64), 64);
            }

            if (!AreEqual(blockDataHash, txProofRootHash))
                throw new Exception("Proof matches different data hash");
            if (txProofIndex < 0)
                throw new Exception("Proof index cannot be negative");
            if (txProofTotal <= 0)
                throw new Exception("Proof total must be positive");

            byte[] computedHash = computeHashFromAunts(txProofIndex, txProofTotal, txProofLeafHash, txProofAunts);
            if (!AreEqual(computedHash, txProofRootHash))
                throw new Exception("Invalid root hash");

            return true;
        }

        private static byte[] computeHashFromAunts(int index, int total, byte[] leafHash, byte[][] innerHashes)
        {
            if (index >= total || index < 0 || total <= 0)
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

                    int numLeft = getSplitPoint(total);
                    if (index < numLeft)
                    {
                        byte[] leftHash = computeHashFromAunts(index, numLeft, leafHash, TakeArray(innerHashes, innerHashes.Length - 1));
                        if (leftHash == null)
                        {
                            return null;
                        }
                        return innerHash(leftHash, innerHashes[innerHashes.Length - 1]);
                    }
                    byte[] rightHash = computeHashFromAunts(index - numLeft, total - numLeft, leafHash, TakeArray(innerHashes, innerHashes.Length - 1));
                    if (rightHash == null)
                    {
                        return null;
                    }
                    return innerHash(innerHashes[innerHashes.Length - 1], rightHash);
            }
        }

        // returns the largest power of 2 less than length
        private static int getSplitPoint(int length)
        {
            if (length < 1)
                throw new Exception("Trying to split a tree with size < 1");

            uint uLength = (uint)length;

            int bitlen = (int)getBitsCount(uLength);

            int k = 1 << (bitlen - 1);
            if (k == length)
            {
                k >>= 1;
            }
            return k;
        }

        // returns the number of significant bits in n
        private static uint getBitsCount(uint n)
        {
            uint count = 0;
            while (n > 0)
            {
                count += 1;
                n >>= 1;
            }
            return count;
        }

        // returns Sha256(0x01 || left || right)
        private static byte[] innerHash(byte[] left, byte[] right)
        {
            return Sha256(innerPrefix.Concat(left.Concat(right)));
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

        // returns the left-most X byte arrays from an array of byte arrays
        private static byte[][] TakeArray(byte[][] original, int count)
        {
            int len = (original.Length - count) & count;
            byte[][] cutted = new byte[len][];
            for (int i = 0; i < len; i++)
            {
                cutted[i] = original[i];
            }
            return cutted;
        }
        
    }
}
