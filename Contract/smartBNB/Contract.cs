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

        private static readonly BigInteger Pcurve = BigInteger.Parse("115792089237316195423570985008687907853269984665640564039457584007908834671663");
        private static readonly BigInteger Acurve = 0;
        private static readonly BigInteger N = BigInteger.Parse("115792089237316195423570985008687907852837564279074904382605163141518161494337");

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

        // https://en.wikipedia.org/wiki/Elliptic_Curve_Digital_Signature_Algorithm
        // inverse_s needs to be calculated by the contract caller
        private static bool VerifyECSDA(BigInteger r, BigInteger s, byte[] message, BigInteger pubkey_x, BigInteger pubkey_y, BigInteger inverse_s)
        {
            // Verify public key is valid

            // Verify signature coordinates
            if (r <= 0 || r >= n || s <= 0 || s >= n)
            { // r,s \in [1, n-1]
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
            BigInteger[] mul1 = ScalarMultECC(G_x, G_y, u1);

            BigInteger[] mul2 = ScalarMultECC(pubkey_x, pubkey_y, u2);

            BigInteger x1 = mul1[0] + mul2[0];
            BigInteger y1 = mul1[1] + mul2[1];
            if (x1 == x_0 && y1 == y_0)
            { //Check if (x1, y1) == O
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

        // https://en.wikipedia.org/wiki/Elliptic_curve_point_multiplication#Point_multiplication
        private static BigInteger[] ScalarMultECC(BigInteger x, BigInteger y, BigInteger k)
        {
            BigInteger x1, y2;
            return new BigInteger[2] { x, y };
        }

        /*
        //BUGGY, NEED TO FIX
        private static BigInteger[] EccMultiply(BigInteger xs, BigInteger ys, BigInteger Scalar)
        {
            if (Scalar == 0 || Scalar >= N)
                throw new Exception("Invalid Scalar");

            BigInteger Qx = xs;
            BigInteger Qy = ys;

            for (int i = GetBitsCount(Scalar) - 1; i >= 0; i--)
            {
                BigInteger[] resECdouble = ECdouble(Qx, Qy);
                Qx = resECdouble[0];
                Qy = resECdouble[1];
                if (((Scalar >> i) & 1) == 1)
                {
                    BigInteger[] resECadd = ECadd(Qx, Qy, xs, ys);
                    Qx = resECadd[0];
                    Qy = resECadd[1];
                }
            }

            return new BigInteger[2] { Qx, Qy };
        }


        private static int GetBitsCount(BigInteger n)
        {
            int count = 0;
            while (n > 0)
            {
                n >>= 1;
                count++;
            }
            return count;
        }
        */

        private static BigInteger[] ECdouble(BigInteger xp, BigInteger yp)
        {
            BigInteger LamNumer = 3 * xp * xp + Acurve;
            BigInteger LamDenom = 2 * yp;
            BigInteger Lam = mod((LamNumer * ModInv(LamDenom, Pcurve)), Pcurve);
            BigInteger xr = mod((Lam * Lam - 2 * xp), Pcurve);
            BigInteger yr = mod((Lam * (xp - xr) - yp), Pcurve);
            return new BigInteger[2] { xr, yr };
        }

        private static BigInteger[] ECadd(BigInteger xp, BigInteger yp, BigInteger xq, BigInteger yq)
        {
            BigInteger m = mod(((yq - yp) * ModInv(xq - xp, Pcurve)), Pcurve);
            BigInteger xr = mod((m * m - xp - xq), Pcurve);
            BigInteger yr = mod((m * (xp - xr) - yp), Pcurve);
            return new BigInteger[2] { xr, yr };
        }

        private static BigInteger ModInv(BigInteger a, BigInteger n)
        {
            BigInteger lm = 1;
            BigInteger hm = 0;
            BigInteger low = mod(a, n); //a % n;
            BigInteger high = n;
            BigInteger ratio, nm, _new;
            while (low > 1)
            {
                ratio = high / low;
                nm = hm - lm * ratio;
                _new = high - low * ratio;
                BigInteger temp_lm = lm;
                lm = nm;
                BigInteger temp_low = low;
                low = _new;
                hm = temp_lm;
                high = temp_low;
            }

            return mod(lm, n); //lm % n;
        }

        public static BigInteger mod(BigInteger x, BigInteger m)
        {
            BigInteger r = x % m;
            return r < 0 ? r + m : r;
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

    }
}
