using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using System;

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
