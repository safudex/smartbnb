using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework;
using System;
using System.Numerics;

namespace Neo.SmartContract
{
    public class HelloWorld : Framework.SmartContract
    {

        public static bool Main()
        {
            
			
			byte[] byteX = {0x6b, 0xef, 0x5e, 0xac, 0x02, 0xc3, 0x50, 0x83, 0x30, 0x3a, 0xf1, 0x52, 0xbb, 0xa1, 0xda, 0x6d, 0x41, 0xe4, 0xda, 0xfe, 0xa9, 0x2d, 0x9e, 0x3a, 0x78, 0x8f, 0x6a, 0x57, 0x12, 0xc7, 0x5e, 0x0d};
			BigInteger x = byteX.AsBigInteger();
			byte[] byteY = {0xd3, 0x76, 0x9d, 0x8a, 0x1f, 0x78, 0xb4, 0xc1, 0x7a, 0x96, 0x5f, 0x7a, 0x30, 0xd4, 0x18, 0x1f, 0xab, 0xbd, 0x1f, 0x96, 0x9f, 0x46, 0xd3, 0xc8, 0xe8, 0x3b, 0x5a, 0xd4, 0x84, 0x54, 0x21, 0x58};
			BigInteger y = byteY.AsBigInteger();
			byte[] compressed = {211, 118, 157, 138, 31, 120, 180, 193, 122, 150, 95, 122, 48, 212, 24, 31, 171, 189, 31, 150, 159, 70, 211, 200, 232, 59, 90, 212, 132, 84, 33, 216};
			
			byte[] s_byteX = {0x42, 0x71, 0x50, 0x0a, 0x66, 0xb9, 0xa6, 0x78, 0x73, 0x3d, 0x95, 0xa5, 0x8a, 0xf9, 0x83, 0xd3, 0x0e, 0x11, 0xc1, 0xbc, 0xd7, 0xe1, 0x90, 0x71, 0xf7, 0x28, 0x5a, 0xbe, 0x64, 0x6f, 0x2f, 0x07};
			BigInteger s_x = s_byteX.AsBigInteger();
			byte[] s_byteY = {0x88, 0x54, 0x07, 0x0a, 0xc1, 0x7c, 0x6a, 0xed, 0xf5, 0x1e, 0x64, 0x4f, 0x68, 0xc5, 0x61, 0x31, 0x5e, 0xb2, 0x4a, 0x7b, 0x46, 0xee, 0x61, 0x29, 0xb9, 0x7f, 0x8d, 0xf4, 0xad, 0x57, 0x73, 0x5b};
			BigInteger s_y = s_byteY.AsBigInteger();
			byte[] s_compressed = {136, 84, 7, 10, 193, 124, 106, 237, 245, 30, 100, 79, 104, 197, 97, 49, 94, 178, 74, 123, 70, 238, 97, 41, 185, 127, 141, 244, 173, 87, 115, 91};
            
            int ini = 1;
            int fin = 1488;
            byte[] msg = {0x88, 0x54, 0x7, 0xa, 0xc1, 0x7c, 0x6a, 0xed, 0xf5, 0x1e, 0x64, 0x4f, 0x68, 0xc5, 0x61, 0x31, 0x5e, 0xb2, 0x4a, 0x7b, 0x46, 0xee, 0x61, 0x29, 0xb9, 0x7f, 0x8d, 0xf4, 0xad, 0x57, 0x73, 0x5b, 0xd3, 0x76, 0x9d, 0x8a, 0x1f, 0x78, 0xb4, 0xc1, 0x7a, 0x96, 0x5f, 0x7a, 0x30, 0xd4, 0x18, 0x1f, 0xab, 0xbd, 0x1f, 0x96, 0x9f, 0x46, 0xd3, 0xc8, 0xe8, 0x3b, 0x5a, 0xd4, 0x84, 0x54, 0x21, 0xd8, 0x79, 0x8, 0x2, 0x11, 0x9c, 0x5, 0xff, 0x2, 0x0, 0x0, 0x0, 0x0, 0x22, 0x48, 0xa, 0x20, 0xdf, 0x47, 0xf7, 0x85, 0x7b, 0x58, 0x28, 0xe3, 0x93, 0xf9, 0x5f, 0xe7, 0xdd, 0xca, 0x2a, 0x83, 0xd5, 0x81, 0xc4, 0xca, 0x9b, 0xb8, 0x47, 0x3d, 0xba, 0xc0, 0x75, 0x87, 0x18, 0x5d, 0x41, 0xf8, 0x12, 0x24, 0xa, 0x20, 0xb, 0xc9, 0xa9, 0x70, 0x61, 0xef, 0xe7, 0x2b, 0x48, 0xd3, 0xc1, 0xa3, 0xaa, 0x15, 0xab, 0x54, 0x5, 0xf5, 0x2b, 0x23, 0x3e, 0x7f, 0x62, 0x7f, 0x8, 0x6f, 0x2b, 0xc1, 0x8c, 0xd2, 0x87, 0xe4, 0x10, 0x1, 0x2a, 0xc, 0x8, 0xb6, 0xef, 0xe1, 0xee, 0x5, 0x10, 0x8d, 0xd6, 0xce, 0xec, 0x1, 0x32, 0x14, 0x42, 0x69, 0x6e, 0x61, 0x6e, 0x63, 0x65, 0x2d, 0x43, 0x68, 0x61, 0x69, 0x6e, 0x2d, 0x54, 0x69, 0x67, 0x72, 0x69, 0x73};
            ulong[] pre = new ulong[32];
            pre[0] = 9823484429979118317;
            pre[1] = 17662665080816623921;
            pre[2] = 6823598278751183145;
            pre[3] = 13366558301078647643;
            pre[4] = 15237539605813703873;
            pre[5] = 8833352697543661599;
            pre[6] = 12375082082861765576;
            pre[7] = 16734068709224882648;
            pre[8] = 8721222953058303746;
            pre[9] = 575146528;
            pre[10] = 16089100346476472547;
            pre[11] = 10662659042239457923;
            pre[12] = 15384794176549308221;
            pre[13] = 13456884909672841720;
            pre[14] = 1307180924597283184;
            pre[15] = 7057113314204303779;
            pre[16] = 12255890338324228899;
            pre[17] = 4503426450135657409;
            pre[18] = 10147322324009953804;
            pre[19] = 627952951704752269;
            pre[20] = 15478568459198284393;
            pre[21] = 7953759790091289448;
            pre[22] = 7019262635202406258;
            pre[23] = 7598557733792514048;
            pre[24] = 0;
            pre[25] = 0;
            pre[26] = 0;
            pre[27] = 0;
            pre[28] = 0;
            pre[29] = 0;
            pre[30] = 0;
            pre[31] = 1488;
            
            byte[] hash = {0xdf, 0x47, 0xf7, 0x85, 0x7b, 0x58, 0x28, 0xe3, 0x93, 0xf9, 0x5f, 0xe7, 0xdd, 0xca, 0x2a, 0x83, 0xd5, 0x81, 0xc4, 0xca, 0x9b, 0xb8, 0x47, 0x3d, 0xba, 0xc0, 0x75, 0x87, 0x18, 0x5d, 0x41, 0xf8};
            byte[] sbytes = {0xcf, 0x41, 0x33, 0x8f, 0xc2, 0x18, 0xd3, 0x7b, 0x2a, 0x63, 0x6e, 0xdc, 0x83, 0xee, 0x63, 0xe1, 0x4f, 0x6c, 0x0c, 0x4e, 0x14, 0x9a, 0x8f, 0x05, 0x73, 0xe4, 0x8d, 0x70, 0x12, 0xab, 0x15, 0x0d};
            
            Storage.Put("v", verify_signature(x, y, compressed, sbytes, s_compressed, s_x, s_y, pre, msg, ini, fin, hash)?"true":"false");
            
			return false;
        }
        
        private static bool verify_signature(BigInteger A0_xPubK, BigInteger A1_yPubK, byte[] pubK, byte[] s_signatureLow,
            byte[] Rs_signatureHigh, BigInteger R0_xSigHigh, BigInteger R1_ySigHigh, ulong[] pre, byte[] signBytes, int sB_ini, int sB_fin, byte[] blockHash){
            byte[] byteP = {0xed, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x7f};
			BigInteger p = byteP.AsBigInteger();
			
			byte[] byteD = {0xa3, 0x78, 0x59, 0x13, 0xca, 0x4d, 0xeb, 0x75, 0xab, 0xd8, 0x41, 0x41, 0x4d, 0x0a, 0x70, 0x00, 0x98, 0xe8, 0x79, 0x77, 0x79, 0x40, 0xc7, 0x8c, 0x73, 0xfe, 0x6f, 0x2b, 0xee, 0x6c, 0x03, 0x52};
			BigInteger d = byteD.AsBigInteger();
        
            if (pubK.Length != 32)
                throw new Exception("Bad public key length");
                
            if ((s_signatureLow.Length+Rs_signatureHigh.Length)!=64)
                throw new Exception("Bad signature length");
                
            if (!checkCompressed(A0_xPubK, A1_yPubK, pubK, p))
                throw new Exception("Relationship between compressed and decompressed public key not found");
            
            //BigInteger Rs = Rs_signatureHigh.AsBigInteger();
            if (!checkCompressed(R0_xSigHigh, R1_ySigHigh, Rs_signatureHigh, p))
                throw new Exception("Relationship between compressed and decompressed public key not found");
                
            byte[] q_bytes = {0xed, 0xd3, 0xf5, 0x5c, 0x1a, 0x63, 0x12, 0x58, 0xd6, 0x9c, 0xf7, 0xa2, 0xde, 0xf9, 0xde, 0x14, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x10};
            BigInteger q = q_bytes.AsBigInteger();
            BigInteger s = s_signatureLow.AsBigInteger();
            if (s>=q)
                return false;
            
            for (int i=0; i<32;i++)
            {
                if(blockHash[i]!=signBytes[80+i])
                    throw new Exception("Hash not contained in signBytes");
            }
            if (!checkBytes(pre, signBytes, sB_ini, sB_fin))
                throw new Exception("Wrong padded message");
            
            BigInteger h = sha512_modq(pre);
            
            byte[] g1bytes = {0x1a, 0xd5, 0x25, 0x8f, 0x60, 0x2d, 0x56, 0xc9, 0xb2, 0xa7, 0x25, 0x95, 0x60, 0xc7, 0x2c, 0x69, 0x5c, 0xdc, 0xd6, 0xfd, 0x31, 0xe2, 0xa4, 0xc0, 0xfe, 0x53, 0x6e, 0xcd, 0xd3, 0x36, 0x69, 0x21};
            BigInteger g1 = g1bytes.AsBigInteger();
            byte[] g2bytes = {0x58, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66, 0x66};
            BigInteger g2 = g2bytes.AsBigInteger();
            byte[] g4bytes = {0xa3, 0xdd, 0xb7, 0xa5, 0xb3, 0x8a, 0xde, 0x6d, 0xf5, 0x52, 0x51, 0x77, 0x80, 0x9f, 0xf0, 0x20, 0x7d, 0xe3, 0xab, 0x64, 0x8e, 0x4e, 0xea, 0x66, 0x65, 0x76, 0x8b, 0xd7, 0x0f, 0x5f, 0x87, 0x67};
            BigInteger g4 = g4bytes.AsBigInteger();
            BigInteger[] G = {g1, g2,1, g4};
            
            BigInteger[] A = {A0_xPubK, A1_yPubK, 1, mulmod(A0_xPubK, A1_yPubK, p)};
            
            BigInteger[] sB = EdDSA_PointMul(s, G, p, d);
            BigInteger[] hA = EdDSA_PointMul(h, A, p, d);
            
			BigInteger[] R = {R0_xSigHigh, R1_ySigHigh, 1, mulmod(R0_xSigHigh, R1_ySigHigh, p)};
            
            return point_equal(sB, EdDSA_PointAdd(R, hA, p, d), p);
        }
        
        public static bool checkBytes(ulong[] pre, byte[] bytes, int ini, int fin)
        {
            if(bytes.Length!=((fin-ini)+1)/8)
                return false;

            ulong val = pre[(fin - 1) / 64];
            
            byte byt = bytes[bytes.Length-1];
            
            ulong itmsg = ((ulong)1)<<(64 - (fin - 64));
            ulong itbyt = 1;
            
            for (int i=0; i<(fin - ini); i++)
            {
                if ((64-(fin%64)+i) % 64 == 0 & i != 0)
                {
                    val = pre[((fin - 1) / 64)-((64 - (fin % 64) + i) / 64)];
                    itmsg = 1;
                }

                if (i % 8 == 0)
                {
                    byt = bytes[((bytes.Length*8-i) / 8)-1];
                    itbyt = 1;
                }

                if(((byt & itbyt)>>(i%8)) == ((val & itmsg)>> ((64 - (fin - 64-i))%64)))
                {
                    itbyt = itbyt << 1;
                    itmsg = itmsg << 1;
                }
                else
                    return false;
      
            }
            return true;
        }
        
        private static bool point_equal(BigInteger[] P, BigInteger[] Q, BigInteger p)
        {
            if (modrest(mulmod(P[0], Q[2], p), mulmod(Q[0], P[2], p), p) != 0)
                return false;
            if (modrest(mulmod(P[1], Q[2], p), mulmod(Q[1], P[2], p), p) != 0)
                return false;
            return true;
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
        
        private static BigInteger modPositive(BigInteger x, BigInteger m)
        {
            return x < 0 ? x + m : x;
        }
	
        
        private static BigInteger[] EdDSA_PointMul(BigInteger s, BigInteger[] P, BigInteger p, BigInteger d)
        {
            BigInteger[] Q = { 0, 1, 1, 0 };
            while(s>0)
            {
                if ((s%2)==1)
                    Q = EdDSA_PointAdd(Q, P, p, d);
                P = EdDSA_PointAdd(P, P, p, d);
                s = s / 2;
            }
            return Q;
        }
        
        private static bool checkCompressed (BigInteger x, BigInteger y, byte[] compressed, BigInteger p){
            if(x<0 || x>=p){
                return false;
            }
            if(y<0 || y>=p){
                return false;
            }
            //Check that point belongs in the curve
            BigInteger x2 = mulmod(x, x, p);
            BigInteger y2 = mulmod(y, y, p);
            //((-x**2+y**2-1)*121666)%p==(-121665*x**2*y**2)%p
            if(mulmod(((modrest(y2, x2, p)-1)%p), 121666, p) != mulmod(p-121665, mulmod(x2, y2, p), p)){
                return false; //Point doesn't belong in the curve
            }
            //int.to_bytes(y | ((x & 1) << 255), 32, "little")
            int sign = compressed[31] & 0x80;
            if((sign>>7) != (x%2)){
                return false; //Compressed is wrong
            }
            
            int index31 = 31;
            byte withSign = compressed[31];
            byte noSign = (byte)(compressed[31] & 0x7F);
            compressed[index31] = noSign;
            if(compressed.AsBigInteger() != y){
                return false;
            }
            
            compressed[index31] = withSign;
            
            return true;
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
            Storage.Put("H", H[0]);
            
            ulong[] hash = new ulong[8];
            hash[0] = H[7];
            hash[1] = H[6];
            hash[2] = H[5];
            hash[3] = H[4];
            hash[4] = H[3];
            hash[5] = H[2];
            hash[6] = H[1];
            hash[7] = H[0];
           
            return sha512mod(H);
        }
        
        private static BigInteger sha512mod(ulong[] num){
			byte[] byteQ = {0xed, 0xd3, 0xf5, 0x5c, 0x1a, 0x63, 0x12, 0x58, 0xd6, 0x9c, 0xf7, 0xa2, 0xde, 0xf9, 0xde, 0x14, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x10};
			BigInteger q = byteQ.AsBigInteger();

			BigInteger res = 0;
			BigInteger powers = 1;
			for(int i=0; i<8; i++){
				for(int j=0; j<8; j++){
				    for(int k =0; k<8; k++){
    					if(((num[i]>>(56+k)) & 1) == 1){
    						res = modsum(res, powers, q);
    					}
    					powers = modsum(powers, powers, q);
				    }
				    num[i] = num[i] << 8;
				}
			}
			return res;
		}



    }
}
