﻿using Neo.SmartContract.Framework;
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
			byte[] byteP =  {0xed, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x7f};
			BigInteger p = byteP.AsBigInteger();
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
		private static BigInteger mulmod(BigInteger a, BigInteger b, BigInteger p){
            byte[] bytePower127 = {0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00};
            BigInteger power127 = bytePower127.AsBigInteger();
            BigInteger power128 = power127 * 2;
            BigInteger lowA = a%power128;
            BigInteger lowB = b%power127;
            BigInteger highA = a/power128;
            BigInteger highB = b/power127;
            BigInteger low = (lowA*lowB)%p;
            BigInteger high = (highA*highB)%p;
            BigInteger medium1 = ((lowA/2)*highB)%p;
            BigInteger medium2 = (lowB*highA)%p;
            BigInteger medium = modsum(medium1, medium2, p);
            medium = modsum(medium, medium, p);
            if(lowA%2 == 1){
                medium = modsum(medium, highB, p);
            }
            low = modsum(low, ((medium%power128)*power127)%p, p);
            high = modsum(high, medium/power128, p);
            high = mulmod256(high, 19, p);
            return modsum(high, low, p);
        }
	
        private static BigInteger mulmod256(BigInteger a, BigInteger b, BigInteger mod)  
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
	
        public static ulong sum0(ulong v)
        {
            return ROTR(v, 28) ^ ROTR(v, 34) ^ ROTR(v, 39);
        }
    
        public static ulong sum1(ulong v)
        {
            return ROTR(v, 14) ^ ROTR(v, 18) ^ ROTR(v, 41);
        }
    
        public static ulong sig0(ulong v)
        {
            return ROTR(v, 1) ^ ROTR(v, 8) ^ (v >> 7);
        }
    
        public static ulong sig1(ulong v)
        {
            return ROTR(v, 19) ^ ROTR(v, 61) ^ (v >> 6);
        }
    
        public static ulong Ch(ulong x, ulong y, ulong z)
        {
            return (x & y) ^ ((~x) & z);
        }
    
        public static ulong Maj(ulong x, ulong y, ulong z)
        {
            return (x & y) ^ (x & z) ^ (y & z);
        }
    
        public static ulong ROTR(ulong v, int count)//61
        {
            ulong temp = (v >> count);
         
            ulong a = (ulong)0x7FFFFFFFFFFFFFFF >> (63 - count);
            ulong d = v & a;
            ulong temp1 = (d << (64 - count));
         
            ulong res = temp | temp1;
         
            return res;
        }
	
	private static BigInteger sha512_modq(ulong[] pre)
        {
            /** arg example
            ulong[] pre = { 7017280570803617792, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 24 };
            see SHA512 preprocessing https://csrc.nist.gov/csrc/media/publications/fips/180/4/final/documents/fips180-4-draft-aug2014.pdf
            */
            ulong[] K = new ulong[80];
            K[0] = 4794697086780616226;
            K[1] = 8158064640168781261;
            K[2] = 13096744586834688815;
            K[3] = 16840607885511220156;
            K[4] = 4131703408338449720;
            K[5] = 6480981068601479193;
            K[6] = 10538285296894168987;
            K[7] = 12329834152419229976;
            K[8] = 15566598209576043074;
            K[9] = 1334009975649890238;
            K[10] = 2608012711638119052;
            K[11] = 6128411473006802146;
            K[12] = 8268148722764581231;
            K[13] = 9286055187155687089;
            K[14] = 11230858885718282805;
            K[15] = 13951009754708518548;
            K[16] = 16472876342353939154;
            K[17] = 17275323862435702243;
            K[18] = 1135362057144423861;
            K[19] = 2597628984639134821;
            K[20] = 3308224258029322869;
            K[21] = 5365058923640841347;
            K[22] = 6679025012923562964;
            K[23] = 8573033837759648693;
            K[24] = 10970295158949994411;
            K[25] = 12119686244451234320;
            K[26] = 12683024718118986047;
            K[27] = 13788192230050041572;
            K[28] = 14330467153632333762;
            K[29] = 15395433587784984357;
            K[30] = 489312712824947311;
            K[31] = 1452737877330783856;
            K[32] = 2861767655752347644;
            K[33] = 3322285676063803686;
            K[34] = 5560940570517711597;
            K[35] = 5996557281743188959;
            K[36] = 7280758554555802590;
            K[37] = 8532644243296465576;
            K[38] = 9350256976987008742;
            K[39] = 10552545826968843579;
            K[40] = 11727347734174303076;
            K[41] = 12113106623233404929;
            K[42] = 14000437183269869457;
            K[43] = 14369950271660146224;
            K[44] = 15101387698204529176;
            K[45] = 15463397548674623760;
            K[46] = 17586052441742319658;
            K[47] = 1182934255886127544;
            K[48] = 1847814050463011016;
            K[49] = 2177327727835720531;
            K[50] = 2830643537854262169;
            K[51] = 3796741975233480872;
            K[52] = 4115178125766777443;
            K[53] = 5681478168544905931;
            K[54] = 6601373596472566643;
            K[55] = 7507060721942968483;
            K[56] = 8399075790359081724;
            K[57] = 8693463985226723168;
            K[58] = 9568029438360202098;
            K[59] = 10144078919501101548;
            K[60] = 10430055236837252648;
            K[61] = 11840083180663258601;
            K[62] = 13761210420658862357;
            K[63] = 14299343276471374635;
            K[64] = 14566680578165727644;
            K[65] = 15097957966210449927;
            K[66] = 16922976911328602910;
            K[67] = 17689382322260857208;
            K[68] = 500013540394364858;
            K[69] = 748580250866718886;
            K[70] = 1242879168328830382;
            K[71] = 1977374033974150939;
            K[72] = 2944078676154940804;
            K[73] = 3659926193048069267;
            K[74] = 4368137639120453308;
            K[75] = 4836135668995329356;
            K[76] = 5532061633213252278;
            K[77] = 6448918945643986474;
            K[78] = 6902733635092675308;
            K[79] = 7801388544844847127;
           
            byte[] byteV = {0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,};
            ulong v = (ulong)byteV.AsBigInteger();
   
            ulong[] W = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            ulong[] H = new ulong[8];
            H[0] = 7640891576956012808;
            H[1] = 13503953896175478587;
            H[2] = 4354685564936845355;
            H[3] = 11912009170470909681;
            H[4] = 5840696475078001361;
            H[5] = 11170449401992604703;
            H[6] = 2270897969802886507;
            H[7] = 6620516959819538809;
           
            ulong a = H[0];
            ulong b = H[1];
            ulong c = H[2];
            ulong d = H[3];
            ulong e = H[4];
            ulong f = H[5];
            ulong g = H[6];
            ulong h = H[7];
            ulong T1=0;
            ulong T2=0;
           
            for(int j =0; j<pre.Length/16;j++)
            {
                a = H[0];
                b = H[1];
                c = H[2];
                d = H[3];
                e = H[4];
                f = H[5];
                g = H[6];
                h = H[7];
                for (int i = 0; i < 16; i++)
                {
                    W[i] = pre[(j*16)+i] & v;
                }

                for (int i = 16; i < 80; i++)
                {
                    W[i] = (sig1(W[i - 2]) + W[i - 7] + sig0(W[i - 15]) + W[i - 16])&v;
                }

                for (int i = 0; i < 80; i++)
                {
                    T1 = (h + sum1(e) + Ch(e, f, g) + K[i] + W[i])&v;
                    T2 = (sum0(a) + Maj(a, b, c))&v;
                    h = g;
                    g = f;
                    f = e;
                    e = (d + T1)&v;
                    d = c;
                    c = b;
                    b = a;
                    a = (T1 + T2)&v;
                    
                  
                }
                
                H[0] = (a+H[0])&v;
                H[1] = (b+H[1])&v;
                H[2] = (c+H[2])&v;
                H[3] = (d+H[3])&v;
                H[4] = (e+H[4])&v;
                H[5] = (f+H[5])&v;
                H[6] = (g+H[6])&v;
                H[7] = (h+H[7])&v;
           
            }
            //final hash is stored in H
            
            ulong[] hash = new ulong[8];
            hash[0] = H[7];
            hash[1] = H[6];
            hash[2] = H[5];
            hash[3] = H[4];
            hash[4] = H[3];
            hash[5] = H[2];
            hash[6] = H[1];
            hash[7] = H[0];
           
            return sha512mod(hash);
        }

    }
}
